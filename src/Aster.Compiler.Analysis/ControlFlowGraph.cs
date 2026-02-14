using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Analysis;

/// <summary>
/// Control Flow Graph representation for MIR.
/// Provides structured access to control flow for analysis and optimization.
/// </summary>
public sealed class ControlFlowGraph
{
    public MirFunction Function { get; }
    public List<CfgNode> Nodes { get; } = new();
    public CfgNode Entry { get; }
    public CfgNode Exit { get; }

    internal ControlFlowGraph(MirFunction function, CfgNode entry, CfgNode exit)
    {
        Function = function;
        Entry = entry;
        Exit = exit;
    }

    /// <summary>Build a CFG from a MIR function.</summary>
    public static ControlFlowGraph Build(MirFunction function)
    {
        var builder = new CfgBuilder(function);
        return builder.Build();
    }

    /// <summary>Get predecessors of a block.</summary>
    public IEnumerable<CfgNode> GetPredecessors(CfgNode node)
    {
        return Nodes.Where(n => n.Successors.Contains(node));
    }

    /// <summary>Get all blocks in reverse post-order (good for dataflow).</summary>
    public List<CfgNode> GetReversePostOrder()
    {
        var visited = new HashSet<CfgNode>();
        var postOrder = new List<CfgNode>();

        void Visit(CfgNode node)
        {
            if (!visited.Add(node))
                return;

            foreach (var succ in node.Successors)
            {
                Visit(succ);
            }
            postOrder.Add(node);
        }

        Visit(Entry);
        postOrder.Reverse();
        return postOrder;
    }
}

/// <summary>CFG node representing a basic block.</summary>
public sealed class CfgNode
{
    public int BlockIndex { get; }
    public MirBasicBlock Block { get; }
    public List<CfgNode> Successors { get; } = new();

    public CfgNode(int blockIndex, MirBasicBlock block)
    {
        BlockIndex = blockIndex;
        Block = block;
    }
}

/// <summary>Builder for constructing CFGs.</summary>
internal sealed class CfgBuilder
{
    private readonly MirFunction _function;
    private readonly Dictionary<int, CfgNode> _nodes = new();

    public CfgBuilder(MirFunction function)
    {
        _function = function;
    }

    public ControlFlowGraph Build()
    {
        // Create nodes for all blocks
        for (int i = 0; i < _function.BasicBlocks.Count; i++)
        {
            var block = _function.BasicBlocks[i];
            _nodes[i] = new CfgNode(i, block);
        }

        // Build edges based on terminators
        foreach (var (index, node) in _nodes)
        {
            var block = node.Block;
            if (block.Terminator == null)
                continue;

            switch (block.Terminator)
            {
                case MirReturn:
                    // No successors
                    break;

                case MirBranch branch:
                    if (_nodes.TryGetValue(branch.TargetBlock, out var target))
                        node.Successors.Add(target);
                    break;

                case MirConditionalBranch condBranch:
                    if (_nodes.TryGetValue(condBranch.TrueBlock, out var trueTarget))
                        node.Successors.Add(trueTarget);
                    if (_nodes.TryGetValue(condBranch.FalseBlock, out var falseTarget))
                        node.Successors.Add(falseTarget);
                    break;

                case MirSwitch switchTerm:
                    foreach (var (_, targetBlock) in switchTerm.Cases)
                    {
                        if (_nodes.TryGetValue(targetBlock, out var caseTarget))
                            node.Successors.Add(caseTarget);
                    }
                    if (_nodes.TryGetValue(switchTerm.DefaultBlock, out var defaultTarget))
                        node.Successors.Add(defaultTarget);
                    break;
            }
        }

        // Create synthetic entry and exit nodes
        var entry = _nodes.ContainsKey(0) ? _nodes[0] : new CfgNode(-1, new MirBasicBlock("entry", -1));
        var exit = new CfgNode(-2, new MirBasicBlock("exit", -2));

        var cfg = new ControlFlowGraph(_function, entry, exit);
        cfg.Nodes.AddRange(_nodes.Values.OrderBy(n => n.BlockIndex));

        return cfg;
    }
}
