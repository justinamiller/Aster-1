namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Controls how a lint (warning-category diagnostic) is reported.
/// Mirrors Rust's allow/warn/deny/forbid lint level system.
/// </summary>
public enum LintLevel
{
    /// <summary>Suppress this lint entirely.</summary>
    Allow,
    /// <summary>Emit a warning for this lint (default for most lints).</summary>
    Warn,
    /// <summary>Treat this lint as a hard error.</summary>
    Deny,
    /// <summary>Like Deny but cannot be overridden by inner allow/warn/deny attributes.</summary>
    Forbid,
}

/// <summary>
/// A registered lint rule with its default level and description.
/// </summary>
public sealed class LintRule
{
    /// <summary>Stable lint identifier (e.g. "unused_variables").</summary>
    public string Name { get; }

    /// <summary>Default enforcement level.</summary>
    public LintLevel DefaultLevel { get; }

    /// <summary>Short human-readable description of what this lint checks.</summary>
    public string Description { get; }

    public LintRule(string name, LintLevel defaultLevel, string description)
    {
        Name = name;
        DefaultLevel = defaultLevel;
        Description = description;
    }
}

/// <summary>
/// Registry of all known lint rules and their current effective levels.
/// Supports per-rule overrides via <c>#[allow(...)]</c>, <c>#[warn(...)]</c>,
/// <c>#[deny(...)]</c>, and <c>#[forbid(...)]</c> attributes.
/// </summary>
public sealed class LintRegistry
{
    private readonly Dictionary<string, LintLevel> _overrides = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LintRule> _rules;

    /// <summary>All built-in lint rules.</summary>
    public static readonly IReadOnlyList<LintRule> BuiltinRules = new LintRule[]
    {
        new("unused_variables",       LintLevel.Warn,  "Variable is declared but never used"),
        new("unused_imports",         LintLevel.Warn,  "Import is unused"),
        new("dead_code",              LintLevel.Warn,  "Code is unreachable or unused"),
        new("deprecated",             LintLevel.Warn,  "Item is marked as deprecated"),
        new("non_snake_case",         LintLevel.Warn,  "Identifier does not follow snake_case convention"),
        new("non_camel_case_types",   LintLevel.Warn,  "Type name does not follow CamelCase convention"),
        new("missing_docs",           LintLevel.Allow, "Public item is missing documentation"),
        new("overflow_literals",      LintLevel.Deny,  "Numeric literal overflows its type"),
        new("unconditional_recursion",LintLevel.Warn,  "Function unconditionally recurses to itself"),
    };

    public LintRegistry()
    {
        _rules = BuiltinRules.ToDictionary(r => r.Name, r => r, StringComparer.Ordinal);
    }

    /// <summary>Get the effective level for a lint, respecting user overrides.</summary>
    public LintLevel GetLevel(string lintName)
    {
        if (_overrides.TryGetValue(lintName, out var level))
            return level;
        if (_rules.TryGetValue(lintName, out var rule))
            return rule.DefaultLevel;
        return LintLevel.Allow; // unknown lints are silently allowed
    }

    /// <summary>
    /// Override a lint's level.  Returns false if the lint is currently Forbid
    /// (Forbid cannot be downgraded).
    /// </summary>
    public bool TrySetLevel(string lintName, LintLevel newLevel)
    {
        // Forbid: cannot be overridden once set
        if (_overrides.TryGetValue(lintName, out var existing) && existing == LintLevel.Forbid)
            return false;

        _overrides[lintName] = newLevel;
        return true;
    }

    /// <summary>Returns true if the given lint is known to this registry.</summary>
    public bool IsKnown(string lintName) => _rules.ContainsKey(lintName);

    /// <summary>Convert a lint diagnostic (W-code) to a <see cref="DiagnosticSeverity"/>.</summary>
    public DiagnosticSeverity ToSeverity(string lintName) => GetLevel(lintName) switch
    {
        LintLevel.Allow  => DiagnosticSeverity.Hint,   // effectively suppressed
        LintLevel.Warn   => DiagnosticSeverity.Warning,
        LintLevel.Deny   => DiagnosticSeverity.Error,
        LintLevel.Forbid => DiagnosticSeverity.Error,
        _ => DiagnosticSeverity.Warning,
    };
}
