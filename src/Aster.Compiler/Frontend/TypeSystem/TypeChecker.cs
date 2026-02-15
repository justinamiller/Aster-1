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
    public DiagnosticBag Diagnostics { get; } = new();

    public TypeChecker()
    {
        _traitSolver.RegisterBuiltins();
    }

    /// <summary>Type-check an HIR program.</summary>
    public void Check(HirProgram program)
    {
        // Phase 1: Generate constraints
        foreach (var decl in program.Declarations)
        {
            CheckNode(decl);
        }

        // Phase 2: Solve constraints
        _solver.Solve();

        // Phase 3: Check trait constraints
        var traitConstraints = new List<TraitConstraint>();
        _traitSolver.CheckConstraints(traitConstraints, _solver);

        // Merge diagnostics
        Diagnostics.AddRange(_solver.Diagnostics);
        Diagnostics.AddRange(_traitSolver.Diagnostics);
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
        // For now, return a type variable
        // In a full implementation, we'd look up the struct definition and verify fields
        return new TypeVariable();
    }

    private AsterType CheckPath(HirPathExpr path)
    {
        // For paths like Option::Some, we need to resolve them
        // For now, return a fresh type variable
        return new TypeVariable();
    }

    private AsterType CheckFunctionDecl(HirFunctionDecl fn)
    {
        var paramTypes = new List<AsterType>();
        foreach (var param in fn.Parameters)
        {
            var paramType = ResolveTypeRef(param.TypeRef);
            param.Symbol.Type = paramType;
            paramTypes.Add(paramType);
        }

        var returnType = fn.ReturnType != null ? ResolveTypeRef(fn.ReturnType) : PrimitiveType.Void;
        fn.Symbol.Type = new FunctionType(paramTypes, returnType);

        var bodyType = CheckBlock(fn.Body);

        if (fn.ReturnType != null && !IsAssignableTo(bodyType, returnType))
        {
            Diagnostics.ReportError("E0300", $"Function '{fn.Symbol.Name}' expects return type '{returnType.DisplayName}' but body has type '{bodyType.DisplayName}'", fn.Span);
        }

        return fn.Symbol.Type;
    }

    private AsterType CheckStructDecl(HirStructDecl s)
    {
        var fields = new List<(string, AsterType)>();
        foreach (var field in s.Fields)
        {
            fields.Add((field.Name, ResolveTypeRef(field.TypeRef)));
        }
        var structType = new StructType(s.Symbol.Name, fields);
        s.Symbol.Type = structType;
        return structType;
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

            for (int i = 0; i < Math.Min(call.Arguments.Count, fnType.ParameterTypes.Count); i++)
            {
                var argType = CheckNode(call.Arguments[i]);
                _solver.AddConstraint(new EqualityConstraint(argType, fnType.ParameterTypes[i], call.Arguments[i].Span));
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

        return typeRef.Name switch
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
            _ => typeRef.ResolvedSymbol?.Type ?? new TypeVariable(),
        };
    }
}
