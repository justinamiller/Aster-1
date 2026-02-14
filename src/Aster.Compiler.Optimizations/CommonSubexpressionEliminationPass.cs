using Aster.Compiler.Analysis;
using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Optimizations;

/// <summary>
/// Common Subexpression Elimination (CSE) pass.
/// Eliminates redundant computations by reusing previously computed values.
/// This is a local (within-block) CSE implementation.
/// </summary>
public sealed class CommonSubexpressionEliminationPass : IOptimizationPass
{
    public string Name => "CommonSubexpressionElimination";

    public bool Run(MirFunction function, PassContext context)
    {
        context.Metrics.StartTiming();

        bool changed = false;

        foreach (var block in function.BasicBlocks)
        {
            changed |= EliminateInBlock(block, context);
        }

        context.Metrics.StopTiming();
        return changed;
    }

    private bool EliminateInBlock(MirBasicBlock block, PassContext context)
    {
        bool changed = false;
        var availableExpressions = new Dictionary<string, MirOperand>();

        for (int i = 0; i < block.Instructions.Count; i++)
        {
            var instr = block.Instructions[i];

            // Only handle pure operations
            if (!IsPureOperation(instr))
            {
                // Invalidate expressions that depend on this destination
                if (instr.Destination != null)
                {
                    InvalidateDependentExpressions(availableExpressions, instr.Destination.Name);
                }
                continue;
            }

            // Compute expression signature
            var signature = ComputeSignature(instr);
            
            if (signature != null && availableExpressions.TryGetValue(signature, out var existingResult))
            {
                // Replace with copy from existing result
                block.Instructions[i] = new MirInstruction(
                    MirOpcode.Assign,
                    instr.Destination,
                    new[] { existingResult }
                );
                context.Metrics.InstructionsRemoved++;
                changed = true;
            }
            else if (signature != null && instr.Destination != null)
            {
                // Record this expression
                availableExpressions[signature] = instr.Destination;
            }

            // Invalidate when destination is redefined
            if (instr.Destination != null)
            {
                InvalidateDependentExpressions(availableExpressions, instr.Destination.Name);
            }
        }

        return changed;
    }

    private static bool IsPureOperation(MirInstruction instr)
    {
        return instr.Opcode switch
        {
            MirOpcode.BinaryOp => true,
            MirOpcode.UnaryOp => true,
            MirOpcode.Literal => true,
            MirOpcode.Load => true,  // Assuming no aliasing
            _ => false
        };
    }

    private static string? ComputeSignature(MirInstruction instr)
    {
        var parts = new List<string>
        {
            instr.Opcode.ToString()
        };

        if (instr.Extra != null)
        {
            parts.Add(instr.Extra.ToString() ?? "");
        }

        foreach (var operand in instr.Operands)
        {
            if (operand.Kind == MirOperandKind.Variable)
            {
                parts.Add($"var:{operand.Name}");
            }
            else if (operand.Kind == MirOperandKind.Constant)
            {
                parts.Add($"const:{operand.Value}");
            }
            else
            {
                return null; // Can't hash this expression
            }
        }

        return string.Join("|", parts);
    }

    private static void InvalidateDependentExpressions(Dictionary<string, MirOperand> expressions, string varName)
    {
        var toRemove = new List<string>();
        foreach (var (sig, result) in expressions)
        {
            if (result.Name == varName || sig.Contains($"var:{varName}"))
            {
                toRemove.Add(sig);
            }
        }

        foreach (var sig in toRemove)
        {
            expressions.Remove(sig);
        }
    }
}
