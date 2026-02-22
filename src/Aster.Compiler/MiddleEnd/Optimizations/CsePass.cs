using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.Optimizations;

/// <summary>
/// Local Common Subexpression Elimination (CSE) pass.
/// Within each basic block, replaces redundant pure computations with copies of
/// earlier results. This reduces instruction count and register pressure.
///
/// Phase 4: wired into the compiler pipeline after constant folding and DCE.
/// </summary>
public sealed class CsePass
{
    /// <summary>Run CSE on all functions in the module.</summary>
    public bool Eliminate(MirModule module)
    {
        bool changed = false;
        foreach (var fn in module.Functions)
            changed |= EliminateFunction(fn);
        return changed;
    }

    private bool EliminateFunction(MirFunction fn)
    {
        bool changed = false;
        foreach (var block in fn.BasicBlocks)
            changed |= EliminateBlock(block);
        return changed;
    }

    /// <summary>
    /// Within one block, maintain a table of "expression signature → first result operand".
    /// When the same pure expression is computed a second time, replace it with a copy.
    /// </summary>
    private bool EliminateBlock(MirBasicBlock block)
    {
        bool changed = false;
        // Map: expression signature → first result variable
        var available = new Dictionary<string, MirOperand>(StringComparer.Ordinal);

        for (int i = 0; i < block.Instructions.Count; i++)
        {
            var instr = block.Instructions[i];

            // For impure ops, just invalidate any cached expressions that depended on the destination
            if (!IsPure(instr))
            {
                if (instr.Destination != null)
                    Invalidate(available, instr.Destination.Name);
                continue;
            }

            var sig = ComputeSignature(instr);
            if (sig == null)
            {
                if (instr.Destination != null)
                    Invalidate(available, instr.Destination.Name);
                continue;
            }

            if (available.TryGetValue(sig, out var prior) && instr.Destination != null)
            {
                // Replace this computation with an assignment from the prior result
                block.Instructions[i] = new MirInstruction(
                    MirOpcode.Assign,
                    instr.Destination,
                    new[] { prior });
                changed = true;
                // Destination now holds same value as prior; invalidate any downstream uses of old dest
                Invalidate(available, instr.Destination.Name);
            }
            else
            {
                // First time computing this expression: invalidate old entries for this destination,
                // then record the new computation.
                if (instr.Destination != null)
                    Invalidate(available, instr.Destination.Name);

                if (instr.Destination != null)
                    available[sig] = instr.Destination;
            }
        }

        return changed;
    }

    private static bool IsPure(MirInstruction instr) => instr.Opcode switch
    {
        MirOpcode.BinaryOp  => true,
        MirOpcode.UnaryOp   => true,
        MirOpcode.Literal   => true,
        MirOpcode.Load      => true, // conservative: assume no aliasing
        _                   => false,
    };

    /// <summary>Build a canonical string key for an instruction's computation.</summary>
    private static string? ComputeSignature(MirInstruction instr)
    {
        var parts = new System.Text.StringBuilder();
        parts.Append(instr.Opcode);

        if (instr.Extra != null)
        {
            parts.Append('|');
            parts.Append(instr.Extra);
        }

        foreach (var op in instr.Operands)
        {
            parts.Append('|');
            switch (op.Kind)
            {
                case MirOperandKind.Variable: parts.Append("v:"); parts.Append(op.Name); break;
                case MirOperandKind.Constant: parts.Append("c:"); parts.Append(op.Value); break;
                default: return null; // cannot hash this operand kind
            }
        }

        return parts.ToString();
    }

    /// <summary>Remove all cached expressions whose result or inputs include <paramref name="varName"/>.</summary>
    /// <remarks>
    /// Uses string matching on the signature. For the block-local scope of CSE this is efficient;
    /// a reverse index from variable → signatures would improve worst-case scaling for very large
    /// basic blocks (future optimization).
    /// </remarks>
    private static void Invalidate(Dictionary<string, MirOperand> available, string varName)
    {
        var toRemove = new List<string>();
        foreach (var (sig, result) in available)
        {
            if (result.Name == varName || sig.Contains($"|v:{varName}"))
                toRemove.Add(sig);
        }
        foreach (var key in toRemove)
            available.Remove(key);
    }
}
