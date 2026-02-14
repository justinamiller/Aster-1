using Aster.Compiler.Analysis;
using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Optimizations;

/// <summary>
/// CFG Simplification pass.
/// Removes unreachable blocks and merges trivial blocks.
/// </summary>
public sealed class SimplifyCfgPass : IOptimizationPass
{
    public string Name => "SimplifyCFG";

    public bool Run(MirFunction function, PassContext context)
    {
        context.Metrics.StartTiming();

        bool changed = false;
        changed |= RemoveUnreachableBlocks(function, context);
        changed |= MergeTrivialBlocks(function, context);

        context.Metrics.StopTiming();
        return changed;
    }

    private bool RemoveUnreachableBlocks(MirFunction function, PassContext context)
    {
        var cfg = ControlFlowGraph.Build(function);
        var reachable = new HashSet<int>();
        var queue = new Queue<CfgNode>();

        // BFS from entry
        if (cfg.Nodes.Count > 0)
        {
            queue.Enqueue(cfg.Entry);
            reachable.Add(cfg.Entry.BlockIndex);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var succ in node.Successors)
                {
                    if (reachable.Add(succ.BlockIndex))
                    {
                        queue.Enqueue(succ);
                    }
                }
            }
        }

        // Remove unreachable blocks
        var toRemove = new List<int>();
        for (int i = 0; i < function.BasicBlocks.Count; i++)
        {
            if (!reachable.Contains(i))
            {
                toRemove.Add(i);
            }
        }

        if (toRemove.Count > 0)
        {
            // Remove in reverse order to maintain indices
            for (int i = toRemove.Count - 1; i >= 0; i--)
            {
                function.BasicBlocks.RemoveAt(toRemove[i]);
                context.Metrics.BlocksRemoved++;
            }
            return true;
        }

        return false;
    }

    private bool MergeTrivialBlocks(MirFunction function, PassContext context)
    {
        bool changed = false;
        var cfg = ControlFlowGraph.Build(function);

        for (int i = 0; i < function.BasicBlocks.Count; i++)
        {
            var block = function.BasicBlocks[i];

            // Can merge if:
            // 1. Block has no instructions (only terminator)
            // 2. Block has only one successor (unconditional branch)
            if (block.Instructions.Count == 0 &&
                block.Terminator is MirBranch branch)
            {
                // Find predecessors
                var predecessors = cfg.GetPredecessors(cfg.Nodes[i]).ToList();
                
                // Update all predecessors to point directly to successor
                foreach (var pred in predecessors)
                {
                    UpdateTerminator(pred.Block, i, branch.TargetBlock);
                }

                if (predecessors.Count > 0)
                {
                    context.Metrics.BlocksMerged++;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private void UpdateTerminator(MirBasicBlock block, int oldTarget, int newTarget)
    {
        if (block.Terminator is MirBranch branch && branch.TargetBlock == oldTarget)
        {
            block.Terminator = new MirBranch(newTarget);
        }
        else if (block.Terminator is MirConditionalBranch condBranch)
        {
            var newTrueBlock = condBranch.TrueBlock == oldTarget ? newTarget : condBranch.TrueBlock;
            var newFalseBlock = condBranch.FalseBlock == oldTarget ? newTarget : condBranch.FalseBlock;
            
            if (newTrueBlock != condBranch.TrueBlock || newFalseBlock != condBranch.FalseBlock)
            {
                block.Terminator = new MirConditionalBranch(condBranch.Condition, newTrueBlock, newFalseBlock);
            }
        }
        else if (block.Terminator is MirSwitch switchTerm)
        {
            var newCases = switchTerm.Cases.Select(c => 
                c.Block == oldTarget ? (c.Value, newTarget) : c
            ).ToList();
            
            var newDefaultBlock = switchTerm.DefaultBlock == oldTarget ? newTarget : switchTerm.DefaultBlock;
            
            block.Terminator = new MirSwitch(switchTerm.Scrutinee, newCases, newDefaultBlock);
        }
    }
}
