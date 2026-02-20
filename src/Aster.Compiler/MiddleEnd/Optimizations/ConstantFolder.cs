using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.Optimizations;

/// <summary>
/// Phase 3 optimization: constant folding.
/// Replaces binary and unary operations on compile-time constants with their results,
/// eliminating unnecessary computations in the generated LLVM IR.
/// </summary>
public sealed class ConstantFolder
{
    /// <summary>Apply constant folding to all functions in the module.</summary>
    public void Fold(MirModule module)
    {
        foreach (var fn in module.Functions)
            FoldFunction(fn);
    }

    private void FoldFunction(MirFunction fn)
    {
        foreach (var block in fn.BasicBlocks)
            FoldBlock(block);
    }

    private void FoldBlock(MirBasicBlock block)
    {
        for (int i = 0; i < block.Instructions.Count; i++)
        {
            var instr = block.Instructions[i];
            if (instr.Opcode == MirOpcode.BinaryOp && instr.Destination != null)
            {
                var folded = TryFoldBinary(instr);
                if (folded != null)
                    block.Instructions[i] = folded;
            }
            else if (instr.Opcode == MirOpcode.UnaryOp && instr.Destination != null)
            {
                var folded = TryFoldUnary(instr);
                if (folded != null)
                    block.Instructions[i] = folded;
            }
        }
    }

    private static MirInstruction? TryFoldBinary(MirInstruction instr)
    {
        if (instr.Operands.Count < 2) return null;
        var left = instr.Operands[0];
        var right = instr.Operands[1];

        if (left.Kind != MirOperandKind.Constant || right.Kind != MirOperandKind.Constant)
            return null;

        var op = instr.Extra?.ToString() ?? "";
        var result = EvalBinary(op, left.Value, right.Value, left.Type);
        if (result == null) return null;

        var constOperand = MirOperand.Constant(result, instr.Destination!.Type);
        return new MirInstruction(MirOpcode.Assign, instr.Destination, new[] { constOperand });
    }

    private static MirInstruction? TryFoldUnary(MirInstruction instr)
    {
        if (instr.Operands.Count < 1) return null;
        var operand = instr.Operands[0];

        if (operand.Kind != MirOperandKind.Constant) return null;

        var op = instr.Extra?.ToString() ?? "";
        var result = EvalUnary(op, operand.Value, operand.Type);
        if (result == null) return null;

        var constOperand = MirOperand.Constant(result, instr.Destination!.Type);
        return new MirInstruction(MirOpcode.Assign, instr.Destination, new[] { constOperand });
    }

    private static object? EvalBinary(string op, object? left, object? right, MirType type)
    {
        // Integer arithmetic
        if (TryGetLong(left, out long l) && TryGetLong(right, out long r))
        {
            return op switch
            {
                "add" => l + r,
                "sub" => l - r,
                "mul" => l * r,
                "div" when r != 0 => l / r,
                "rem" when r != 0 => l % r,
                "eq" => l == r,
                "ne" => l != r,
                "lt" => l < r,
                "le" => l <= r,
                "gt" => l > r,
                "ge" => l >= r,
                "and" => l & r,
                "or" => l | r,
                "xor" => l ^ r,
                _ => null,
            };
        }

        // Float arithmetic
        if (TryGetDouble(left, out double dl) && TryGetDouble(right, out double dr))
        {
            return op switch
            {
                "add" => dl + dr,
                "sub" => dl - dr,
                "mul" => dl * dr,
                "div" when dr != 0.0 => dl / dr,
                "eq" => dl == dr,
                "ne" => dl != dr,
                "lt" => dl < dr,
                "le" => dl <= dr,
                "gt" => dl > dr,
                "ge" => dl >= dr,
                _ => null,
            };
        }

        // Boolean logic
        if (left is bool lb && right is bool rb)
        {
            return op switch
            {
                "and" => lb && rb,
                "or" => lb || rb,
                "eq" => lb == rb,
                "ne" => lb != rb,
                _ => null,
            };
        }

        return null;
    }

    private static object? EvalUnary(string op, object? operand, MirType type)
    {
        if (TryGetLong(operand, out long l))
        {
            return op switch
            {
                "neg" => -l,
                "not" => ~l,
                _ => null,
            };
        }

        if (TryGetDouble(operand, out double d))
        {
            return op switch
            {
                "neg" => -d,
                _ => null,
            };
        }

        if (operand is bool b && op == "not")
            return !b;

        return null;
    }

    private static bool TryGetLong(object? value, out long result)
    {
        result = value switch
        {
            long l => l,
            int i => i,
            _ => 0,
        };
        return value is long or int;
    }

    private static bool TryGetDouble(object? value, out double result)
    {
        result = value switch
        {
            double d => d,
            float f => f,
            _ => 0.0,
        };
        return value is double or float;
    }
}
