using System.Collections.Generic;
using System.Linq;
using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.Optimizations;

/// <summary>
/// Phase 6: Loop Unrolling pass.
///
/// Strategy:
///   Detect "counted" loops — blocks that contain a <c>__range_new(lo, hi)</c> Call with
///   constant integer arguments where trip count ≤ <see cref="MaxUnrollCount"/>.
///   Clone the loop body (trip count − 1) times, renaming temporaries to avoid SSA
///   conflicts, then remove the back-edge branch.
///
/// Limitations (by design — keep the pass simple):
///   • Only constant-bound ranges: <c>lo..hi</c> where both lo and hi are integer literals.
///   • Unrolls at most <see cref="MaxUnrollCount"/> = 8 iterations.
///   • Falls back gracefully (leaves the loop unchanged) when any pre-condition is not met.
/// </summary>
public sealed class LoopUnrollPass
{
    private const int MaxUnrollCount = 8;

    public MirModule Run(MirModule module)
    {
        var result = new MirModule(module.Name);
        foreach (var fn in module.Functions)
            result.Functions.Add(UnrollFunction(fn));
        return result;
    }

    private static MirFunction UnrollFunction(MirFunction fn)
    {
        if (fn.BasicBlocks.Count < 2)
            return fn;

        var outFn = new MirFunction(fn.Name);
        outFn.ReturnType = fn.ReturnType;
        foreach (var p in fn.Parameters)
            outFn.Parameters.Add(p);

        var workBlocks = fn.BasicBlocks.ToList();
        bool changed = true;

        while (changed)
        {
            changed = false;
            for (int i = 0; i < workBlocks.Count; i++)
            {
                if (TryGetConstantRange(workBlocks[i], out var lo, out var hi))
                {
                    int tripCount = hi - lo;
                    if (tripCount <= 0 || tripCount > MaxUnrollCount)
                        continue;

                    int headerIdx = FindLoopHeader(workBlocks, i);
                    if (headerIdx < 0)
                        continue;

                    // Collect body blocks (inclusive of header through back-edge block)
                    var bodyBlocks = workBlocks.Skip(headerIdx).Take(i - headerIdx + 1).ToList();

                    // Build unrolled replacement: tripCount copies of the body, each with a unique
                    // iteration suffix.  Indices here are provisional; ReindexBlocks() corrects them
                    // after the function is fully assembled.
                    var unrolled = new List<MirBasicBlock>();
                    for (int iter = 0; iter < tripCount; iter++)
                    {
                        foreach (var bb in bodyBlocks)
                        {
                            // Provisional index: headerIdx + position in unrolled list.
                            // ReindexBlocks() will canonicalise all indices afterwards.
                            int provisionalIdx = headerIdx + unrolled.Count;
                            var newBb = new MirBasicBlock($"{bb.Label}_ur{iter}", provisionalIdx);
                            foreach (var instr in bb.Instructions)
                                // Note: back-edge label strings in instruction Extra are metadata
                                // only (control flow is carried by Terminators, not Extra).
                                // They are intentionally left unrenamed — downstream passes do not
                                // use them for control-flow resolution.
                                newBb.Instructions.Add(CloneInstruction(instr, $"_ur{iter}"));
                            unrolled.Add(newBb);
                        }
                    }

                    workBlocks.RemoveRange(headerIdx, i - headerIdx + 1);
                    workBlocks.InsertRange(headerIdx, unrolled);
                    changed = true;
                    break;
                }
            }
        }

        foreach (var bb in workBlocks)
            outFn.BasicBlocks.Add(bb);

        // Re-index all blocks so their .Index matches their position — the borrow checker
        // uses block.Index to index into CFG arrays of size BasicBlocks.Count.
        ReindexBlocks(outFn);

        return outFn;
    }

    /// <summary>Reassign block indices to be sequential [0, 1, 2, …].</summary>
    private static void ReindexBlocks(MirFunction fn)
    {
        // MirBasicBlock.Index has no setter, so we re-create each block with the correct index.
        var reindexed = new List<MirBasicBlock>();
        for (int idx = 0; idx < fn.BasicBlocks.Count; idx++)
        {
            var old = fn.BasicBlocks[idx];
            if (old.Index == idx)
            {
                reindexed.Add(old);
            }
            else
            {
                var nb = new MirBasicBlock(old.Label, idx);
                foreach (var instr in old.Instructions)
                    nb.Instructions.Add(instr);
                nb.Terminator = old.Terminator;
                reindexed.Add(nb);
            }
        }
        fn.BasicBlocks.Clear();
        foreach (var bb in reindexed)
            fn.BasicBlocks.Add(bb);
    }

    /// <summary>
    /// Check whether a block contains a constant-bound range call <c>__range_new(lo, hi)</c>.
    /// </summary>
    private static bool TryGetConstantRange(MirBasicBlock block, out int lo, out int hi)
    {
        lo = 0; hi = 0;
        foreach (var instr in block.Instructions)
        {
            if (instr.Opcode == MirOpcode.Call &&
                instr.Extra is string name && name == "__range_new" &&
                instr.Operands.Count == 2 &&
                instr.Operands[0].Kind == MirOperandKind.Constant &&
                instr.Operands[1].Kind == MirOperandKind.Constant)
            {
                try
                {
                    lo = System.Convert.ToInt32(instr.Operands[0].Value);
                    hi = System.Convert.ToInt32(instr.Operands[1].Value);
                    return true;
                }
                catch { return false; }
            }
        }
        return false;
    }

    /// <summary>Find the index of the loop header by looking for a branch back-edge target.</summary>
    private static int FindLoopHeader(IReadOnlyList<MirBasicBlock> blocks, int backEdgeIdx)
    {
        var block = blocks[backEdgeIdx];
        if (block.Instructions.Count == 0) return -1;

        var last = block.Instructions[^1];
        if (last.Extra is string targetLabel)
        {
            for (int j = 0; j < backEdgeIdx; j++)
                if (blocks[j].Label == targetLabel)
                    return j;
        }
        return backEdgeIdx > 0 ? backEdgeIdx - 1 : -1;
    }

    private static MirInstruction CloneInstruction(MirInstruction instr, string suffix)
    {
        var dest = instr.Destination != null ? RenameOp(instr.Destination, suffix) : null;
        var ops = instr.Operands.Select(o => RenameOp(o, suffix)).ToList();
        // Do NOT copy the "__range_new" extra so unrolled blocks are not re-unrolled.
        var extra = instr.Extra is string s && s == "__range_new" ? null : instr.Extra;
        return new MirInstruction(instr.Opcode, dest, ops, extra);
    }

    private static MirOperand RenameOp(MirOperand op, string suffix) =>
        op.Kind == MirOperandKind.Temp ? MirOperand.Temp(op.Name + suffix, op.Type) : op;
}
