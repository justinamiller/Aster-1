using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Analysis;

/// <summary>
/// SSA (Static Single Assignment) form builder.
/// Converts MIR to SSA form with phi nodes.
/// </summary>
public sealed class SsaBuilder
{
    private readonly MirFunction _function;
    private readonly ControlFlowGraph _cfg;
    private readonly DominatorTree _domTree;
    private readonly Dictionary<string, int> _varVersions = new();
    private readonly Dictionary<(int Block, string Var), PhiNode> _phiNodes = new();

    public SsaBuilder(MirFunction function)
    {
        _function = function;
        _cfg = ControlFlowGraph.Build(function);
        _domTree = DominatorTree.Build(_cfg);
    }

    /// <summary>Convert function to SSA form by inserting phi nodes.</summary>
    public SsaResult BuildSsa()
    {
        // Step 1: Find all variables
        var allVars = CollectVariables();

        // Step 2: Insert phi nodes
        InsertPhiNodes(allVars);

        // Step 3: Rename variables
        RenameVariables();

        return new SsaResult(_phiNodes.Values.ToList());
    }

    private HashSet<string> CollectVariables()
    {
        var vars = new HashSet<string>();

        foreach (var block in _function.BasicBlocks)
        {
            foreach (var instr in block.Instructions)
            {
                if (instr.Destination != null)
                {
                    vars.Add(instr.Destination.Name);
                }

                foreach (var operand in instr.Operands)
                {
                    if (operand.Kind == MirOperandKind.Variable)
                    {
                        vars.Add(operand.Name);
                    }
                }
            }
        }

        return vars;
    }

    private void InsertPhiNodes(HashSet<string> variables)
    {
        foreach (var variable in variables)
        {
            var defBlocks = new HashSet<int>();

            // Find blocks where variable is defined
            for (int i = 0; i < _function.BasicBlocks.Count; i++)
            {
                var block = _function.BasicBlocks[i];
                foreach (var instr in block.Instructions)
                {
                    if (instr.Destination?.Name == variable)
                    {
                        defBlocks.Add(i);
                    }
                }
            }

            // Insert phi nodes at dominance frontiers
            var workQueue = new Queue<int>(defBlocks);
            var processed = new HashSet<int>();

            while (workQueue.Count > 0)
            {
                var blockIdx = workQueue.Dequeue();
                if (blockIdx >= _cfg.Nodes.Count)
                    continue;

                var node = _cfg.Nodes.FirstOrDefault(n => n.BlockIndex == blockIdx);
                if (node == null)
                    continue;

                var frontier = _domTree.GetDominanceFrontier(node);

                foreach (var dfNode in frontier)
                {
                    if (processed.Add(dfNode.BlockIndex))
                    {
                        // Insert phi node
                        var phi = new PhiNode(dfNode.BlockIndex, variable);
                        _phiNodes[(dfNode.BlockIndex, variable)] = phi;

                        if (!defBlocks.Contains(dfNode.BlockIndex))
                        {
                            workQueue.Enqueue(dfNode.BlockIndex);
                        }
                    }
                }
            }
        }
    }

    private void RenameVariables()
    {
        // Simplified renaming - in a full implementation, this would
        // traverse the dominator tree and rename variables to SSA form
        // For now, we just track that phi nodes exist
    }
}

/// <summary>Result of SSA construction.</summary>
public sealed record SsaResult(List<PhiNode> PhiNodes);

/// <summary>
/// Phi node for SSA form.
/// Φ(v1, v2, ..., vn) merges values from different control flow paths.
/// </summary>
public sealed class PhiNode
{
    public int BlockIndex { get; }
    public string Variable { get; }
    public List<(int PredecessorBlock, string Value)> Operands { get; } = new();

    public PhiNode(int blockIndex, string variable)
    {
        BlockIndex = blockIndex;
        Variable = variable;
    }

    public override string ToString()
    {
        var operands = string.Join(", ", Operands.Select(o => $"[{o.PredecessorBlock}: {o.Value}]"));
        return $"{Variable} = φ({operands})";
    }
}
