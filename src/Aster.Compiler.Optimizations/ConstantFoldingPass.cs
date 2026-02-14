using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Optimizations;

/// <summary>
/// Constant Folding and Propagation pass.
/// Evaluates constant expressions at compile time.
/// </summary>
public sealed class ConstantFoldingPass : IOptimizationPass
{
    public string Name => "ConstantFolding";

    public bool Run(MirFunction function, PassContext context)
    {
        context.Metrics.StartTiming();

        bool changed = false;

        foreach (var block in function.BasicBlocks)
        {
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var instr = block.Instructions[i];

                if (instr.Opcode == MirOpcode.BinaryOp && 
                    instr.Operands.Count == 2 &&
                    instr.Operands[0].Kind == MirOperandKind.Constant &&
                    instr.Operands[1].Kind == MirOperandKind.Constant)
                {
                    // Try to fold binary operation
                    var folded = TryFoldBinaryOp(instr);
                    if (folded != null)
                    {
                        block.Instructions[i] = folded;
                        context.Metrics.InstructionsRemoved++;
                        changed = true;
                    }
                }
                else if (instr.Opcode == MirOpcode.UnaryOp &&
                         instr.Operands.Count == 1 &&
                         instr.Operands[0].Kind == MirOperandKind.Constant)
                {
                    // Try to fold unary operation
                    var folded = TryFoldUnaryOp(instr);
                    if (folded != null)
                    {
                        block.Instructions[i] = folded;
                        context.Metrics.InstructionsRemoved++;
                        changed = true;
                    }
                }
            }
        }

        context.Metrics.StopTiming();
        return changed;
    }

    private static MirInstruction? TryFoldBinaryOp(MirInstruction instr)
    {
        if (instr.Extra is not string op)
            return null;

        var left = instr.Operands[0].Value;
        var right = instr.Operands[1].Value;

        if (left == null || right == null)
            return null;

        object? result = null;

        // Integer operations
        if (left is int leftInt && right is int rightInt)
        {
            result = op switch
            {
                "+" => leftInt + rightInt,
                "-" => leftInt - rightInt,
                "*" => leftInt * rightInt,
                "/" => rightInt != 0 ? leftInt / rightInt : null,
                "%" => rightInt != 0 ? leftInt % rightInt : null,
                "==" => leftInt == rightInt,
                "!=" => leftInt != rightInt,
                "<" => leftInt < rightInt,
                "<=" => leftInt <= rightInt,
                ">" => leftInt > rightInt,
                ">=" => leftInt >= rightInt,
                "&" => leftInt & rightInt,
                "|" => leftInt | rightInt,
                "^" => leftInt ^ rightInt,
                _ => null
            };
        }
        // Long operations
        else if (left is long leftLong && right is long rightLong)
        {
            result = op switch
            {
                "+" => leftLong + rightLong,
                "-" => leftLong - rightLong,
                "*" => leftLong * rightLong,
                "/" => rightLong != 0 ? leftLong / rightLong : null,
                "%" => rightLong != 0 ? leftLong % rightLong : null,
                _ => null
            };
        }
        // Boolean operations
        else if (left is bool leftBool && right is bool rightBool)
        {
            result = op switch
            {
                "&&" => leftBool && rightBool,
                "||" => leftBool || rightBool,
                "==" => leftBool == rightBool,
                "!=" => leftBool != rightBool,
                _ => null
            };
        }

        if (result == null)
            return null;

        // Create literal instruction with folded constant
        var constant = MirOperand.Constant(result, instr.Destination?.Type ?? MirType.I32);
        return new MirInstruction(
            MirOpcode.Literal,
            instr.Destination,
            new[] { constant }
        );
    }

    private static MirInstruction? TryFoldUnaryOp(MirInstruction instr)
    {
        if (instr.Extra is not string op)
            return null;

        var operand = instr.Operands[0].Value;
        if (operand == null)
            return null;

        object? result = op switch
        {
            "-" when operand is int i => -i,
            "-" when operand is long l => -l,
            "!" when operand is bool b => !b,
            "~" when operand is int i => ~i,
            _ => null
        };

        if (result == null)
            return null;

        var constant = MirOperand.Constant(result, instr.Destination?.Type ?? MirType.I32);
        return new MirInstruction(
            MirOpcode.Literal,
            instr.Destination,
            new[] { constant }
        );
    }
}
