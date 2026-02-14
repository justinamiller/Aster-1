using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.DropLowering;

/// <summary>
/// Inserts explicit drop instructions for values going out of scope.
/// Ensures all destructors are called at the right points in the control flow.
/// </summary>
public sealed class DropLower
{
    /// <summary>Lower drops in a MIR module.</summary>
    public void Lower(MirModule module)
    {
        foreach (var fn in module.Functions)
        {
            InsertDrops(fn);
        }
    }

    private void InsertDrops(MirFunction fn)
    {
        // Track all assigned variables to insert drops before returns
        var assignedVars = new HashSet<string>();

        foreach (var block in fn.BasicBlocks)
        {
            foreach (var instr in block.Instructions)
            {
                if (instr.Destination != null && instr.Destination.Kind == MirOperandKind.Variable)
                {
                    assignedVars.Add(instr.Destination.Name);
                }
            }

            // Insert drops before return terminators
            if (block.Terminator is MirReturn ret)
            {
                var dropsToInsert = new List<MirInstruction>();
                foreach (var varName in assignedVars)
                {
                    // Don't drop the return value
                    if (ret.Value != null && ret.Value.Name == varName)
                        continue;

                    dropsToInsert.Add(new MirInstruction(
                        MirOpcode.Drop,
                        null,
                        new[] { MirOperand.Variable(varName, MirType.Void) }));
                }

                // Insert drops before the existing instructions' end
                block.Instructions.AddRange(dropsToInsert);
            }
        }
    }
}
