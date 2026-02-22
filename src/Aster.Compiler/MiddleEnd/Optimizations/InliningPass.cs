using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.Optimizations;

/// <summary>
/// Function inlining optimization pass.
///
/// Identifies "small" functions (at most <see cref="MaxInlineInstructions"/> non-terminator
/// instructions across all basic blocks) that are not recursive and have a single basic block.
/// At each call site whose callee is an inlinable function, replaces the <c>Call</c>
/// instruction with a renamed copy of the callee's instructions and — if the callee returns
/// a value — an <c>Assign</c> from the renamed return-value temporary.
///
/// Phase 5: wired into the compiler pipeline after LICM.
/// </summary>
public sealed class InliningPass
{
    /// <summary>Maximum number of instructions in a callee for it to be considered for inlining.</summary>
    public int MaxInlineInstructions { get; set; } = 5;

    /// <summary>Run inlining on all functions in the module.  Returns true if anything changed.</summary>
    public bool Inline(MirModule module)
    {
        // Build index of inlinable functions by name.
        var inlinable = BuildInlinableIndex(module);
        if (inlinable.Count == 0) return false;

        bool changed = false;
        foreach (var fn in module.Functions)
        {
            changed |= InlineCallsIn(fn, inlinable);
        }
        return changed;
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────────

    private Dictionary<string, MirFunction> BuildInlinableIndex(MirModule module)
    {
        var result = new Dictionary<string, MirFunction>(StringComparer.Ordinal);
        foreach (var fn in module.Functions)
        {
            if (!IsInlinable(fn)) continue;
            result[fn.Name] = fn;
        }
        return result;
    }

    private bool IsInlinable(MirFunction fn)
    {
        // Only inline single-block functions to keep the transform simple.
        if (fn.BasicBlocks.Count != 1) return false;

        var block = fn.BasicBlocks[0];
        int count = block.Instructions.Count;
        if (count > MaxInlineInstructions) return false;

        // Don't inline functions that call themselves (direct recursion).
        foreach (var instr in block.Instructions)
        {
            if (instr.Opcode != MirOpcode.Call) continue;
            if (instr.Operands.Count > 0 &&
                instr.Operands[0].Kind == MirOperandKind.FunctionRef &&
                instr.Operands[0].Name == fn.Name)
                return false;
        }
        return true;
    }

    private bool InlineCallsIn(MirFunction caller, Dictionary<string, MirFunction> inlinable)
    {
        bool changed = false;
        int uniqueSuffix = 0;

        foreach (var block in caller.BasicBlocks)
        {
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var instr = block.Instructions[i];
                if (instr.Opcode != MirOpcode.Call) continue;
                if (instr.Operands.Count == 0) continue;
                var calleeRef = instr.Operands[0];
                if (calleeRef.Kind != MirOperandKind.FunctionRef) continue;
                if (!inlinable.TryGetValue(calleeRef.Name, out var callee)) continue;
                // Don't inline a function into itself (prevents infinite recursion during inlining).
                if (callee.Name == caller.Name) continue;

                // Build argument map: parameter name → call-site operand.
                var argMap = new Dictionary<string, MirOperand>(StringComparer.Ordinal);
                for (int p = 0; p < callee.Parameters.Count && p + 1 < instr.Operands.Count; p++)
                    argMap[callee.Parameters[p].Name] = instr.Operands[p + 1];

                string suffix = $"__inlined_{calleeRef.Name}_{uniqueSuffix++}";

                // Collect inlined instructions (substituting parameters).
                var inlinedInstrs = new List<MirInstruction>();

                var calleeBlock = callee.BasicBlocks[0];
                foreach (var calleeInstr in calleeBlock.Instructions)
                    inlinedInstrs.Add(RenameInstruction(calleeInstr, argMap, suffix));

                // Handle return value.
                if (calleeBlock.Terminator is MirReturn ret && ret.Value != null && instr.Destination != null)
                {
                    var renamedRetVal = RenameOperand(ret.Value, argMap, suffix);
                    inlinedInstrs.Add(new MirInstruction(
                        MirOpcode.Assign,
                        instr.Destination,
                        new[] { renamedRetVal }));
                }

                // Replace the call instruction with the inlined body.
                block.Instructions.RemoveAt(i);
                block.Instructions.InsertRange(i, inlinedInstrs);
                i += inlinedInstrs.Count - 1; // adjust loop index
                changed = true;
            }
        }
        return changed;
    }

    private static MirInstruction RenameInstruction(
        MirInstruction src,
        Dictionary<string, MirOperand> argMap,
        string suffix)
    {
        var dest = src.Destination != null
            ? MirOperand.Temp($"{src.Destination.Name}{suffix}", src.Destination.Type)
            : null;

        var ops = src.Operands.Select(op => RenameOperand(op, argMap, suffix)).ToArray();
        return new MirInstruction(src.Opcode, dest, ops, src.Extra);
    }

    private static MirOperand RenameOperand(
        MirOperand op,
        Dictionary<string, MirOperand> argMap,
        string suffix)
    {
        switch (op.Kind)
        {
            case MirOperandKind.Variable:
                // If this variable is a parameter, substitute the call-site argument.
                if (argMap.TryGetValue(op.Name, out var arg))
                    return arg;
                return MirOperand.Variable($"{op.Name}{suffix}", op.Type);

            case MirOperandKind.Temp:
                return MirOperand.Temp($"{op.Name}{suffix}", op.Type);

            default:
                return op; // Constants and FunctionRefs are unchanged.
        }
    }
}
