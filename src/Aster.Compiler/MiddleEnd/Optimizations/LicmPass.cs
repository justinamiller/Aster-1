using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.Optimizations;

/// <summary>
/// Loop Invariant Code Motion (LICM) optimization pass.
///
/// Detects back-edges in the MIR CFG (an unconditional branch whose target block has
/// a lower index than the source — a proxy for a loop back-edge in the lowered IR).
/// For each back-edge, treats the target as the loop header and the blocks from header
/// up to (but not including) the source as the loop body.  Instructions whose operands
/// are all loop-invariant (constants or defined outside the loop) are hoisted to a
/// synthesised pre-header block inserted immediately before the loop header.
///
/// Phase 5: wired into the compiler pipeline after CSE.
/// </summary>
public sealed class LicmPass
{
    /// <summary>Run LICM on all functions in the module.  Returns true if anything changed.</summary>
    public bool Hoist(MirModule module)
    {
        bool changed = false;
        foreach (var fn in module.Functions)
            changed |= HoistFunction(fn);
        return changed;
    }

    private bool HoistFunction(MirFunction fn)
    {
        bool changed = false;

        // Find all back-edges: an unconditional branch from block[i] to block[j] where j <= i.
        // A self-loop (j == i) is also a back-edge.
        // We iterate from high to low so that inserting pre-header blocks doesn't shift indices
        // that we have already processed.
        for (int i = fn.BasicBlocks.Count - 1; i >= 0; i--)
        {
            var block = fn.BasicBlocks[i];
            if (block.Terminator is not MirBranch branch)
                continue;

            int headerIdx = branch.TargetBlock;
            if (headerIdx > i)
                continue; // forward branch — not a back-edge

            // Loop body: blocks [headerIdx .. i] inclusive.
            var loopBlocks = new HashSet<int>();
            for (int b = headerIdx; b <= i; b++)
                loopBlocks.Add(b);

            // Collect all variables defined inside the loop.
            var loopDefined = new HashSet<string>(StringComparer.Ordinal);
            foreach (int b in loopBlocks)
                foreach (var instr in fn.BasicBlocks[b].Instructions)
                    if (instr.Destination != null)
                        loopDefined.Add(instr.Destination.Name);

            // Find invariant instructions in the loop body (excluding the header since it
            // contains the loop-variant phi-like assignments).
            var toHoist = new List<(int BlockIdx, int InstrIdx, MirInstruction Instr)>();
            foreach (int b in loopBlocks)
            {
                var blk = fn.BasicBlocks[b];
                for (int j = 0; j < blk.Instructions.Count; j++)
                {
                    var instr = blk.Instructions[j];
                    if (IsInvariant(instr, loopDefined))
                        toHoist.Add((b, j, instr));
                }
            }

            if (toHoist.Count == 0)
                continue;

            // Create a pre-header block and insert it before the header.
            var preHeader = new MirBasicBlock($"licm_preheader_{headerIdx}", -1);
            foreach (var (_, _, instr) in toHoist)
                preHeader.Instructions.Add(instr);
            preHeader.Terminator = new MirBranch(headerIdx);

            // Insert pre-header at position headerIdx (shifting everything else).
            fn.BasicBlocks.Insert(headerIdx, preHeader);

            // Remove hoisted instructions from their original blocks (indices shifted by +1).
            // Group by block, remove in reverse order to preserve indices.
            var byBlock = toHoist.GroupBy(t => t.BlockIdx)
                                 .ToDictionary(g => g.Key, g => g.Select(t => t.InstrIdx).OrderByDescending(x => x).ToList());
            foreach (var (blkIdx, instrIndices) in byBlock)
            {
                var blk = fn.BasicBlocks[blkIdx + 1]; // +1 because we inserted pre-header
                foreach (int idx in instrIndices)
                    blk.Instructions.RemoveAt(idx);
            }

            // Fix up all branch targets that pointed to or past headerIdx.
            // The pre-header is now at headerIdx; the former header is at headerIdx+1.
            FixBranchTargets(fn, headerIdx, preHeaderIdx: headerIdx);

            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// After inserting a pre-header at <paramref name="preHeaderIdx"/>, every block
    /// that previously branched to <c>preHeaderIdx</c> (the old header) must now
    /// branch to the pre-header instead — except the back-edge block itself and the
    /// pre-header's own terminator which already point correctly.
    ///
    /// Additionally, all blocks whose index is &gt; <paramref name="preHeaderIdx"/> have
    /// shifted up by one, so branch targets that were ≥ preHeaderIdx+1 must be incremented.
    /// </summary>
    private static void FixBranchTargets(MirFunction fn, int oldHeaderIdx, int preHeaderIdx)
    {
        int newHeaderIdx = oldHeaderIdx + 1;

        for (int b = 0; b < fn.BasicBlocks.Count; b++)
        {
            var blk = fn.BasicBlocks[b];
            if (blk.Terminator == null) continue;

            blk.Terminator = blk.Terminator switch
            {
                MirBranch br => new MirBranch(ShiftTarget(br.TargetBlock, oldHeaderIdx, newHeaderIdx, b, preHeaderIdx)),
                MirConditionalBranch cb => new MirConditionalBranch(
                    cb.Condition,
                    ShiftTarget(cb.TrueBlock, oldHeaderIdx, newHeaderIdx, b, preHeaderIdx),
                    ShiftTarget(cb.FalseBlock, oldHeaderIdx, newHeaderIdx, b, preHeaderIdx)),
                _ => blk.Terminator,
            };
        }
    }

    private static int ShiftTarget(int target, int oldHeader, int newHeader, int sourceBlock, int preHeader)
    {
        // The pre-header block is at preHeader (== oldHeader).
        // The old header is now at newHeader (== oldHeader+1).
        // Blocks that were ≥ newHeader have all shifted up by one.
        // Exception: back-edge block should still jump to old header (now newHeader).
        if (target == oldHeader && sourceBlock != preHeader)
            return preHeader;   // redirect to pre-header
        if (target >= newHeader)
            return target + 1;  // shift for inserted block
        return target;
    }

    private static bool IsInvariant(MirInstruction instr, HashSet<string> loopDefined)
    {
        // Only hoist pure, side-effect-free operations.
        if (instr.Opcode != MirOpcode.BinaryOp && instr.Opcode != MirOpcode.UnaryOp)
            return false;
        if (instr.Destination == null)
            return false;

        foreach (var op in instr.Operands)
        {
            if (op.Kind == MirOperandKind.Constant)
                continue; // constants are always invariant
            if (op.Kind == MirOperandKind.Variable && !loopDefined.Contains(op.Name))
                continue; // defined outside the loop → invariant
            return false; // defined inside the loop → variant
        }
        return true;
    }
}
