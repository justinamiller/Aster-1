using Aster.Compiler.Analysis;
using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Optimizations;

/// <summary>
/// Dead Code Elimination (DCE) pass.
/// Removes instructions that compute values that are never used.
/// </summary>
public sealed class DeadCodeEliminationPass : IOptimizationPass
{
    public string Name => "DeadCodeElimination";

    public bool Run(MirFunction function, PassContext context)
    {
        context.Metrics.StartTiming();

        bool changed = false;
        var livenessAnalysis = new LivenessAnalysis(function);
        var liveness = livenessAnalysis.Analyze();
        var defUse = DefUseChains.Build(function);

        // Remove dead instructions
        for (int blockIdx = 0; blockIdx < function.BasicBlocks.Count; blockIdx++)
        {
            var block = function.BasicBlocks[blockIdx];
            var toRemove = new List<int>();

            for (int instrIdx = 0; instrIdx < block.Instructions.Count; instrIdx++)
            {
                var instr = block.Instructions[instrIdx];

                // Skip instructions with side effects
                if (HasSideEffects(instr))
                    continue;

                // If the destination is never used, this is dead code
                if (instr.Destination != null && 
                    instr.Destination.Kind == MirOperandKind.Variable)
                {
                    var destName = instr.Destination.Name;
                    if (!defUse.IsUsed(destName) && 
                        !livenessAnalysis.IsLiveOut(blockIdx, destName))
                    {
                        toRemove.Add(instrIdx);
                    }
                }
            }

            // Remove instructions in reverse order to maintain indices
            for (int i = toRemove.Count - 1; i >= 0; i--)
            {
                block.Instructions.RemoveAt(toRemove[i]);
                context.Metrics.InstructionsRemoved++;
                changed = true;
            }
        }

        context.Metrics.StopTiming();
        return changed;
    }

    private static bool HasSideEffects(MirInstruction instr)
    {
        return instr.Opcode switch
        {
            MirOpcode.Store => true,
            MirOpcode.Call => true,
            MirOpcode.Drop => true,
            _ => false
        };
    }
}
