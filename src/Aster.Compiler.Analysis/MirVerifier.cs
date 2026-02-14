using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Analysis;

/// <summary>
/// MIR verifier - validates MIR well-formedness.
/// Checks CFG structure, SSA properties, and correctness.
/// </summary>
public sealed class MirVerifier
{
    private readonly List<string> _errors = new();

    /// <summary>Verify a MIR module.</summary>
    public VerificationResult Verify(MirModule module)
    {
        _errors.Clear();

        foreach (var function in module.Functions)
        {
            VerifyFunction(function);
        }

        return new VerificationResult(_errors.Count == 0, _errors.ToArray());
    }

    /// <summary>Verify a single function.</summary>
    public VerificationResult VerifyFunction(MirFunction function)
    {
        _errors.Clear();

        // Check 1: Every block must have a terminator
        CheckTerminators(function);

        // Check 2: CFG well-formedness
        CheckCfgWellFormed(function);

        // Check 3: No dangling references
        CheckNoDanglingReferences(function);

        // Check 4: Type consistency (basic check)
        CheckTypeConsistency(function);

        // Check 5: No duplicate block labels
        CheckNoDuplicateLabels(function);

        return new VerificationResult(_errors.Count == 0, _errors.ToArray());
    }

    private void CheckTerminators(MirFunction function)
    {
        for (int i = 0; i < function.BasicBlocks.Count; i++)
        {
            var block = function.BasicBlocks[i];
            if (block.Terminator == null)
            {
                _errors.Add($"Block {i} ({block.Label}) has no terminator");
            }
        }
    }

    private void CheckCfgWellFormed(MirFunction function)
    {
        var validIndices = new HashSet<int>(
            Enumerable.Range(0, function.BasicBlocks.Count)
        );

        for (int i = 0; i < function.BasicBlocks.Count; i++)
        {
            var block = function.BasicBlocks[i];
            if (block.Terminator == null)
                continue;

            switch (block.Terminator)
            {
                case MirBranch branch:
                    if (!validIndices.Contains(branch.TargetBlock))
                    {
                        _errors.Add($"Block {i} branches to invalid block {branch.TargetBlock}");
                    }
                    break;

                case MirConditionalBranch condBranch:
                    if (!validIndices.Contains(condBranch.TrueBlock))
                    {
                        _errors.Add($"Block {i} has invalid true branch to {condBranch.TrueBlock}");
                    }
                    if (!validIndices.Contains(condBranch.FalseBlock))
                    {
                        _errors.Add($"Block {i} has invalid false branch to {condBranch.FalseBlock}");
                    }
                    break;

                case MirSwitch switchTerm:
                    foreach (var (value, targetBlock) in switchTerm.Cases)
                    {
                        if (!validIndices.Contains(targetBlock))
                        {
                            _errors.Add($"Block {i} switch has invalid case branch to {targetBlock}");
                        }
                    }
                    if (!validIndices.Contains(switchTerm.DefaultBlock))
                    {
                        _errors.Add($"Block {i} switch has invalid default branch to {switchTerm.DefaultBlock}");
                    }
                    break;
            }
        }
    }

    private void CheckNoDanglingReferences(MirFunction function)
    {
        var definedVars = new HashSet<string>();

        // Collect parameters
        foreach (var param in function.Parameters)
        {
            definedVars.Add(param.Name);
        }

        // Collect all definitions
        foreach (var block in function.BasicBlocks)
        {
            foreach (var instr in block.Instructions)
            {
                if (instr.Destination != null && instr.Destination.Kind == MirOperandKind.Variable)
                {
                    definedVars.Add(instr.Destination.Name);
                }
            }
        }

        // Check all uses
        for (int blockIdx = 0; blockIdx < function.BasicBlocks.Count; blockIdx++)
        {
            var block = function.BasicBlocks[blockIdx];

            for (int instrIdx = 0; instrIdx < block.Instructions.Count; instrIdx++)
            {
                var instr = block.Instructions[instrIdx];
                
                foreach (var operand in instr.Operands)
                {
                    if (operand.Kind == MirOperandKind.Variable && !definedVars.Contains(operand.Name))
                    {
                        _errors.Add($"Block {blockIdx} instruction {instrIdx} uses undefined variable '{operand.Name}'");
                    }
                }
            }

            // Check terminator
            if (block.Terminator is MirConditionalBranch condBranch)
            {
                if (condBranch.Condition.Kind == MirOperandKind.Variable && !definedVars.Contains(condBranch.Condition.Name))
                {
                    _errors.Add($"Block {blockIdx} condition uses undefined variable '{condBranch.Condition.Name}'");
                }
            }
        }
    }

    private void CheckTypeConsistency(MirFunction function)
    {
        // Basic type checking - ensure operands have types
        for (int blockIdx = 0; blockIdx < function.BasicBlocks.Count; blockIdx++)
        {
            var block = function.BasicBlocks[blockIdx];

            foreach (var instr in block.Instructions)
            {
                if (instr.Destination != null && instr.Destination.Type == null)
                {
                    _errors.Add($"Block {blockIdx} instruction destination has no type");
                }

                foreach (var operand in instr.Operands)
                {
                    if (operand.Type == null)
                    {
                        _errors.Add($"Block {blockIdx} instruction operand has no type");
                    }
                }
            }
        }
    }

    private void CheckNoDuplicateLabels(MirFunction function)
    {
        var labels = new HashSet<string>();

        foreach (var block in function.BasicBlocks)
        {
            if (!labels.Add(block.Label))
            {
                _errors.Add($"Duplicate block label: {block.Label}");
            }
        }
    }
}

/// <summary>Result of MIR verification.</summary>
public sealed record VerificationResult(bool IsValid, string[] Errors)
{
    public override string ToString()
    {
        if (IsValid)
            return "MIR is valid";
        
        return $"MIR verification failed with {Errors.Length} error(s):\n" +
               string.Join("\n", Errors.Select(e => $"  - {e}"));
    }
}
