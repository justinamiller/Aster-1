using Aster.Compiler.Diagnostics;

namespace Aster.Compiler.Frontend.Hir;

/// <summary>
/// Represents a lexical scope containing symbol definitions.
/// Supports nested scopes via parent chaining.
/// </summary>
public sealed class Scope
{
    private readonly Dictionary<string, Symbol> _symbols = new(StringComparer.Ordinal);
    private readonly Scope? _parent;

    /// <summary>Kind of scope.</summary>
    public ScopeKind Kind { get; }

    public Scope(ScopeKind kind, Scope? parent = null)
    {
        Kind = kind;
        _parent = parent;
    }

    /// <summary>Define a symbol in this scope. Returns false if duplicate.</summary>
    public bool Define(Symbol symbol)
    {
        return _symbols.TryAdd(symbol.Name, symbol);
    }

    /// <summary>Look up a symbol in this scope and parent scopes.</summary>
    public Symbol? Lookup(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
            return symbol;
        return _parent?.Lookup(name);
    }

    /// <summary>Look up a symbol only in this scope (not parents).</summary>
    public Symbol? LookupLocal(string name)
    {
        _symbols.TryGetValue(name, out var symbol);
        return symbol;
    }

    /// <summary>Create a child scope.</summary>
    public Scope CreateChild(ScopeKind kind) => new(kind, this);

    /// <summary>Get all symbols defined in this scope.</summary>
    public IEnumerable<Symbol> GetSymbols() => _symbols.Values;
}

/// <summary>
/// Kinds of scopes in the Aster language.
/// </summary>
public enum ScopeKind
{
    Module,
    Function,
    Block,
    Trait,
    Impl,
}
