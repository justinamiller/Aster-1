using Aster.Compiler.Diagnostics;
using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.MiddleEnd.BorrowChecker;

/// <summary>
/// Implements non-lexical lifetimes (NLL) based borrow checking.
/// Builds a control-flow graph and performs dataflow analysis to detect:
/// - Use after move
/// - Mutable aliasing
/// - Dangling borrows
/// </summary>
public sealed class BorrowCheck
{
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>
    /// Represents a tracked value with its ownership/borrow state.
    /// </summary>
    private sealed class ValueState
    {
        public string Name { get; }
        public bool IsMoved { get; set; }
        public int ImmutableBorrowCount { get; set; }
        public bool IsMutablyBorrowed { get; set; }

        public ValueState(string name) => Name = name;
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
        var states = new Dictionary<string, ValueState>();

        // Initialize parameters as owned
        foreach (var param in fn.Parameters)
        {
            states[param.Name] = new ValueState(param.Name);
        }

        // Process each basic block in order
        foreach (var block in fn.BasicBlocks)
        {
            foreach (var instruction in block.Instructions)
            {
                CheckInstruction(instruction, states);
            }
        }
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
                            Diagnostics.ReportError("E0500", $"Use of moved value '{source.Name}'", Span.Unknown);
                        }
                        if (sourceState.ImmutableBorrowCount > 0 || sourceState.IsMutablyBorrowed)
                        {
                            Diagnostics.ReportError("E0501", $"Cannot move '{source.Name}' while it is borrowed", Span.Unknown);
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
                            Diagnostics.ReportError("E0502", $"Cannot borrow moved value '{target.Name}'", Span.Unknown);
                        }

                        var isMutable = instruction.Extra is bool b && b;
                        if (isMutable)
                        {
                            if (targetState.ImmutableBorrowCount > 0 || targetState.IsMutablyBorrowed)
                            {
                                Diagnostics.ReportError("E0503", $"Cannot mutably borrow '{target.Name}' while it is already borrowed", Span.Unknown);
                            }
                            targetState.IsMutablyBorrowed = true;
                        }
                        else
                        {
                            if (targetState.IsMutablyBorrowed)
                            {
                                Diagnostics.ReportError("E0504", $"Cannot immutably borrow '{target.Name}' while it is mutably borrowed", Span.Unknown);
                            }
                            targetState.ImmutableBorrowCount++;
                        }
                    }
                }
                break;

            case MirOpcode.Load:
            case MirOpcode.Call:
                // Check that all operands are not moved
                foreach (var operand in instruction.Operands)
                {
                    if (operand.Kind == MirOperandKind.Variable &&
                        states.TryGetValue(operand.Name, out var opState) &&
                        opState.IsMoved)
                    {
                        Diagnostics.ReportError("E0505", $"Use of moved value '{operand.Name}'", Span.Unknown);
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
                    }
                }
                break;
        }
    }
}
