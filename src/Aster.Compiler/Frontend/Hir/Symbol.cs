namespace Aster.Compiler.Frontend.Hir;

/// <summary>
/// Represents a resolved symbol in the compiler.
/// </summary>
public sealed class Symbol
{
    private static int _nextId;

    /// <summary>Unique symbol ID.</summary>
    public int Id { get; }

    /// <summary>Name as it appears in source.</summary>
    public string Name { get; }

    /// <summary>Kind of symbol (Type, Value, Function, etc.).</summary>
    public SymbolKind Kind { get; }

    /// <summary>Whether the symbol is publicly visible.</summary>
    public bool IsPublic { get; }

    /// <summary>Resolved type (set during type checking).</summary>
    public TypeSystem.AsterType? Type { get; set; }

    public Symbol(string name, SymbolKind kind, bool isPublic = false)
    {
        Id = Interlocked.Increment(ref _nextId);
        Name = name;
        Kind = kind;
        IsPublic = isPublic;
    }

    public override string ToString() => $"{Kind}:{Name}#{Id}";
}

/// <summary>
/// Kinds of symbols in the Aster language.
/// </summary>
public enum SymbolKind
{
    Type,
    Value,
    Function,
    Trait,
    Module,
    Parameter,
    Field,
}
