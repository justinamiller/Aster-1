using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Hir;

namespace Aster.Compiler.Frontend.Ownership;

/// <summary>
/// Ownership state of a value.
/// </summary>
public enum OwnershipState
{
    /// <summary>Value is owned and valid.</summary>
    Owned,

    /// <summary>Value has been moved and is no longer valid.</summary>
    Moved,

    /// <summary>Value is borrowed immutably.</summary>
    BorrowedImmutable,

    /// <summary>Value is borrowed mutably.</summary>
    BorrowedMutable,
}

/// <summary>
/// Tracks ownership state for values during compilation.
/// </summary>
public sealed class OwnershipTracker
{
    private readonly Dictionary<int, OwnershipState> _states = new();
    private readonly Dictionary<int, int> _borrowCounts = new();
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>Register a new owned value.</summary>
    public void RegisterOwned(Symbol symbol)
    {
        _states[symbol.Id] = OwnershipState.Owned;
        _borrowCounts[symbol.Id] = 0;
    }

    /// <summary>Move a value, invalidating the source.</summary>
    public void Move(Symbol source, Span span)
    {
        if (_states.TryGetValue(source.Id, out var state) && state == OwnershipState.Moved)
        {
            Diagnostics.ReportError("E0400", $"Use of moved value '{source.Name}'", span);
            return;
        }

        if (state == OwnershipState.BorrowedImmutable || state == OwnershipState.BorrowedMutable)
        {
            Diagnostics.ReportError("E0401", $"Cannot move '{source.Name}' while it is borrowed", span);
            return;
        }

        _states[source.Id] = OwnershipState.Moved;
    }

    /// <summary>Borrow a value immutably.</summary>
    public void BorrowImmutable(Symbol symbol, Span span)
    {
        if (_states.TryGetValue(symbol.Id, out var state))
        {
            if (state == OwnershipState.Moved)
            {
                Diagnostics.ReportError("E0402", $"Cannot borrow moved value '{symbol.Name}'", span);
                return;
            }
            if (state == OwnershipState.BorrowedMutable)
            {
                Diagnostics.ReportError("E0403", $"Cannot immutably borrow '{symbol.Name}' while it is mutably borrowed", span);
                return;
            }
        }

        _states[symbol.Id] = OwnershipState.BorrowedImmutable;
        _borrowCounts[symbol.Id] = _borrowCounts.GetValueOrDefault(symbol.Id) + 1;
    }

    /// <summary>Borrow a value mutably.</summary>
    public void BorrowMutable(Symbol symbol, Span span)
    {
        if (_states.TryGetValue(symbol.Id, out var state))
        {
            if (state == OwnershipState.Moved)
            {
                Diagnostics.ReportError("E0404", $"Cannot borrow moved value '{symbol.Name}'", span);
                return;
            }
            if (state == OwnershipState.BorrowedImmutable || state == OwnershipState.BorrowedMutable)
            {
                Diagnostics.ReportError("E0405", $"Cannot mutably borrow '{symbol.Name}' while it is already borrowed", span);
                return;
            }
        }

        _states[symbol.Id] = OwnershipState.BorrowedMutable;
    }

    /// <summary>Release a borrow.</summary>
    public void ReleaseBorrow(Symbol symbol)
    {
        if (_borrowCounts.TryGetValue(symbol.Id, out var count))
        {
            count--;
            _borrowCounts[symbol.Id] = count;
            if (count <= 0)
                _states[symbol.Id] = OwnershipState.Owned;
        }
    }

    /// <summary>Get the ownership state of a symbol.</summary>
    public OwnershipState GetState(Symbol symbol) =>
        _states.TryGetValue(symbol.Id, out var state) ? state : OwnershipState.Owned;
}
