using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Analysis;

/// <summary>
/// Liveness analysis for MIR.
/// Determines which variables are live at each program point.
/// </summary>
public sealed class LivenessAnalysis
{
    private readonly MirFunction _function;
    private readonly ControlFlowGraph _cfg;
    private readonly Dictionary<int, HashSet<string>> _liveIn = new();
    private readonly Dictionary<int, HashSet<string>> _liveOut = new();

    public LivenessAnalysis(MirFunction function)
    {
        _function = function;
        _cfg = ControlFlowGraph.Build(function);
    }

    /// <summary>Run liveness analysis.</summary>
    public LivenessResult Analyze()
    {
        // Initialize
        foreach (var node in _cfg.Nodes)
        {
            _liveIn[node.BlockIndex] = new HashSet<string>();
            _liveOut[node.BlockIndex] = new HashSet<string>();
        }

        // Iterate to fixpoint (backward dataflow)
        bool changed = true;
        while (changed)
        {
            changed = false;

            // Process blocks in reverse post-order
            var blocks = _cfg.GetReversePostOrder();
            blocks.Reverse();

            foreach (var node in blocks)
            {
                var blockIdx = node.BlockIndex;
                var block = node.Block;

                // out[n] = union of in[s] for all successors s
                var newLiveOut = new HashSet<string>();
                foreach (var succ in node.Successors)
                {
                    newLiveOut.UnionWith(_liveIn[succ.BlockIndex]);
                }

                // in[n] = use[n] ∪ (out[n] - def[n])
                var (use, def) = GetUseDefSets(block);
                var newLiveIn = new HashSet<string>(use);
                foreach (var v in newLiveOut)
                {
                    if (!def.Contains(v))
                    {
                        newLiveIn.Add(v);
                    }
                }

                // Check if changed
                if (!newLiveIn.SetEquals(_liveIn[blockIdx]) || !newLiveOut.SetEquals(_liveOut[blockIdx]))
                {
                    _liveIn[blockIdx] = newLiveIn;
                    _liveOut[blockIdx] = newLiveOut;
                    changed = true;
                }
            }
        }

        return new LivenessResult(_liveIn, _liveOut);
    }

    private (HashSet<string> Use, HashSet<string> Def) GetUseDefSets(MirBasicBlock block)
    {
        var use = new HashSet<string>();
        var def = new HashSet<string>();

        foreach (var instr in block.Instructions)
        {
            // Add uses (operands that are variables)
            foreach (var operand in instr.Operands)
            {
                if (operand.Kind == MirOperandKind.Variable && !def.Contains(operand.Name))
                {
                    use.Add(operand.Name);
                }
            }

            // Add definitions
            if (instr.Destination != null && instr.Destination.Kind == MirOperandKind.Variable)
            {
                def.Add(instr.Destination.Name);
            }
        }

        // Handle terminator
        if (block.Terminator is MirConditionalBranch condBranch)
        {
            if (condBranch.Condition.Kind == MirOperandKind.Variable && !def.Contains(condBranch.Condition.Name))
            {
                use.Add(condBranch.Condition.Name);
            }
        }
        else if (block.Terminator is MirSwitch switchTerm)
        {
            if (switchTerm.Scrutinee.Kind == MirOperandKind.Variable && !def.Contains(switchTerm.Scrutinee.Name))
            {
                use.Add(switchTerm.Scrutinee.Name);
            }
        }
        else if (block.Terminator is MirReturn ret && ret.Value != null)
        {
            if (ret.Value.Kind == MirOperandKind.Variable && !def.Contains(ret.Value.Name))
            {
                use.Add(ret.Value.Name);
            }
        }

        return (use, def);
    }

    /// <summary>Check if a variable is live at the end of a block.</summary>
    public bool IsLiveOut(int blockIndex, string variable)
    {
        return _liveOut.TryGetValue(blockIndex, out var liveOut) && liveOut.Contains(variable);
    }

    /// <summary>Check if a variable is live at the start of a block.</summary>
    public bool IsLiveIn(int blockIndex, string variable)
    {
        return _liveIn.TryGetValue(blockIndex, out var liveIn) && liveIn.Contains(variable);
    }
}

/// <summary>Result of liveness analysis.</summary>
public sealed record LivenessResult(
    IReadOnlyDictionary<int, HashSet<string>> LiveIn,
    IReadOnlyDictionary<int, HashSet<string>> LiveOut);
