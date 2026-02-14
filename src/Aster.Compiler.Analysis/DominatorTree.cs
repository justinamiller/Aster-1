using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Analysis;

/// <summary>
/// Dominator tree for control flow analysis.
/// A block X dominates block Y if all paths from entry to Y go through X.
/// </summary>
public sealed class DominatorTree
{
    private readonly ControlFlowGraph _cfg;
    private readonly Dictionary<CfgNode, CfgNode?> _immediateDominators = new();
    private readonly Dictionary<CfgNode, HashSet<CfgNode>> _dominanceFrontier = new();

    private DominatorTree(ControlFlowGraph cfg)
    {
        _cfg = cfg;
    }

    /// <summary>Build dominator tree using iterative algorithm.</summary>
    public static DominatorTree Build(ControlFlowGraph cfg)
    {
        var tree = new DominatorTree(cfg);
        tree.ComputeDominators();
        tree.ComputeDominanceFrontier();
        return tree;
    }

    /// <summary>Get the immediate dominator of a node.</summary>
    public CfgNode? GetImmediateDominator(CfgNode node)
    {
        return _immediateDominators.TryGetValue(node, out var idom) ? idom : null;
    }

    /// <summary>Check if node X dominates node Y.</summary>
    public bool Dominates(CfgNode x, CfgNode y)
    {
        if (x == y)
            return true;

        var current = y;
        while (current != null)
        {
            var idom = GetImmediateDominator(current);
            if (idom == x)
                return true;
            if (idom == current)
                break;
            current = idom;
        }

        return false;
    }

    /// <summary>Get the dominance frontier of a node.</summary>
    public IReadOnlySet<CfgNode> GetDominanceFrontier(CfgNode node)
    {
        return _dominanceFrontier.TryGetValue(node, out var frontier)
            ? frontier
            : new HashSet<CfgNode>();
    }

    private void ComputeDominators()
    {
        var nodes = _cfg.Nodes.ToList();
        
        // Entry dominates itself
        _immediateDominators[_cfg.Entry] = _cfg.Entry;

        // Initialize all other nodes
        foreach (var node in nodes)
        {
            if (node != _cfg.Entry)
            {
                _immediateDominators[node] = null;
            }
        }

        // Iterative dataflow until fixpoint
        bool changed = true;
        while (changed)
        {
            changed = false;

            foreach (var node in nodes)
            {
                if (node == _cfg.Entry)
                    continue;

                var predecessors = _cfg.GetPredecessors(node).ToList();
                if (predecessors.Count == 0)
                    continue;

                // New idom is intersection of all predecessor dominators
                CfgNode? newIdom = null;
                foreach (var pred in predecessors)
                {
                    if (_immediateDominators[pred] != null)
                    {
                        if (newIdom == null)
                        {
                            newIdom = pred;
                        }
                        else
                        {
                            newIdom = Intersect(pred, newIdom);
                        }
                    }
                }

                if (newIdom != _immediateDominators[node])
                {
                    _immediateDominators[node] = newIdom;
                    changed = true;
                }
            }
        }
    }

    private CfgNode? Intersect(CfgNode b1, CfgNode b2)
    {
        var finger1 = b1;
        var finger2 = b2;

        while (finger1 != finger2)
        {
            while (finger1 != null && finger2 != null && finger1.BlockIndex < finger2.BlockIndex)
            {
                finger1 = _immediateDominators[finger1];
            }

            while (finger1 != null && finger2 != null && finger2.BlockIndex < finger1.BlockIndex)
            {
                finger2 = _immediateDominators[finger2];
            }

            if (finger1 == null || finger2 == null)
                return null;
        }

        return finger1;
    }

    private void ComputeDominanceFrontier()
    {
        foreach (var node in _cfg.Nodes)
        {
            _dominanceFrontier[node] = new HashSet<CfgNode>();
        }

        foreach (var node in _cfg.Nodes)
        {
            var predecessors = _cfg.GetPredecessors(node).ToList();
            if (predecessors.Count < 2)
                continue;

            foreach (var pred in predecessors)
            {
                var runner = pred;
                while (runner != null && runner != _immediateDominators[node])
                {
                    _dominanceFrontier[runner].Add(node);
                    runner = _immediateDominators[runner];
                }
            }
        }
    }
}
