using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Hir;

namespace Aster.Compiler.Frontend.TypeSystem;

/// <summary>
/// Hindley-Milner style type checker with constraint-based inference.
/// Performs unification, occurs check, and trait bound checking.
/// </summary>
public sealed class TypeChecker
{
    private readonly ConstraintSolver _solver = new();
    private readonly TraitSolver _traitSolver = new();
    private readonly Dictionary<int, TypeScheme> _symbolSchemes = new();
    private readonly Dictionary<string, StructType> _structTypes = new();
    private readonly Dictionary<string, EnumType> _enumTypes = new();
    // Tracks the current function/struct's generic type parameters by name
    private readonly Dictionary<string, GenericParameter> _currentGenericParams = new();
    // Collects trait constraints emitted at call sites (e.g. T: Clone when calling clone<T: Clone>)
    private readonly List<TraitConstraint> _pendingTraitConstraints = new();
    public DiagnosticBag Diagnostics { get; } = new();

    public TypeChecker()
    {
        _traitSolver.RegisterBuiltins();
    }

    /// <summary>Type-check an HIR program.</summary>
    public void Check(HirProgram program)
    {
        // Phase 0: Collect type declarations (structs, enums)
        CollectTypeDeclarations(program);

        // Phase 1: Generate constraints
        foreach (var decl in program.Declarations)
        {
            CheckNode(decl);
        }

        // Phase 2: Solve equality constraints
        _solver.Solve();

        // Phase 3: Check trait constraints (both from generic bounds and call sites)
        _traitSolver.CheckConstraints(_pendingTraitConstraints, _solver);

        // Merge diagnostics
        Diagnostics.AddRange(_solver.Diagnostics);
        Diagnostics.AddRange(_traitSolver.Diagnostics);
    }

    /// <summary>Collect struct and enum declarations for type resolution.</summary>
    private void CollectTypeDeclarations(HirProgram program)
    {
        foreach (var decl in program.Declarations)
        {
            if (decl is HirStructDecl structDecl)
            {
                // Register generic params temporarily for field resolution
                var savedGp = PushGenericParams(structDecl.GenericParams);

                var fields = new List<(string, AsterType)>();
                foreach (var field in structDecl.Fields)
                {
                    fields.Add((field.Name, ResolveTypeRef(field.TypeRef)));
                }
                var structType = new StructType(structDecl.Symbol.Name, fields);
                _structTypes[structDecl.Symbol.Name] = structType;
                structDecl.Symbol.Type = structType;

                PopGenericParams(savedGp);
            }
            else if (decl is HirEnumDecl enumDecl)
            {
                var variants = new List<(string, IReadOnlyList<AsterType>)>();
                foreach (var v in enumDecl.Variants)
                {
                    var vFields = v.Fields.Select(f => ResolveTypeRef(f)).ToList();
                    variants.Add((v.Name, vFields));
                }
                var enumType = new EnumType(enumDecl.Symbol.Name, variants);
                _enumTypes[enumDecl.Symbol.Name] = enumType;
                enumDecl.Symbol.Type = enumType;
            }
        }
    }

    private AsterType CheckNode(HirNode node) => node switch
    {
        HirFunctionDecl fn => CheckFunctionDecl(fn),
        HirStructDecl s => CheckStructDecl(s),
        HirEnumDecl e => CheckEnumDecl(e),
        HirLetStmt let => CheckLetStmt(let),
        HirReturnStmt ret => ret.Value != null ? CheckNode(ret.Value) : PrimitiveType.Void,
        HirExprStmt es => CheckNode(es.Expression),
        HirCallExpr call => CheckCallExpr(call),
        HirIdentifierExpr id => CheckIdentifier(id),
        HirLiteralExpr lit => CheckLiteral(lit),
        HirBinaryExpr bin => CheckBinaryExpr(bin),
        HirUnaryExpr un => CheckUnaryExpr(un),
        HirIfExpr ifExpr => CheckIfExpr(ifExpr),
        HirWhileStmt ws => CheckWhileStmt(ws),
        HirBlock block => CheckBlock(block),
        HirAssignExpr assign => CheckAssign(assign),
        HirMemberAccessExpr ma => CheckMemberAccess(ma),
        HirStructInitExpr structInit => CheckStructInit(structInit),
        HirPathExpr path => CheckPath(path),
        _ => PrimitiveType.Void,
    };

    private AsterType CheckStructInit(HirStructInitExpr structInit)
    {
        // Look up the struct type in our registry
        if (!_structTypes.TryGetValue(structInit.StructName, out var structType))
        {
            Diagnostics.ReportError("E0306", $"Unknown struct '{structInit.StructName}'", structInit.Span);
            return ErrorType.Instance;
        }

        // Verify all required fields are initialized
        var requiredFields = structType.Fields.Select(f => f.Name).ToHashSet();
        var providedFields = new HashSet<string>();

        foreach (var fieldInit in structInit.Fields)
        {
            // Check if field exists in struct
            var field = structType.Fields.FirstOrDefault(f => f.Name == fieldInit.FieldName);
            if (field.Name == null)
            {
                Diagnostics.ReportError("E0307", $"Struct '{structInit.StructName}' has no field '{fieldInit.FieldName}'", fieldInit.Span);
                continue;
            }

            // Check duplicate field initialization
            if (!providedFields.Add(fieldInit.FieldName))
            {
                Diagnostics.ReportError("E0308", $"Field '{fieldInit.FieldName}' initialized multiple times", fieldInit.Span);
                continue;
            }

            // Type-check the field value
            var valueType = CheckNode(fieldInit.Value);
            _solver.AddConstraint(new EqualityConstraint(valueType, field.Type, fieldInit.Value.Span));
        }

        // Check for missing fields
        var missingFields = requiredFields.Except(providedFields).ToList();
        if (missingFields.Count > 0)
        {
            Diagnostics.ReportError("E0309", $"Missing fields in struct '{structInit.StructName}' initialization: {string.Join(", ", missingFields)}", structInit.Span);
        }

        return structType;
    }

    private AsterType CheckPath(HirPathExpr path)
    {
        // For paths like Option::Some or Module::Type, we need to resolve them
        // For now, handle simple qualified paths (Type::Variant for enums)
        
        if (path.Segments.Count == 2)
        {
            // Could be EnumType::Variant
            var typeName = path.Segments[0];
            var variantName = path.Segments[1];

            if (_enumTypes.TryGetValue(typeName, out var enumType))
            {
                // Check if variant exists
                var variant = enumType.Variants.FirstOrDefault(v => v.Name == variantName);
                if (variant.Name == null)
                {
                    Diagnostics.ReportError("E0310", $"Enum '{typeName}' has no variant '{variantName}'", path.Span);
                    return ErrorType.Instance;
                }

                // For enum variant constructors, return a function type if it has fields
                if (variant.Fields.Count > 0)
                {
                    // Variant with fields acts as a constructor function
                    return new FunctionType(variant.Fields, enumType);
                }
                else
                {
                    // Variant without fields is just a value of the enum type
                    return enumType;
                }
            }
            
            // Could also be Module::Item - for now, return type variable
            // Full module system implementation would resolve this properly
        }
        
        // For single-segment paths or unresolved multi-segment paths
        // Return a type variable for now (will be resolved later if possible)
        return new TypeVariable();
    }

    private AsterType CheckFunctionDecl(HirFunctionDecl fn)
    {
        // Build generic parameter map for this function (with bounds)
        var savedGenericParams = PushGenericParams(fn.GenericParams);

        var paramTypes = new List<AsterType>();
        foreach (var param in fn.Parameters)
        {
            var paramType = ResolveTypeRef(param.TypeRef);
            param.Symbol.Type = paramType;
            paramTypes.Add(paramType);
        }

        var returnType = fn.ReturnType != null ? ResolveTypeRef(fn.ReturnType) : PrimitiveType.Void;
        fn.Symbol.Type = new FunctionType(paramTypes, returnType);

        // Register TypeScheme for generic functions so call sites get fresh type variables
        // This enables proper return-type inference: identity(42) → i32, not GenericParameter(T)
        if (fn.GenericParams.Count > 0)
        {
            var typeParams = _currentGenericParams.Values.ToList();
            var bounds = typeParams
                .SelectMany(gp => gp.Bounds.Select(b => new TraitBound(b.TraitName)))
                .ToList();
            _symbolSchemes[fn.Symbol.Id] = new TypeScheme(typeParams, bounds, fn.Symbol.Type);
        }

        var bodyType = CheckBlock(fn.Body);

        if (fn.ReturnType != null && !IsAssignableTo(bodyType, returnType))
        {
            Diagnostics.ReportError("E0300", $"Function '{fn.Symbol.Name}' expects return type '{returnType.DisplayName}' but body has type '{bodyType.DisplayName}'", fn.Span);
        }

        PopGenericParams(savedGenericParams);
        return fn.Symbol.Type;
    }

    private AsterType CheckStructDecl(HirStructDecl s)
    {
        // Register generic type parameters for field type resolution
        var savedGenericParams = PushGenericParams(s.GenericParams);

        var fields = new List<(string, AsterType)>();
        foreach (var field in s.Fields)
        {
            fields.Add((field.Name, ResolveTypeRef(field.TypeRef)));
        }
        var structType = new StructType(s.Symbol.Name, fields);
        s.Symbol.Type = structType;

        PopGenericParams(savedGenericParams);
        return structType;
    }

    /// <summary>
    /// Push generic type parameters (with their trait bounds) into the current scope,
    /// returning the saved state so it can be restored with <see cref="PopGenericParams"/>.
    /// </summary>
    private Dictionary<string, GenericParameter> PushGenericParams(IReadOnlyList<HirGenericParam> hirParams)
    {
        var saved = new Dictionary<string, GenericParameter>(_currentGenericParams);
        var gpId = 0;
        foreach (var hp in hirParams)
        {
            var bounds = hp.Bounds.Select(b => new TraitBound(b)).ToArray();
            _currentGenericParams[hp.Name] = new GenericParameter(hp.Name, gpId++, bounds);
        }
        return saved;
    }

    /// <summary>Restore the generic param scope saved by <see cref="PushGenericParams"/>.</summary>
    private void PopGenericParams(Dictionary<string, GenericParameter> saved)
    {
        _currentGenericParams.Clear();
        foreach (var kv in saved)
            _currentGenericParams[kv.Key] = kv.Value;
    }

    private AsterType CheckEnumDecl(HirEnumDecl e)
    {
        var variants = new List<(string, IReadOnlyList<AsterType>)>();
        foreach (var v in e.Variants)
        {
            var vFields = v.Fields.Select(f => ResolveTypeRef(f)).ToList();
            variants.Add((v.Name, vFields));
        }
        var enumType = new EnumType(e.Symbol.Name, variants);
        e.Symbol.Type = enumType;
        return enumType;
    }

    private AsterType CheckLetStmt(HirLetStmt let)
    {
        AsterType type;
        if (let.TypeRef != null)
        {
            type = ResolveTypeRef(let.TypeRef);
        }
        else if (let.Initializer != null)
        {
            type = CheckNode(let.Initializer);
        }
        else
        {
            type = new TypeVariable();
        }

        if (let.Initializer != null)
        {
            var initType = CheckNode(let.Initializer);
            _solver.AddConstraint(new EqualityConstraint(initType, type, let.Span));
        }

        // Generalize: create a type scheme for let-polymorphism
        var scheme = Generalize(type);
        _symbolSchemes[let.Symbol.Id] = scheme;
        let.Symbol.Type = type;
        return type;
    }

    /// <summary>Generalize a type into a type scheme for let-polymorphism.</summary>
    private TypeScheme Generalize(AsterType type)
    {
        var freeVars = new List<GenericParameter>();
        CollectFreeTypeVariables(type, freeVars);
        return new TypeScheme(freeVars, Array.Empty<TraitBound>(), type);
    }

    private void CollectFreeTypeVariables(AsterType type, List<GenericParameter> vars)
    {
        type = _solver.Resolve(type);
        switch (type)
        {
            case TypeVariable tv:
                if (!vars.Any(v => v.Id == tv.Id))
                {
                    vars.Add(new GenericParameter($"T{tv.Id}", tv.Id));
                }
                break;
            case FunctionType ft:
                foreach (var param in ft.ParameterTypes)
                    CollectFreeTypeVariables(param, vars);
                CollectFreeTypeVariables(ft.ReturnType, vars);
                break;
            case ReferenceType rt:
                CollectFreeTypeVariables(rt.Inner, vars);
                break;
            case TypeApp ta:
                CollectFreeTypeVariables(ta.Constructor, vars);
                foreach (var arg in ta.Arguments)
                    CollectFreeTypeVariables(arg, vars);
                break;
        }
    }

    private AsterType CheckCallExpr(HirCallExpr call)
    {
        var calleeType = CheckNode(call.Callee);

        // Handle built-in print function
        if (call.Callee is HirIdentifierExpr id && (id.Name == "print" || id.Name == "println"))
        {
            foreach (var arg in call.Arguments) CheckNode(arg);
            return PrimitiveType.Void;
        }

        if (calleeType is FunctionType fnType)
        {
            if (call.Arguments.Count != fnType.ParameterTypes.Count)
            {
                Diagnostics.ReportError("E0302", $"Expected {fnType.ParameterTypes.Count} arguments but got {call.Arguments.Count}", call.Span);
            }

            // Collect argument types and add equality constraints
            var argTypes = new List<AsterType>(call.Arguments.Count);
            for (int i = 0; i < Math.Min(call.Arguments.Count, fnType.ParameterTypes.Count); i++)
            {
                var argType = CheckNode(call.Arguments[i]);
                argTypes.Add(argType);
                _solver.AddConstraint(new EqualityConstraint(argType, fnType.ParameterTypes[i], call.Arguments[i].Span));
            }

            // Emit trait constraints for bounded generic parameters.
            // CheckIdentifier may have instantiated a TypeScheme, replacing GenericParameters with
            // fresh TypeVariables.  To recover the bounds, look up the original symbol type.
            // The original symbol.Type is a FunctionType whose ParameterTypes are GenericParameters.
            var originalParamTypes = GetOriginalParamTypes(call.Callee);
            for (int i = 0; i < Math.Min(argTypes.Count, originalParamTypes.Count); i++)
            {
                if (originalParamTypes[i] is GenericParameter gp && gp.Bounds.Count > 0)
                {
                    foreach (var bound in gp.Bounds)
                    {
                        _pendingTraitConstraints.Add(new TraitConstraint(argTypes[i], bound, call.Arguments[i].Span));
                    }
                }
            }

            return fnType.ReturnType;
        }

        // For unknown callee types, assume function and generate constraints
        var returnType = new TypeVariable();
        var paramTypes = call.Arguments.Select(_ => new TypeVariable()).ToList();
        var expectedFnType = new FunctionType(paramTypes, returnType);
        _solver.AddConstraint(new EqualityConstraint(calleeType, expectedFnType, call.Span));

        for (int i = 0; i < call.Arguments.Count; i++)
        {
            var argType = CheckNode(call.Arguments[i]);
            _solver.AddConstraint(new EqualityConstraint(argType, paramTypes[i], call.Arguments[i].Span));
        }

        return returnType;
    }

    /// <summary>
    /// Retrieve the original (pre-instantiation) parameter types of the callee's function type.
    /// This is used for bound checking: TypeScheme instantiation replaces GenericParameters with
    /// TypeVariables, losing bound information.  The original symbol.Type still has GenericParameters.
    /// </summary>
    private static IReadOnlyList<AsterType> GetOriginalParamTypes(HirNode callee)
    {
        if (callee is HirIdentifierExpr id &&
            id.ResolvedSymbol?.Type is FunctionType originalFnType)
        {
            return originalFnType.ParameterTypes;
        }
        return Array.Empty<AsterType>();
    }

    private AsterType CheckIdentifier(HirIdentifierExpr id)
    {
        if (id.ResolvedSymbol == null)
            return new TypeVariable();

        // Instantiate type scheme for let-polymorphism
        if (_symbolSchemes.TryGetValue(id.ResolvedSymbol.Id, out var scheme))
        {
            var substitution = new Dictionary<int, AsterType>();
            return scheme.Instantiate(substitution);
        }

        return id.ResolvedSymbol.Type ?? new TypeVariable();
    }

    private AsterType CheckLiteral(HirLiteralExpr lit) => lit.LiteralKind switch
    {
        Ast.LiteralKind.Integer => PrimitiveType.I32,
        Ast.LiteralKind.Float => PrimitiveType.F64,
        Ast.LiteralKind.String => PrimitiveType.StringType,
        Ast.LiteralKind.Char => PrimitiveType.Char,
        Ast.LiteralKind.Bool => PrimitiveType.Bool,
        _ => ErrorType.Instance,
    };

    private AsterType CheckBinaryExpr(HirBinaryExpr bin)
    {
        var leftType = CheckNode(bin.Left);
        var rightType = CheckNode(bin.Right);

        return bin.Operator switch
        {
            Ast.BinaryOperator.Add or Ast.BinaryOperator.Sub or Ast.BinaryOperator.Mul or Ast.BinaryOperator.Div or Ast.BinaryOperator.Mod
                => leftType,
            Ast.BinaryOperator.Eq or Ast.BinaryOperator.Ne or Ast.BinaryOperator.Lt or Ast.BinaryOperator.Le or Ast.BinaryOperator.Gt or Ast.BinaryOperator.Ge
                => PrimitiveType.Bool,
            Ast.BinaryOperator.And or Ast.BinaryOperator.Or
                => PrimitiveType.Bool,
            _ => leftType,
        };
    }

    private AsterType CheckUnaryExpr(HirUnaryExpr un)
    {
        var operandType = CheckNode(un.Operand);
        return un.Operator switch
        {
            Ast.UnaryOperator.Negate => operandType,
            Ast.UnaryOperator.Not => PrimitiveType.Bool,
            Ast.UnaryOperator.BitNot => operandType,
            Ast.UnaryOperator.Ref => new ReferenceType(operandType, false),
            Ast.UnaryOperator.MutRef => new ReferenceType(operandType, true),
            _ => operandType,
        };
    }

    private AsterType CheckIfExpr(HirIfExpr ifExpr)
    {
        var condType = CheckNode(ifExpr.Condition);
        if (condType is not PrimitiveType { Kind: PrimitiveKind.Bool } && condType is not TypeVariable)
        {
            Diagnostics.ReportError("E0304", $"If condition must be bool, got '{condType.DisplayName}'", ifExpr.Condition.Span);
        }

        var thenType = CheckBlock(ifExpr.ThenBranch);

        if (ifExpr.ElseBranch != null)
        {
            var elseType = CheckNode(ifExpr.ElseBranch);
            if (!IsAssignableTo(elseType, thenType))
            {
                Diagnostics.ReportWarning("W0002", $"If branches have different types: '{thenType.DisplayName}' and '{elseType.DisplayName}'", ifExpr.Span);
            }
        }

        return thenType;
    }

    private AsterType CheckWhileStmt(HirWhileStmt ws)
    {
        var condType = CheckNode(ws.Condition);
        CheckBlock(ws.Body);
        return PrimitiveType.Void;
    }

    private AsterType CheckBlock(HirBlock block)
    {
        AsterType? returnType = null;
        
        foreach (var stmt in block.Statements)
        {
            var stmtType = CheckNode(stmt);
            // If we encounter a return statement, track its type
            if (stmt is HirReturnStmt ret)
            {
                returnType = ret.Value != null ? CheckNode(ret.Value) : PrimitiveType.Void;
            }
        }
        
        // If block has a return statement, use that type
        if (returnType != null)
        {
            return returnType;
        }
        
        // Otherwise, use tail expression type or void
        return block.TailExpression != null ? CheckNode(block.TailExpression) : PrimitiveType.Void;
    }

    private AsterType CheckAssign(HirAssignExpr assign)
    {
        var targetType = CheckNode(assign.Target);
        var valueType = CheckNode(assign.Value);
        return PrimitiveType.Void;
    }

    private AsterType CheckMemberAccess(HirMemberAccessExpr ma)
    {
        var objType = CheckNode(ma.Object);
        if (objType is StructType st)
        {
            var field = st.Fields.FirstOrDefault(f => f.Name == ma.Member);
            if (field.Name == null)
            {
                Diagnostics.ReportError("E0305", $"Type '{st.Name}' has no field '{ma.Member}'", ma.Span);
                return ErrorType.Instance;
            }
            return field.Type;
        }
        return new TypeVariable();
    }

    // ========== Unification ==========

    /// <summary>Unify two types, recording substitutions.</summary>
    public bool Unify(AsterType a, AsterType b)
    {
        return _solver.Unify(a, b);
    }

    private AsterType Resolve(AsterType type)
    {
        return _solver.Resolve(type);
    }

    private bool OccursCheck(TypeVariable tv, AsterType type)
    {
        type = Resolve(type);
        if (type is TypeVariable other && other.Id == tv.Id)
            return true;
        if (type is FunctionType fn)
        {
            return fn.ParameterTypes.Any(p => OccursCheck(tv, p)) || OccursCheck(tv, fn.ReturnType);
        }
        return false;
    }

    private bool IsAssignableTo(AsterType from, AsterType to)
    {
        return _solver.Unify(from, to);
    }

    private AsterType ResolveTypeRef(HirTypeRef? typeRef)
    {
        if (typeRef == null) return new TypeVariable();

        // Check primitive types first
        var primitiveType = typeRef.Name switch
        {
            "i8" => PrimitiveType.I8,
            "i16" => PrimitiveType.I16,
            "i32" => PrimitiveType.I32,
            "i64" => PrimitiveType.I64,
            "u8" => PrimitiveType.U8,
            "u16" => PrimitiveType.U16,
            "u32" => PrimitiveType.U32,
            "u64" => PrimitiveType.U64,
            "f32" => PrimitiveType.F32,
            "f64" => PrimitiveType.F64,
            "bool" => PrimitiveType.Bool,
            "char" => PrimitiveType.Char,
            "String" => PrimitiveType.StringType,
            "void" => PrimitiveType.Void,
            _ => null
        };
        
        if (primitiveType != null)
            return primitiveType;

        // Check generic type parameters in current scope (e.g. T, A, B)
        if (_currentGenericParams.TryGetValue(typeRef.Name, out var genericParam))
            return genericParam;

        // Check user-defined types
        if (_structTypes.TryGetValue(typeRef.Name, out var structType))
            return structType;
        
        if (_enumTypes.TryGetValue(typeRef.Name, out var enumType))
            return enumType;

        // Fall back to resolved symbol or type variable
        return typeRef.ResolvedSymbol?.Type ?? new TypeVariable();
    }
}
