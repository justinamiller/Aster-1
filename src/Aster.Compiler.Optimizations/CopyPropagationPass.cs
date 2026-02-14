using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Optimizations;

/// <summary>
/// Copy Propagation pass.
/// Replaces uses of copied variables with the original value.
/// Example: x = y; z = x  =>  z = y
/// </summary>
public sealed class CopyPropagationPass : IOptimizationPass
{
    public string Name => "CopyPropagation";

    public bool Run(MirFunction function, PassContext context)
    {
        context.Metrics.StartTiming();

        bool changed = false;
        var copyMap = new Dictionary<string, MirOperand>();

        foreach (var block in function.BasicBlocks)
        {
            copyMap.Clear();

            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var instr = block.Instructions[i];

                // Propagate copies in operands
                bool instrChanged = false;
                var newOperands = new List<MirOperand>();

                foreach (var operand in instr.Operands)
                {
                    if (operand.Kind == MirOperandKind.Variable && copyMap.TryGetValue(operand.Name, out var replacement))
                    {
                        newOperands.Add(replacement);
                        instrChanged = true;
                    }
                    else
                    {
                        newOperands.Add(operand);
                    }
                }

                if (instrChanged)
                {
                    block.Instructions[i] = new MirInstruction(
                        instr.Opcode,
                        instr.Destination,
                        newOperands,
                        instr.Extra
                    );
                    changed = true;
                }

                // Track copy assignments: x = y
                if (instr.Opcode == MirOpcode.Assign &&
                    instr.Destination != null &&
                    instr.Operands.Count == 1 &&
                    instr.Operands[0].Kind == MirOperandKind.Variable)
                {
                    copyMap[instr.Destination.Name] = instr.Operands[0];
                }
                // Invalidate copies when variable is redefined
                else if (instr.Destination != null)
                {
                    var toRemove = copyMap.Where(kv => kv.Value.Name == instr.Destination.Name).Select(kv => kv.Key).ToList();
                    foreach (var key in toRemove)
                    {
                        copyMap.Remove(key);
                    }
                }
            }
        }

        context.Metrics.StopTiming();
        return changed;
    }
}
