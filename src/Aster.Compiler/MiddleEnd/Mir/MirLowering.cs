using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.Hir;

namespace Aster.Compiler.MiddleEnd.Mir;

/// <summary>
/// Lowers HIR to MIR (Mid-level IR).
/// Converts high-level constructs into SSA-based instructions with explicit control flow.
/// </summary>
public sealed class MirLowering
{
    private MirFunction? _currentFunction;
    private MirBasicBlock? _currentBlock;
    private int _tempCounter;
    private readonly Dictionary<string, MirOperand> _localVariables = new();
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>Lower an HIR program to MIR.</summary>
    public MirModule Lower(HirProgram program)
    {
        var module = new MirModule("main");

        foreach (var decl in program.Declarations)
        {
            if (decl is HirFunctionDecl fn)
            {
                var mirFn = LowerFunction(fn);
                module.Functions.Add(mirFn);
            }
        }

        return module;
    }

    private MirFunction LowerFunction(HirFunctionDecl fn)
    {
        _currentFunction = new MirFunction(fn.Symbol.Name);
        _tempCounter = 0;
        _localVariables.Clear(); // Clear local variables for new function

        // Add parameters
        for (int i = 0; i < fn.Parameters.Count; i++)
        {
            var param = fn.Parameters[i];
            var mirType = ResolveType(param.TypeRef);
            _currentFunction.Parameters.Add(new MirParameter(param.Symbol.Name, mirType, i));
        }

        _currentFunction.ReturnType = fn.ReturnType != null ? ResolveType(fn.ReturnType) : MirType.Void;

        // Create entry block
        _currentBlock = _currentFunction.CreateBlock("entry");

        // Lower body
        var result = LowerBlock(fn.Body);

        // Add return if not already terminated
        if (_currentBlock?.Terminator == null)
        {
            // If function returns void, don't return the tail expression value
            if (_currentFunction.ReturnType.Name == "void")
            {
                _currentBlock!.Terminator = new MirReturn(null);
            }
            else
            {
                _currentBlock!.Terminator = new MirReturn(result);
            }
        }

        return _currentFunction;
    }

    private MirOperand? LowerBlock(HirBlock block)
    {
        MirOperand? lastResult = null;

        foreach (var stmt in block.Statements)
        {
            LowerNode(stmt);
        }

        if (block.TailExpression != null)
        {
            lastResult = LowerExpr(block.TailExpression);
        }

        return lastResult;
    }

    private void LowerNode(HirNode node)
    {
        switch (node)
        {
            case HirLetStmt let:
                LowerLetStmt(let);
                break;
            case HirReturnStmt ret:
                LowerReturn(ret);
                break;
            case HirExprStmt es:
                LowerExpr(es.Expression);
                break;
            case HirWhileStmt ws:
                LowerWhile(ws);
                break;
            default:
                LowerExpr(node);
                break;
        }
    }

    private void LowerLetStmt(HirLetStmt let)
    {
        if (let.Initializer != null)
        {
            var value = LowerExpr(let.Initializer);
            if (value != null)
            {
                // Instead of creating a new variable and assigning,
                // just map the variable name to the value operand directly
                // This works because in SSA form, variables are just names for values
                _localVariables[let.Symbol.Name] = value;
                
                // Don't emit an Assign instruction - in SSA, we just use the value directly
            }
        }
        else
        {
            // No initializer - create a placeholder (though this is unusual in SSA)
            var dest = MirOperand.Variable(let.Symbol.Name, MirType.I64);
            _localVariables[let.Symbol.Name] = dest;
        }
    }

    private void LowerReturn(HirReturnStmt ret)
    {
        MirOperand? value = null;
        if (ret.Value != null)
        {
            value = LowerExpr(ret.Value);
            // Coerce the return value type to match the function's return type
            if (value != null && _currentFunction != null)
            {
                value = CoerceType(value, _currentFunction.ReturnType);
            }
        }
        _currentBlock!.Terminator = new MirReturn(value);
    }

    private void LowerWhile(HirWhileStmt ws)
    {
        var condBlock = _currentFunction!.CreateBlock("while.cond");
        var bodyBlock = _currentFunction.CreateBlock("while.body");
        var exitBlock = _currentFunction.CreateBlock("while.exit");

        _currentBlock!.Terminator = new MirBranch(condBlock.Index);

        _currentBlock = condBlock;
        var cond = LowerExpr(ws.Condition);
        _currentBlock.Terminator = new MirConditionalBranch(cond!, bodyBlock.Index, exitBlock.Index);

        _currentBlock = bodyBlock;
        LowerBlock(ws.Body);
        if (_currentBlock.Terminator == null)
            _currentBlock.Terminator = new MirBranch(condBlock.Index);

        _currentBlock = exitBlock;
    }

    private MirOperand? LowerExpr(HirNode node)
    {
        switch (node)
        {
            case HirLiteralExpr lit:
                return LowerLiteral(lit);

            case HirIdentifierExpr id:
                // First check if it's a local variable
                if (_localVariables.TryGetValue(id.Name, out var localVar))
                {
                    return localVar;
                }
                
                // Then try to find the type from function parameters
                var paramType = MirType.I64; // default
                if (_currentFunction != null)
                {
                    var param = _currentFunction.Parameters.FirstOrDefault(p => p.Name == id.Name);
                    if (param != null)
                    {
                        paramType = param.Type;
                    }
                }
                return MirOperand.Variable(id.Name, paramType);

            case HirCallExpr call:
                return LowerCall(call);

            case HirBinaryExpr bin:
                return LowerBinary(bin);

            case HirUnaryExpr un:
                return LowerUnary(un);

            case HirIfExpr ifExpr:
                return LowerIf(ifExpr);

            case HirBlock block:
                return LowerBlock(block);

            case HirAssignExpr assign:
                return LowerAssign(assign);

            case HirMemberAccessExpr ma:
                var obj = LowerExpr(ma.Object);
                var temp = NewTemp(MirType.I64);
                Emit(new MirInstruction(MirOpcode.Load, temp, new[] { obj! }, ma.Member));
                return temp;

            default:
                return null;
        }
    }

    private MirOperand LowerLiteral(HirLiteralExpr lit)
    {
        return lit.LiteralKind switch
        {
            LiteralKind.Integer => MirOperand.Constant(lit.Value, MirType.I64),
            LiteralKind.Float => MirOperand.Constant(lit.Value, MirType.F64),
            LiteralKind.String => MirOperand.Constant(lit.Value, MirType.StringPtr),
            LiteralKind.Char => MirOperand.Constant(lit.Value, MirType.Char),
            LiteralKind.Bool => MirOperand.Constant(lit.Value, MirType.Bool),
            _ => MirOperand.Constant(0, MirType.I64),
        };
    }

    private MirOperand? LowerCall(HirCallExpr call)
    {
        var callee = LowerExpr(call.Callee);
        var args = new List<MirOperand>();
        foreach (var arg in call.Arguments)
        {
            var a = LowerExpr(arg);
            if (a != null) args.Add(a);
        }

        var operands = new List<MirOperand>();
        if (callee != null) operands.Add(callee);
        operands.AddRange(args);

        var result = NewTemp(MirType.I64);
        Emit(new MirInstruction(MirOpcode.Call, result, operands));
        return result;
    }

    private MirOperand LowerBinary(HirBinaryExpr bin)
    {
        var left = LowerExpr(bin.Left)!;
        var right = LowerExpr(bin.Right)!;
        
        // Infer result type from operands (use left operand's type)
        // For comparison operators, result is always bool
        MirType resultType;
        switch (bin.Operator)
        {
            case BinaryOperator.Eq:
            case BinaryOperator.Ne:
            case BinaryOperator.Lt:
            case BinaryOperator.Le:
            case BinaryOperator.Gt:
            case BinaryOperator.Ge:
                resultType = MirType.Bool;
                break;
            default:
                resultType = left.Type;
                break;
        }
        
        var result = NewTemp(resultType);
        Emit(new MirInstruction(MirOpcode.BinaryOp, result, new[] { left, right }, bin.Operator));
        return result;
    }

    private MirOperand LowerUnary(HirUnaryExpr un)
    {
        var operand = LowerExpr(un.Operand)!;
        // Infer result type from operand
        var result = NewTemp(operand.Type);
        Emit(new MirInstruction(MirOpcode.UnaryOp, result, new[] { operand }, un.Operator));
        return result;
    }

    private MirOperand? LowerIf(HirIfExpr ifExpr)
    {
        var cond = LowerExpr(ifExpr.Condition)!;

        var thenBlock = _currentFunction!.CreateBlock("if.then");
        var elseBlock = _currentFunction.CreateBlock("if.else");
        var mergeBlock = _currentFunction.CreateBlock("if.merge");

        _currentBlock!.Terminator = new MirConditionalBranch(cond, thenBlock.Index, elseBlock.Index);

        _currentBlock = thenBlock;
        var thenResult = LowerBlock(ifExpr.ThenBranch);
        if (_currentBlock.Terminator == null)
            _currentBlock.Terminator = new MirBranch(mergeBlock.Index);

        _currentBlock = elseBlock;
        if (ifExpr.ElseBranch is HirBlock elBlock)
        {
            LowerBlock(elBlock);
        }
        else if (ifExpr.ElseBranch != null)
        {
            LowerExpr(ifExpr.ElseBranch);
        }
        if (_currentBlock.Terminator == null)
            _currentBlock.Terminator = new MirBranch(mergeBlock.Index);

        _currentBlock = mergeBlock;
        return thenResult;
    }

    private MirOperand? LowerAssign(HirAssignExpr assign)
    {
        var target = LowerExpr(assign.Target);
        var value = LowerExpr(assign.Value);
        if (target != null && value != null)
        {
            Emit(new MirInstruction(MirOpcode.Store, target, new[] { value }));
        }
        return null;
    }

    private MirType ResolveType(HirTypeRef? typeRef)
    {
        if (typeRef == null) return MirType.Void;
        return typeRef.Name switch
        {
            "i32" => MirType.I32,
            "i64" => MirType.I64,
            "f32" => MirType.F32,
            "f64" => MirType.F64,
            "bool" => MirType.Bool,
            "char" => MirType.Char,
            "String" => MirType.StringPtr,
            "void" => MirType.Void,
            _ => MirType.Ptr,
        };
    }

    private MirOperand NewTemp(MirType type) => MirOperand.Temp($"_t{_tempCounter++}", type);

    private void Emit(MirInstruction instruction) => _currentBlock!.AddInstruction(instruction);

    /// <summary>
    /// Coerce an operand to a target type if it's a constant with a compatible but different type.
    /// This is primarily for handling integer literals that default to i64 but need to be i32, etc.
    /// </summary>
    private MirOperand CoerceType(MirOperand operand, MirType targetType)
    {
        // If the operand is already the target type, no coercion needed
        if (operand.Type.Name == targetType.Name)
            return operand;

        // Only coerce constants
        if (operand.Kind != MirOperandKind.Constant)
            return operand;

        // Coerce integer types
        if (targetType.Name == "i32" && operand.Type.Name == "i64")
        {
            return MirOperand.Constant(operand.Value, MirType.I32);
        }
        if (targetType.Name == "i64" && operand.Type.Name == "i32")
        {
            return MirOperand.Constant(operand.Value, MirType.I64);
        }

        // Coerce float types
        if (targetType.Name == "f32" && operand.Type.Name == "f64")
        {
            return MirOperand.Constant(operand.Value, MirType.F32);
        }
        if (targetType.Name == "f64" && operand.Type.Name == "f32")
        {
            return MirOperand.Constant(operand.Value, MirType.F64);
        }

        // Default: return original operand
        return operand;
    }
}
