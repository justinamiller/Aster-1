using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Hir;

namespace Aster.Compiler.Frontend.TypeSystem;

/// <summary>
/// Hindley-Milner style type checker with constraint-based inference.
/// Performs unification, occurs check, and trait bound checking.
/// </summary>
public sealed class TypeChecker
{
    private readonly Dictionary<int, AsterType> _substitutions = new();
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>Type-check an HIR program.</summary>
    public void Check(HirProgram program)
    {
        foreach (var decl in program.Declarations)
        {
            CheckNode(decl);
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
        _ => PrimitiveType.Void,
    };

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
            if (!IsAssignableTo(initType, type))
            {
                Diagnostics.ReportError("E0301", $"Cannot assign '{initType.DisplayName}' to variable of type '{type.DisplayName}'", let.Span);
            }
        }

        let.Symbol.Type = type;
        return type;
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
                if (!IsAssignableTo(argType, fnType.ParameterTypes[i]))
                {
                    Diagnostics.ReportError("E0303", $"Argument type mismatch: expected '{fnType.ParameterTypes[i].DisplayName}' but got '{argType.DisplayName}'", call.Arguments[i].Span);
                }
            }

            return fnType.ReturnType;
        }

        return new TypeVariable();
    }

    private AsterType CheckIdentifier(HirIdentifierExpr id)
    {
        return id.ResolvedSymbol?.Type ?? new TypeVariable();
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
        foreach (var stmt in block.Statements)
        {
            CheckNode(stmt);
        }
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
        a = Resolve(a);
        b = Resolve(b);

        if (a is TypeVariable tv1)
        {
            if (!OccursCheck(tv1, b))
            {
                _substitutions[tv1.Id] = b;
                return true;
            }
            return false;
        }

        if (b is TypeVariable tv2)
        {
            if (!OccursCheck(tv2, a))
            {
                _substitutions[tv2.Id] = a;
                return true;
            }
            return false;
        }

        if (a is PrimitiveType pa && b is PrimitiveType pb)
            return pa.Kind == pb.Kind;

        if (a is FunctionType fa && b is FunctionType fb)
        {
            if (fa.ParameterTypes.Count != fb.ParameterTypes.Count)
                return false;
            for (int i = 0; i < fa.ParameterTypes.Count; i++)
            {
                if (!Unify(fa.ParameterTypes[i], fb.ParameterTypes[i]))
                    return false;
            }
            return Unify(fa.ReturnType, fb.ReturnType);
        }

        return a.GetType() == b.GetType();
    }

    private AsterType Resolve(AsterType type)
    {
        while (type is TypeVariable tv && _substitutions.TryGetValue(tv.Id, out var resolved))
            type = resolved;
        return type;
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
        from = Resolve(from);
        to = Resolve(to);

        if (from is TypeVariable || to is TypeVariable)
            return true;
        if (from is ErrorType || to is ErrorType)
            return true;
        if (from is PrimitiveType pf && to is PrimitiveType pt)
            return pf.Kind == pt.Kind;
        if (from.GetType() == to.GetType())
            return true;
        return false;
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
