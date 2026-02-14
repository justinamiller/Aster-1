namespace Aster.Cli.Diagnostics;

/// <summary>
/// Provides detailed explanations for diagnostic error codes.
/// </summary>
public static class DiagnosticExplainer
{
    private static readonly Dictionary<string, DiagnosticExplanation> _explanations = new()
    {
        ["E3124"] = new DiagnosticExplanation
        {
            Code = "E3124",
            Title = "Cannot unify types",
            Explanation = @"
This error occurs when the type checker cannot reconcile two types that are expected
to be the same. Type unification is the process by which the compiler determines if
two types can be considered equivalent.

Common causes:
1. Assigning a value of one type to a variable of an incompatible type
2. Returning a value from a function that doesn't match the declared return type
3. Passing arguments to a function that don't match the parameter types
4. Mismatched types in conditional expressions (if/else branches)

The compiler uses Hindley-Milner type inference to deduce types automatically,
but when explicit type annotations conflict with inferred types, this error occurs.
",
            Example = @"
// Error: cannot unify i32 and string
fn main() {
    let x: i32 = ""hello""  // string cannot be unified with i32
}
",
            Fix = @"
// Fix 1: Use correct type
fn main() {
    let x: i32 = 42
}

// Fix 2: Change the type annotation
fn main() {
    let x: string = ""hello""
}

// Fix 3: Convert the value
fn main() {
    let x: i32 = parse_int(""42"")
}
",
            RelatedCodes = new[] { "E3000", "E3001", "E3010" }
        },

        ["E3000"] = new DiagnosticExplanation
        {
            Code = "E3000",
            Title = "Type mismatch",
            Explanation = @"
This error occurs when a value of one type is used where a different type is expected.
This is a general type error that indicates an incompatibility between expected and
actual types.
",
            Example = @"
fn add(a: i32, b: i32) -> i32 {
    a + b
}

fn main() {
    add(5, ""hello"")  // Error: expected i32, found string
}
",
            Fix = @"
fn main() {
    add(5, 10)  // Both arguments are i32
}
",
            RelatedCodes = new[] { "E3124", "E3103" }
        },

        ["E6000"] = new DiagnosticExplanation
        {
            Code = "E6000",
            Title = "Use of moved value",
            Explanation = @"
This error occurs when you try to use a value after it has been moved. In Aster's
ownership system, values can be moved from one location to another, and after a
move, the original location becomes invalid.

Values are moved when:
1. They are assigned to a new variable
2. They are passed to a function (unless the type implements Copy)
3. They are returned from a function
",
            Example = @"
fn main() {
    let s = ""hello""
    let t = s       // s is moved to t
    print(s)        // Error: s has been moved
}
",
            Fix = @"
// Fix 1: Use a reference instead of moving
fn main() {
    let s = ""hello""
    let t = &s      // Borrow instead of move
    print(s)        // OK: s is still valid
}

// Fix 2: Clone the value
fn main() {
    let s = ""hello""
    let t = s.clone()  // Create a copy
    print(s)           // OK: s is still valid
}

// Fix 3: Use after move
fn main() {
    let s = ""hello""
    let t = s
    print(t)        // Use t instead of s
}
",
            RelatedCodes = new[] { "E6001", "E6002", "E7000" }
        },

        ["E8001"] = new DiagnosticExplanation
        {
            Code = "E8001",
            Title = "Non-exhaustive match",
            Explanation = @"
This error occurs when a match expression doesn't cover all possible values of
the type being matched. The compiler requires that all match expressions are
exhaustive to prevent runtime errors from unhandled cases.

For enums, you must match all variants. For bools, you must match both true
and false. You can use a wildcard pattern (_) to match any remaining cases.
",
            Example = @"
enum Color {
    Red,
    Green,
    Blue
}

fn describe(c: Color) -> string {
    match c {
        Red => ""red"",
        Green => ""green""
        // Error: missing Blue case
    }
}
",
            Fix = @"
// Fix 1: Add missing case
fn describe(c: Color) -> string {
    match c {
        Red => ""red"",
        Green => ""green"",
        Blue => ""blue""
    }
}

// Fix 2: Use wildcard
fn describe(c: Color) -> string {
    match c {
        Red => ""red"",
        _ => ""other""
    }
}
",
            RelatedCodes = new[] { "E8000", "W0001" }
        }
    };

    public static DiagnosticExplanation? GetExplanation(string code)
    {
        return _explanations.TryGetValue(code, out var explanation) ? explanation : null;
    }

    public static bool HasExplanation(string code)
    {
        return _explanations.ContainsKey(code);
    }

    public static string Format(DiagnosticExplanation explanation)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Error Code: {explanation.Code}");
        sb.AppendLine($"Title: {explanation.Title}");
        sb.AppendLine();
        sb.AppendLine("Explanation:");
        sb.AppendLine(explanation.Explanation.Trim());
        sb.AppendLine();

        if (!string.IsNullOrEmpty(explanation.Example))
        {
            sb.AppendLine("Example that triggers this error:");
            sb.AppendLine(explanation.Example.Trim());
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(explanation.Fix))
        {
            sb.AppendLine("How to fix:");
            sb.AppendLine(explanation.Fix.Trim());
            sb.AppendLine();
        }

        if (explanation.RelatedCodes.Length > 0)
        {
            sb.AppendLine("Related error codes:");
            foreach (var code in explanation.RelatedCodes)
            {
                sb.AppendLine($"  - {code}");
            }
        }

        return sb.ToString();
    }
}

public sealed class DiagnosticExplanation
{
    public string Code { get; init; } = "";
    public string Title { get; init; } = "";
    public string Explanation { get; init; } = "";
    public string Example { get; init; } = "";
    public string Fix { get; init; } = "";
    public string[] RelatedCodes { get; init; } = Array.Empty<string>();
}
