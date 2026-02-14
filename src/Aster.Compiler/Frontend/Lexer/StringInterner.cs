namespace Aster.Compiler.Frontend.Lexer;

/// <summary>
/// Interns string identifiers to reduce allocations and allow reference equality checks.
/// Uses a dictionary-based pool for deduplication.
/// </summary>
public sealed class StringInterner
{
    private readonly Dictionary<string, string> _pool = new(StringComparer.Ordinal);

    /// <summary>
    /// Intern a string. Returns the canonical instance.
    /// </summary>
    public string Intern(string value)
    {
        if (_pool.TryGetValue(value, out var existing))
            return existing;

        _pool[value] = value;
        return value;
    }

    /// <summary>
    /// Intern a span of characters.
    /// </summary>
    public string Intern(ReadOnlySpan<char> span)
    {
        var str = span.ToString();
        return Intern(str);
    }

    /// <summary>Number of interned strings.</summary>
    public int Count => _pool.Count;
}
