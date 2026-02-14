using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Analysis;

/// <summary>
/// Def-use chains for SSA form.
/// Tracks where each value is defined and where it is used.
/// </summary>
public sealed class DefUseChains
{
    private readonly Dictionary<string, Definition> _definitions = new();
    private readonly Dictionary<string, List<Use>> _uses = new();

    /// <summary>Record a definition of a variable.</summary>
    public void RecordDefinition(string variable, int blockIndex, int instructionIndex)
    {
        _definitions[variable] = new Definition(variable, blockIndex, instructionIndex);
        
        if (!_uses.ContainsKey(variable))
        {
            _uses[variable] = new List<Use>();
        }
    }

    /// <summary>Record a use of a variable.</summary>
    public void RecordUse(string variable, int blockIndex, int instructionIndex)
    {
        if (!_uses.ContainsKey(variable))
        {
            _uses[variable] = new List<Use>();
        }
        
        _uses[variable].Add(new Use(variable, blockIndex, instructionIndex));
    }

    /// <summary>Get the definition of a variable.</summary>
    public Definition? GetDefinition(string variable)
    {
        return _definitions.TryGetValue(variable, out var def) ? def : null;
    }

    /// <summary>Get all uses of a variable.</summary>
    public IReadOnlyList<Use> GetUses(string variable)
    {
        return _uses.TryGetValue(variable, out var uses) ? uses : new List<Use>();
    }

    /// <summary>Check if a variable is used anywhere.</summary>
    public bool IsUsed(string variable)
    {
        return _uses.TryGetValue(variable, out var uses) && uses.Count > 0;
    }

    /// <summary>Build def-use chains from a MIR function.</summary>
    public static DefUseChains Build(MirFunction function)
    {
        var chains = new DefUseChains();

        for (int blockIdx = 0; blockIdx < function.BasicBlocks.Count; blockIdx++)
        {
            var block = function.BasicBlocks[blockIdx];

            for (int instrIdx = 0; instrIdx < block.Instructions.Count; instrIdx++)
            {
                var instr = block.Instructions[instrIdx];

                // Record definition
                if (instr.Destination != null && instr.Destination.Kind == MirOperandKind.Variable)
                {
                    chains.RecordDefinition(instr.Destination.Name, blockIdx, instrIdx);
                }

                // Record uses
                foreach (var operand in instr.Operands)
                {
                    if (operand.Kind == MirOperandKind.Variable)
                    {
                        chains.RecordUse(operand.Name, blockIdx, instrIdx);
                    }
                }
            }

            // Handle terminator uses
            if (block.Terminator is MirConditionalBranch condBranch)
            {
                if (condBranch.Condition.Kind == MirOperandKind.Variable)
                {
                    chains.RecordUse(condBranch.Condition.Name, blockIdx, -1);
                }
            }
            else if (block.Terminator is MirSwitch switchTerm)
            {
                if (switchTerm.Scrutinee.Kind == MirOperandKind.Variable)
                {
                    chains.RecordUse(switchTerm.Scrutinee.Name, blockIdx, -1);
                }
            }
            else if (block.Terminator is MirReturn ret && ret.Value != null)
            {
                if (ret.Value.Kind == MirOperandKind.Variable)
                {
                    chains.RecordUse(ret.Value.Name, blockIdx, -1);
                }
            }
        }

        return chains;
    }
}

/// <summary>Represents a definition point.</summary>
public sealed record Definition(string Variable, int BlockIndex, int InstructionIndex);

/// <summary>Represents a use point.</summary>
public sealed record Use(string Variable, int BlockIndex, int InstructionIndex);
