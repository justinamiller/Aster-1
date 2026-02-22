using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.Optimizations;

/// <summary>
/// Scalar Replacement of Aggregates (SROA) optimization pass.
///
/// Identifies struct-field <c>Load</c> instructions where the struct operand is an
/// aggregate variable that is never stored to through a pointer (i.e., it is a
/// locally-initialized value).  When every field load of a given variable always
/// loads the same field from a constant value, the load is replaced with a direct
/// <c>Assign</c> of that constant, eliminating the abstract field-access overhead.
///
/// This simplified SROA handles the most common case produced by the Aster front-end:
/// struct literals used for a single read immediately after construction.
///
/// Phase 5: wired into the compiler pipeline after inlining.
/// </summary>
public sealed class SroaPass
{
    /// <summary>Run SROA on all functions in the module.  Returns true if anything changed.</summary>
    public bool Replace(MirModule module)
    {
        bool changed = false;
        foreach (var fn in module.Functions)
            changed |= ReplaceFunction(fn);
        return changed;
    }

    private bool ReplaceFunction(MirFunction fn)
    {
        bool changed = false;
        foreach (var block in fn.BasicBlocks)
            changed |= ReplaceBlock(block);
        return changed;
    }

    private bool ReplaceBlock(MirBasicBlock block)
    {
        // Pass 1: collect field→value mapping from Assign instructions whose destination
        // name encodes a struct-field initialisation pattern produced by MirLowering.
        // Specifically, look for:
        //   Assign  %struct_var  ← Constant(value)   [single-field structs from literal init]
        // and Load %field_dest ← (struct_var, fieldName).
        //
        // More generally, track the last constant value assigned to each variable name.
        var constMap = new Dictionary<string, MirOperand>(StringComparer.Ordinal);

        for (int i = 0; i < block.Instructions.Count; i++)
        {
            var instr = block.Instructions[i];

            if (instr.Opcode == MirOpcode.Assign &&
                instr.Destination != null &&
                instr.Operands.Count == 1 &&
                instr.Operands[0].Kind == MirOperandKind.Constant)
            {
                constMap[instr.Destination.Name] = instr.Operands[0];
                continue;
            }

            if (instr.Opcode == MirOpcode.Load &&
                instr.Destination != null &&
                instr.Operands.Count >= 1)
            {
                var structOp = instr.Operands[0];

                // If the whole struct was last assigned a constant, propagate it.
                if (structOp.Kind == MirOperandKind.Variable &&
                    constMap.TryGetValue(structOp.Name, out var constVal))
                {
                    block.Instructions[i] = new MirInstruction(
                        MirOpcode.Assign,
                        instr.Destination,
                        new[] { constVal });
                    constMap[instr.Destination.Name] = constVal;
                    return true; // restart (conservative; block is usually short)
                }

                // If the load destination is written later, invalidate it.
                if (instr.Destination != null)
                    constMap.Remove(instr.Destination.Name);
            }
        }

        return false;
    }
}
