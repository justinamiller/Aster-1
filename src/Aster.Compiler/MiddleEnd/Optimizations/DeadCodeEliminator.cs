using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.Optimizations;

/// <summary>
/// Phase 3 optimization: dead code elimination (DCE).
/// Removes:
/// 1. Unreachable basic blocks (blocks with no predecessors after the entry block).
/// 2. Unused temporary assignments (instructions whose destination is never read).
/// </summary>
public sealed class DeadCodeEliminator
{
    /// <summary>Apply DCE to all functions in the module.</summary>
    public void Eliminate(MirModule module)
    {
        foreach (var fn in module.Functions)
            EliminateFunction(fn);
    }

    private void EliminateFunction(MirFunction fn)
    {
        if (fn.BasicBlocks.Count == 0) return;

        RemoveUnreachableBlocks(fn);
        RemoveDeadAssignments(fn);
    }

    /// <summary>Remove basic blocks that cannot be reached from the entry block.</summary>
    private static void RemoveUnreachableBlocks(MirFunction fn)
    {
        var reachable = new HashSet<int>();
        var worklist = new Queue<int>();
        worklist.Enqueue(0);
        reachable.Add(0);

        while (worklist.Count > 0)
        {
            var idx = worklist.Dequeue();
            if (idx >= fn.BasicBlocks.Count) continue;

            foreach (var successor in GetSuccessors(fn.BasicBlocks[idx]))
            {
                if (reachable.Add(successor))
                    worklist.Enqueue(successor);
            }
        }

        // Remove unreachable blocks (iterate backwards to preserve indices)
        for (int i = fn.BasicBlocks.Count - 1; i >= 1; i--)
        {
            if (!reachable.Contains(i))
                fn.BasicBlocks.RemoveAt(i);
        }
    }

    /// <summary>Remove assignments to temporaries that are never used.</summary>
    private static void RemoveDeadAssignments(MirFunction fn)
    {
        // Collect all temporaries that are read
        // Temp names are generated as "_tN" (ordinal case-sensitive) — ordinal comparison is correct and faster
        var usedTemps = new HashSet<string>(StringComparer.Ordinal);

        foreach (var block in fn.BasicBlocks)
        {
            foreach (var instr in block.Instructions)
            {
                foreach (var op in instr.Operands)
                {
                    if (op.Kind == MirOperandKind.Temp && op.Name != null)
                        usedTemps.Add(op.Name);
                }
            }

            // Count reads from terminators
            switch (block.Terminator)
            {
                case MirConditionalBranch cb when cb.Condition.Kind == MirOperandKind.Temp && cb.Condition.Name != null:
                    usedTemps.Add(cb.Condition.Name);
                    break;
                case MirReturn ret when ret.Value?.Kind == MirOperandKind.Temp && ret.Value.Name != null:
                    usedTemps.Add(ret.Value.Name);
                    break;
            }
        }

        // Remove instructions that only write to unused temps
        foreach (var block in fn.BasicBlocks)
        {
            block.Instructions.RemoveAll(instr =>
                instr.Destination != null &&
                instr.Destination.Kind == MirOperandKind.Temp &&
                instr.Destination.Name != null &&
                !usedTemps.Contains(instr.Destination.Name) &&
                IsSideEffectFree(instr));
        }
    }

    /// <summary>Returns true if this instruction has no observable side effects.</summary>
    private static bool IsSideEffectFree(MirInstruction instr) =>
        instr.Opcode is MirOpcode.Assign or MirOpcode.BinaryOp or MirOpcode.UnaryOp or MirOpcode.Load;

    /// <summary>Get successor block indices for a basic block.</summary>
    private static IEnumerable<int> GetSuccessors(MirBasicBlock block)
    {
        return block.Terminator switch
        {
            MirBranch br => new[] { br.TargetBlock },
            MirConditionalBranch cb => new[] { cb.TrueBlock, cb.FalseBlock },
            MirSwitch sw => sw.Cases.Select(c => c.Block).Append(sw.DefaultBlock),
            _ => Array.Empty<int>(),
        };
    }
}
