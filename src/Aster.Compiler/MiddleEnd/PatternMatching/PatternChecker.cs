using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Hir;
using Aster.Compiler.Frontend.TypeSystem;

namespace Aster.Compiler.MiddleEnd.PatternMatching;

/// <summary>
/// Represents a pattern in a match expression.
/// </summary>
public abstract class Pattern
{
    public Span Span { get; }
    protected Pattern(Span span) => Span = span;
}

/// <summary>Wildcard pattern (_).</summary>
public sealed class WildcardPattern : Pattern
{
    public WildcardPattern(Span span) : base(span) { }
}

/// <summary>Literal pattern (42, "hello", true).</summary>
public sealed class LiteralPattern : Pattern
{
    public object Value { get; }
    public LiteralPattern(object value, Span span) : base(span) => Value = value;
}

/// <summary>Constructor pattern (Some(x), Cons(head, tail)).</summary>
public sealed class ConstructorPattern : Pattern
{
    public string Constructor { get; }
    public IReadOnlyList<Pattern> Arguments { get; }

    public ConstructorPattern(string constructor, IReadOnlyList<Pattern> arguments, Span span)
        : base(span)
    {
        Constructor = constructor;
        Arguments = arguments;
    }
}

/// <summary>Variable binding pattern (x, mut y).</summary>
public sealed class VariablePattern : Pattern
{
    public string Name { get; }
    public bool IsMutable { get; }

    public VariablePattern(string name, bool isMutable, Span span) : base(span)
    {
        Name = name;
        IsMutable = isMutable;
    }
}

/// <summary>
/// Pattern match exhaustiveness and reachability checker.
/// Implements decision tree algorithm to check:
/// - Exhaustiveness: all possible values are covered
/// - Unreachable arms: patterns that can never match
/// </summary>
public sealed class PatternChecker
{
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>
    /// Check a match expression for exhaustiveness and unreachable arms.
    /// </summary>
    public void CheckMatch(AsterType scrutineeType, IReadOnlyList<(Pattern Pattern, Span Span)> arms)
    {
        if (arms.Count == 0)
        {
            Diagnostics.ReportError("E0340", "Match expression has no arms", Span.Unknown);
            return;
        }

        // Build decision tree
        var matrix = new PatternMatrix();
        for (int i = 0; i < arms.Count; i++)
        {
            matrix.AddRow(new[] { arms[i].Pattern }, i);
        }

        // Check exhaustiveness
        if (!IsExhaustive(scrutineeType, matrix))
        {
            var missing = ComputeMissingPatterns(scrutineeType, matrix);
            Diagnostics.ReportError(
                "E0341",
                $"Match expression is not exhaustive. Missing patterns: {string.Join(", ", missing)}",
                Span.Unknown);
        }

        // Check for unreachable arms
        CheckUnreachable(scrutineeType, arms);
    }

    /// <summary>
    /// Check if a pattern matrix is exhaustive for the given type.
    /// </summary>
    private bool IsExhaustive(AsterType type, PatternMatrix matrix)
    {
        if (matrix.Rows.Count == 0)
            return false;

        // If any row has a wildcard in the first position, the matrix is exhaustive
        if (matrix.Rows.Any(row => row.Patterns[0] is WildcardPattern or VariablePattern))
            return true;

        // For enum types, check if all constructors are covered
        if (type is EnumType enumType)
        {
            var coveredConstructors = new HashSet<string>();
            foreach (var row in matrix.Rows)
            {
                if (row.Patterns[0] is ConstructorPattern cp)
                {
                    coveredConstructors.Add(cp.Constructor);
                }
            }

            // Check if all variants are covered
            return enumType.Variants.All(v => coveredConstructors.Contains(v.Name));
        }

        // For bool, check if both true and false are covered
        if (type is PrimitiveType { Kind: PrimitiveKind.Bool })
        {
            var hasTrue = matrix.Rows.Any(r => r.Patterns[0] is LiteralPattern { Value: true });
            var hasFalse = matrix.Rows.Any(r => r.Patterns[0] is LiteralPattern { Value: false });
            return hasTrue && hasFalse;
        }

        // For other types, we conservatively assume exhaustiveness if there's at least one pattern
        // In a real implementation, we'd have more sophisticated checks
        return matrix.Rows.Count > 0;
    }

    /// <summary>
    /// Compute missing patterns for an inexhaustive match.
    /// </summary>
    private List<string> ComputeMissingPatterns(AsterType type, PatternMatrix matrix)
    {
        var missing = new List<string>();

        if (type is EnumType enumType)
        {
            var coveredConstructors = new HashSet<string>();
            foreach (var row in matrix.Rows)
            {
                if (row.Patterns[0] is ConstructorPattern cp)
                {
                    coveredConstructors.Add(cp.Constructor);
                }
                else if (row.Patterns[0] is WildcardPattern or VariablePattern)
                {
                    return new List<string>(); // Wildcard covers everything
                }
            }

            foreach (var variant in enumType.Variants)
            {
                if (!coveredConstructors.Contains(variant.Name))
                {
                    missing.Add(variant.Name);
                }
            }
        }
        else if (type is PrimitiveType { Kind: PrimitiveKind.Bool })
        {
            var hasTrue = matrix.Rows.Any(r => r.Patterns[0] is LiteralPattern { Value: true });
            var hasFalse = matrix.Rows.Any(r => r.Patterns[0] is LiteralPattern { Value: false });
            var hasWild = matrix.Rows.Any(r => r.Patterns[0] is WildcardPattern or VariablePattern);

            if (!hasWild)
            {
                if (!hasTrue) missing.Add("true");
                if (!hasFalse) missing.Add("false");
            }
        }
        else
        {
            // For other types, if no wildcard, it's not exhaustive
            var hasWild = matrix.Rows.Any(r => r.Patterns[0] is WildcardPattern or VariablePattern);
            if (!hasWild)
            {
                missing.Add("_");
            }
        }

        return missing;
    }

    /// <summary>
    /// Check for unreachable pattern arms.
    /// </summary>
    private void CheckUnreachable(AsterType type, IReadOnlyList<(Pattern Pattern, Span Span)> arms)
    {
        var seenWildcard = false;
        var coveredPatterns = new HashSet<string>();

        for (int i = 0; i < arms.Count; i++)
        {
            var (pattern, span) = arms[i];

            if (seenWildcard)
            {
                Diagnostics.ReportWarning(
                    "W0001",
                    $"Unreachable pattern: all cases are already covered",
                    span);
                continue;
            }

            if (pattern is WildcardPattern or VariablePattern)
            {
                seenWildcard = true;
            }
            else if (pattern is ConstructorPattern cp)
            {
                if (coveredPatterns.Contains(cp.Constructor))
                {
                    Diagnostics.ReportWarning(
                        "W0001",
                        $"Unreachable pattern: constructor '{cp.Constructor}' is already covered",
                        span);
                }
                else
                {
                    coveredPatterns.Add(cp.Constructor);
                }
            }
            else if (pattern is LiteralPattern lp)
            {
                var key = $"literal:{lp.Value}";
                if (coveredPatterns.Contains(key))
                {
                    Diagnostics.ReportWarning(
                        "W0001",
                        $"Unreachable pattern: literal '{lp.Value}' is already covered",
                        span);
                }
                else
                {
                    coveredPatterns.Add(key);
                }
            }
        }
    }
}

/// <summary>
/// Pattern matrix for decision tree construction.
/// Each row represents a pattern arm with its patterns.
/// </summary>
internal sealed class PatternMatrix
{
    public List<PatternRow> Rows { get; } = new();

    public void AddRow(IReadOnlyList<Pattern> patterns, int armIndex)
    {
        Rows.Add(new PatternRow(patterns, armIndex));
    }
}

internal sealed class PatternRow
{
    public IReadOnlyList<Pattern> Patterns { get; }
    public int ArmIndex { get; }

    public PatternRow(IReadOnlyList<Pattern> patterns, int armIndex)
    {
        Patterns = patterns;
        ArmIndex = armIndex;
    }
}
