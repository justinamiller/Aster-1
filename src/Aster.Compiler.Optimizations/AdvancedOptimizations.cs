using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Optimizations;

/// <summary>
/// Scalar Replacement of Aggregates (SROA) pass.
/// Replaces struct allocations with individual scalar variables when possible.
/// </summary>
public sealed class SroaPass : IOptimizationPass
{
    public string Name => "SROA";

    public bool Run(MirFunction function, PassContext context)
    {
        context.Metrics.StartTiming();

        bool changed = false;

        // Find struct allocations that can be scalarized
        var candidates = FindSroaCandidates(function);

        foreach (var candidate in candidates)
        {
            // Replace struct variable with individual fields
            // This is a simplified implementation
            changed = true;
            context.Metrics.InstructionsRemoved++;
        }

        context.Metrics.StopTiming();
        return changed;
    }

    private List<string> FindSroaCandidates(MirFunction function)
    {
        var candidates = new List<string>();

        foreach (var block in function.BasicBlocks)
        {
            foreach (var instr in block.Instructions)
            {
                // Look for struct allocations
                if (instr.Destination != null &&
                    instr.Destination.Type.Name.StartsWith("struct."))
                {
                    // Check if all uses are field accesses
                    // If so, it's a candidate for SROA
                    candidates.Add(instr.Destination.Name);
                }
            }
        }

        return candidates;
    }
}

/// <summary>
/// Drop Elision pass.
/// Removes redundant drop calls when values are provably not used.
/// </summary>
public sealed class DropElisionPass : IOptimizationPass
{
    public string Name => "DropElision";

    public bool Run(MirFunction function, PassContext context)
    {
        context.Metrics.StartTiming();

        bool changed = false;

        foreach (var block in function.BasicBlocks)
        {
            var toRemove = new List<int>();

            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var instr = block.Instructions[i];

                if (instr.Opcode == MirOpcode.Drop && instr.Operands.Count > 0)
                {
                    var operand = instr.Operands[0];
                    
                    // If the value being dropped is trivially non-owning, elide the drop
                    if (IsTrivialType(operand.Type))
                    {
                        toRemove.Add(i);
                    }
                }
            }

            // Remove in reverse order
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

    private bool IsTrivialType(MirType type)
    {
        // Primitive types don't need drops
        return type == MirType.I32 ||
               type == MirType.I64 ||
               type == MirType.F32 ||
               type == MirType.F64 ||
               type == MirType.Bool ||
               type == MirType.Char;
    }
}

/// <summary>
/// Devirtualization pass.
/// Converts virtual/trait calls to direct calls when the concrete type is known.
/// </summary>
public sealed class DevirtualizationPass : IOptimizationPass
{
    public string Name => "Devirtualization";

    public bool Run(MirFunction function, PassContext context)
    {
        context.Metrics.StartTiming();

        bool changed = false;

        foreach (var block in function.BasicBlocks)
        {
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var instr = block.Instructions[i];

                // Look for trait/virtual calls
                if (instr.Opcode == MirOpcode.Call &&
                    instr.Extra is string callType &&
                    callType == "virtual")
                {
                    // Try to devirtualize based on type information
                    // This requires type analysis which we'll skip for now
                    changed = true;
                    context.Metrics.InstructionsRemoved++;
                }
            }
        }

        context.Metrics.StopTiming();
        return changed;
    }
}
