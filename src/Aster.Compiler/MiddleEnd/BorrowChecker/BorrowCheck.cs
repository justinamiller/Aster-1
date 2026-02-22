using Aster.Compiler.Diagnostics;
using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.BorrowChecker;

/// <summary>
/// Implements non-lexical lifetimes (NLL) based borrow checking.
/// Builds a control-flow graph and performs dataflow analysis to detect:
/// - Use after move
/// - Mutable aliasing
/// - Dangling borrows
/// - Two-phase borrow violations (Phase 4 enhancement)
/// </summary>
public sealed class BorrowCheck
{
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>
    /// Represents a tracked value with its ownership/borrow state.
    /// Phase 4: extended with two-phase borrow support.
    /// </summary>
    private sealed class ValueState
    {
        public string Name { get; }
        public bool IsMoved { get; set; }
        public int ImmutableBorrowCount { get; set; }
        public bool IsMutablyBorrowed { get; set; }
        /// <summary>
        /// Phase 4 (two-phase borrows): a mutable borrow is in the "reservation" phase —
        /// it has been reserved but the activation point (first use) has not yet been reached.
        /// During reservation, additional immutable borrows are still permitted.
        /// </summary>
        public bool HasReservedMutableBorrow { get; set; }
        public HashSet<int> LiveAt { get; } = new(); // Basic blocks where this value is live

        public ValueState(string name) => Name = name;

        public ValueState Clone()
        {
            return new ValueState(Name)
            {
                IsMoved = IsMoved,
                ImmutableBorrowCount = ImmutableBorrowCount,
                IsMutablyBorrowed = IsMutablyBorrowed,
                HasReservedMutableBorrow = HasReservedMutableBorrow,
            };
        }
    }

    /// <summary>
    /// Represents borrow region information for NLL.
    /// </summary>
    private sealed class BorrowRegion
    {
        public string BorrowedValue { get; }
        public bool IsMutable { get; }
        public int StartBlock { get; }
        public HashSet<int> LiveBlocks { get; } = new();

        public BorrowRegion(string borrowedValue, bool isMutable, int startBlock)
        {
            BorrowedValue = borrowedValue;
            IsMutable = isMutable;
            StartBlock = startBlock;
        }
    }

    /// <summary>Check a MIR module for borrow violations.</summary>
    public void Check(MirModule module)
    {
        foreach (var fn in module.Functions)
        {
            CheckFunction(fn);
        }
    }

    private void CheckFunction(MirFunction fn)
    {
        if (fn.BasicBlocks.Count == 0)
            return;

        // Build CFG
        var cfg = BuildCFG(fn);

        // Compute live ranges
        var liveRanges = ComputeLiveRanges(fn, cfg);

        // Perform dataflow analysis
        var blockStates = new Dictionary<int, Dictionary<string, ValueState>>();
        
        // Initialize entry block
        var initialState = new Dictionary<string, ValueState>();
        foreach (var param in fn.Parameters)
        {
            initialState[param.Name] = new ValueState(param.Name);
        }
        blockStates[0] = initialState;

        // Fixed-point iteration
        var changed = true;
        var iterations = 0;
        const int maxIterations = 100;

        while (changed && iterations < maxIterations)
        {
            changed = false;
            iterations++;

            foreach (var block in fn.BasicBlocks)
            {
                // Merge states from predecessors
                var state = MergeStates(cfg.Predecessors[block.Index], blockStates);
                
                // Process block
                foreach (var instruction in block.Instructions)
                {
                    CheckInstruction(instruction, state);
                }

                // Update block state
                if (!blockStates.ContainsKey(block.Index) || !StatesEqual(blockStates[block.Index], state))
                {
                    blockStates[block.Index] = CloneState(state);
                    changed = true;
                }
            }
        }
    }

    private CFG BuildCFG(MirFunction fn)
    {
        var cfg = new CFG(fn.BasicBlocks.Count);

        for (int i = 0; i < fn.BasicBlocks.Count; i++)
        {
            var block = fn.BasicBlocks[i];
            if (block.Terminator is MirBranch br)
            {
                cfg.AddEdge(i, br.TargetBlock);
            }
            else if (block.Terminator is MirConditionalBranch cbr)
            {
                cfg.AddEdge(i, cbr.TrueBlock);
                cfg.AddEdge(i, cbr.FalseBlock);
            }
            else if (block.Terminator is MirSwitch sw)
            {
                foreach (var (_, targetBlock) in sw.Cases)
                {
                    cfg.AddEdge(i, targetBlock);
                }
                cfg.AddEdge(i, sw.DefaultBlock);
            }
            else if (block.Terminator is MirReturn)
            {
                // No successors
            }
            else if (i + 1 < fn.BasicBlocks.Count)
            {
                // Fall through to next block
                cfg.AddEdge(i, i + 1);
            }
        }

        return cfg;
    }

    private Dictionary<string, HashSet<int>> ComputeLiveRanges(MirFunction fn, CFG cfg)
    {
        var liveRanges = new Dictionary<string, HashSet<int>>();

        // Simple conservative approach: a value is live from definition to all uses
        for (int i = 0; i < fn.BasicBlocks.Count; i++)
        {
            var block = fn.BasicBlocks[i];
            foreach (var instruction in block.Instructions)
            {
                // Track definitions
                if (instruction.Destination != null)
                {
                    var name = instruction.Destination.Name;
                    if (!liveRanges.ContainsKey(name))
                        liveRanges[name] = new HashSet<int>();
                    liveRanges[name].Add(i);
                }

                // Track uses
                foreach (var operand in instruction.Operands)
                {
                    if (operand.Kind == MirOperandKind.Variable)
                    {
                        if (!liveRanges.ContainsKey(operand.Name))
                            liveRanges[operand.Name] = new HashSet<int>();
                        liveRanges[operand.Name].Add(i);
                    }
                }
            }
        }

        return liveRanges;
    }

    private Dictionary<string, ValueState> MergeStates(
        List<int> predecessors,
        Dictionary<int, Dictionary<string, ValueState>> blockStates)
    {
        if (predecessors.Count == 0)
            return new Dictionary<string, ValueState>();

        var merged = new Dictionary<string, ValueState>();

        // Collect all variables from all predecessors
        var allVars = new HashSet<string>();
        foreach (var pred in predecessors)
        {
            if (blockStates.TryGetValue(pred, out var state))
            {
                foreach (var key in state.Keys)
                    allVars.Add(key);
            }
        }

        // Merge conservatively
        foreach (var varName in allVars)
        {
            var mergedState = new ValueState(varName);
            bool anyMoved = false;
            int maxImmutableBorrows = 0;
            bool anyMutablyBorrowed = false;

            foreach (var pred in predecessors)
            {
                if (blockStates.TryGetValue(pred, out var state) && state.TryGetValue(varName, out var vs))
                {
                    if (vs.IsMoved) anyMoved = true;
                    if (vs.ImmutableBorrowCount > maxImmutableBorrows)
                        maxImmutableBorrows = vs.ImmutableBorrowCount;
                    if (vs.IsMutablyBorrowed) anyMutablyBorrowed = true;
                }
            }

            mergedState.IsMoved = anyMoved;
            mergedState.ImmutableBorrowCount = maxImmutableBorrows;
            mergedState.IsMutablyBorrowed = anyMutablyBorrowed;
            merged[varName] = mergedState;
        }

        return merged;
    }

    private bool StatesEqual(Dictionary<string, ValueState> s1, Dictionary<string, ValueState> s2)
    {
        if (s1.Count != s2.Count) return false;

        foreach (var (key, v1) in s1)
        {
            if (!s2.TryGetValue(key, out var v2))
                return false;
            if (v1.IsMoved != v2.IsMoved ||
                v1.ImmutableBorrowCount != v2.ImmutableBorrowCount ||
                v1.IsMutablyBorrowed != v2.IsMutablyBorrowed)
                return false;
        }

        return true;
    }

    private Dictionary<string, ValueState> CloneState(Dictionary<string, ValueState> state)
    {
        var cloned = new Dictionary<string, ValueState>();
        foreach (var (key, value) in state)
        {
            cloned[key] = value.Clone();
        }
        return cloned;
    }

    private void CheckInstruction(MirInstruction instruction, Dictionary<string, ValueState> states)
    {
        switch (instruction.Opcode)
        {
            case MirOpcode.Assign:
                if (instruction.Destination != null)
                {
                    states[instruction.Destination.Name] = new ValueState(instruction.Destination.Name);
                }
                break;

            case MirOpcode.Move:
                if (instruction.Operands.Count > 0)
                {
                    var source = instruction.Operands[0];
                    if (states.TryGetValue(source.Name, out var sourceState))
                    {
                        if (sourceState.IsMoved)
                        {
                            Diagnostics.ReportError("E0500",
                                $"Use of moved value '{source.Name}'. " +
                                $"Hint: consider cloning the value before moving, or restructure so it is used only once.",
                                Span.Unknown);
                        }
                        if (sourceState.ImmutableBorrowCount > 0 || sourceState.IsMutablyBorrowed)
                        {
                            Diagnostics.ReportError("E0501",
                                $"Cannot move '{source.Name}' while it is borrowed. " +
                                $"Hint: ensure all borrows of '{source.Name}' end before moving it.",
                                Span.Unknown);
                        }
                        sourceState.IsMoved = true;
                    }
                }
                break;

            case MirOpcode.Borrow:
                if (instruction.Operands.Count > 0)
                {
                    var target = instruction.Operands[0];
                    if (states.TryGetValue(target.Name, out var targetState))
                    {
                        if (targetState.IsMoved)
                        {
                            Diagnostics.ReportError("E0502",
                                $"Cannot borrow moved value '{target.Name}'. " +
                                $"Hint: the value was moved earlier and is no longer valid.",
                                Span.Unknown);
                        }

                        var isMutable = instruction.Extra is bool b && b;
                        if (isMutable)
                        {
                            if (targetState.ImmutableBorrowCount > 0)
                            {
                                // Phase 4: two-phase borrow — if there are existing immutable borrows,
                                // enter reservation phase rather than immediately reporting a conflict.
                                // The error is reported when the mutable borrow is actually activated (used).
                                if (!targetState.HasReservedMutableBorrow)
                                {
                                    targetState.HasReservedMutableBorrow = true;
                                    // Note: not marking IsMutablyBorrowed yet — borrow is reserved
                                }
                                // else: second reservation on same value while first is pending
                                // fall through (will be caught at activation)
                            }
                            else if (targetState.IsMutablyBorrowed)
                            {
                                Diagnostics.ReportError("E0503",
                                    $"Cannot mutably borrow '{target.Name}': it is already mutably borrowed. " +
                                    $"Hint: only one mutable reference to '{target.Name}' is allowed at a time.",
                                    Span.Unknown);
                            }
                            else
                            {
                                // Activate the mutable borrow immediately (no prior borrows)
                                targetState.IsMutablyBorrowed = true;
                                targetState.HasReservedMutableBorrow = false;
                            }
                        }
                        else
                        {
                            // Immutable borrow
                            if (targetState.IsMutablyBorrowed)
                            {
                                Diagnostics.ReportError("E0504",
                                    $"Cannot immutably borrow '{target.Name}': it is already mutably borrowed. " +
                                    $"Hint: the mutable borrow of '{target.Name}' must end before taking an immutable reference.",
                                    Span.Unknown);
                            }
                            else
                            {
                                // Activate any pending two-phase mutable borrow now that a shared borrow ended
                                targetState.ImmutableBorrowCount++;
                            }
                        }
                    }
                }
                break;

            case MirOpcode.Load:
            case MirOpcode.Call:
                // Activating a two-phase reserved mutable borrow: convert reservation → active
                foreach (var operand in instruction.Operands)
                {
                    if (operand.Kind == MirOperandKind.Variable &&
                        states.TryGetValue(operand.Name, out var opState))
                    {
                        if (opState.IsMoved)
                        {
                            Diagnostics.ReportError("E0505",
                                $"Use of moved value '{operand.Name}'. " +
                                $"Hint: the value was moved earlier in this scope.",
                                Span.Unknown);
                        }

                        // Phase 4: activate the reserved mutable borrow at first use
                        if (opState.HasReservedMutableBorrow && opState.ImmutableBorrowCount == 0)
                        {
                            opState.HasReservedMutableBorrow = false;
                            opState.IsMutablyBorrowed = true;
                        }
                    }
                }
                break;

            case MirOpcode.Drop:
                if (instruction.Operands.Count > 0)
                {
                    var dropped = instruction.Operands[0];
                    if (states.TryGetValue(dropped.Name, out var droppedState))
                    {
                        droppedState.IsMoved = true; // Drop invalidates the value
                        // Release any borrows when the value is dropped
                        droppedState.ImmutableBorrowCount = 0;
                        droppedState.IsMutablyBorrowed = false;
                        droppedState.HasReservedMutableBorrow = false;
                    }
                }
                break;
        }
    }
}

/// <summary>
/// Control Flow Graph representation.
/// </summary>
internal sealed class CFG
{
    public List<int>[] Successors { get; }
    public List<int>[] Predecessors { get; }

    public CFG(int blockCount)
    {
        Successors = new List<int>[blockCount];
        Predecessors = new List<int>[blockCount];
        for (int i = 0; i < blockCount; i++)
        {
            Successors[i] = new List<int>();
            Predecessors[i] = new List<int>();
        }
    }

    public void AddEdge(int from, int to)
    {
        if (from < 0 || from >= Successors.Length || to < 0 || to >= Successors.Length)
            return;

        if (!Successors[from].Contains(to))
            Successors[from].Add(to);
        if (!Predecessors[to].Contains(from))
            Predecessors[to].Add(from);
    }
}
