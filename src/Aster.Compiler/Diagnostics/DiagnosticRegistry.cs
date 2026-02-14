namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Central registry of all diagnostic codes with metadata and documentation.
/// Ensures code stability and provides quick lookup for explanations.
/// </summary>
public static class DiagnosticRegistry
{
    private static readonly Dictionary<string, DiagnosticMetadata> _registry = new()
    {
        // Syntax Errors (E1xxx)
        [DiagnosticCode.E1000] = new("Unexpected token", DiagnosticCategory.Syntax),
        [DiagnosticCode.E1001] = new("Expected token", DiagnosticCategory.Syntax),
        [DiagnosticCode.E1002] = new("Unclosed delimiter", DiagnosticCategory.Syntax),
        [DiagnosticCode.E1003] = new("Invalid literal", DiagnosticCategory.Syntax),
        [DiagnosticCode.E1004] = new("Invalid escape sequence", DiagnosticCategory.Syntax),
        [DiagnosticCode.E1005] = new("Invalid character", DiagnosticCategory.Syntax),
        [DiagnosticCode.E1006] = new("Unterminated string", DiagnosticCategory.Syntax),
        [DiagnosticCode.E1007] = new("Unterminated comment", DiagnosticCategory.Syntax),
        [DiagnosticCode.E1008] = new("Invalid number format", DiagnosticCategory.Syntax),

        // Name Resolution (E2xxx)
        [DiagnosticCode.E2000] = new("Undefined name", DiagnosticCategory.NameResolution),
        [DiagnosticCode.E2001] = new("Duplicate definition", DiagnosticCategory.NameResolution),
        [DiagnosticCode.E2002] = new("Ambiguous name", DiagnosticCategory.NameResolution),
        [DiagnosticCode.E2003] = new("Undefined module", DiagnosticCategory.NameResolution),
        [DiagnosticCode.E2004] = new("Cyclic module dependency", DiagnosticCategory.NameResolution),
        [DiagnosticCode.E2005] = new("Private item access", DiagnosticCategory.NameResolution),
        [DiagnosticCode.E2006] = new("Item not found in module", DiagnosticCategory.NameResolution),

        // Type System (E3xxx)
        [DiagnosticCode.E3000] = new("Type mismatch", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3001] = new("Cannot infer type", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3002] = new("Invalid type argument count", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3003] = new("Type parameter bound not satisfied", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3010] = new("Cannot unify types", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3011] = new("Occurs check failed (infinite type)", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3100] = new("Function return type mismatch", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3101] = new("Variable assignment type mismatch", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3102] = new("Function argument count mismatch", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3103] = new("Function argument type mismatch", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3104] = new("If condition must be bool", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3105] = new("Type has no such field", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.E3124] = new("Cannot unify types", DiagnosticCategory.TypeSystem),

        // Trait System (E4xxx)
        [DiagnosticCode.E4000] = new("Type does not implement trait", DiagnosticCategory.Traits),
        [DiagnosticCode.E4001] = new("Trait method not implemented", DiagnosticCategory.Traits),
        [DiagnosticCode.E4002] = new("Conflicting trait implementations", DiagnosticCategory.Traits),
        [DiagnosticCode.E4003] = new("Orphan trait implementation", DiagnosticCategory.Traits),
        [DiagnosticCode.E4020] = new("Cycle detected in trait resolution", DiagnosticCategory.Traits),
        [DiagnosticCode.E4021] = new("Type does not implement required trait", DiagnosticCategory.Traits),

        // Effect System (E5xxx)
        [DiagnosticCode.E5000] = new("Function has undeclared effects", DiagnosticCategory.Effects),
        [DiagnosticCode.E5001] = new("Effect not allowed in this context", DiagnosticCategory.Effects),
        [DiagnosticCode.E5002] = new("Async function called in sync context", DiagnosticCategory.Effects),
        [DiagnosticCode.E5003] = new("Unsafe operation in safe context", DiagnosticCategory.Effects),

        // Ownership (E6xxx)
        [DiagnosticCode.E6000] = new("Use of moved value", DiagnosticCategory.Ownership),
        [DiagnosticCode.E6001] = new("Cannot move while borrowed", DiagnosticCategory.Ownership),
        [DiagnosticCode.E6002] = new("Cannot borrow moved value", DiagnosticCategory.Ownership),
        [DiagnosticCode.E6003] = new("Cannot immutably borrow while mutably borrowed", DiagnosticCategory.Ownership),
        [DiagnosticCode.E6004] = new("Cannot borrow value", DiagnosticCategory.Ownership),
        [DiagnosticCode.E6005] = new("Cannot mutably borrow while already borrowed", DiagnosticCategory.Ownership),

        // Borrow Checking (E7xxx)
        [DiagnosticCode.E7000] = new("Use of moved value", DiagnosticCategory.BorrowChecking),
        [DiagnosticCode.E7001] = new("Cannot move while borrowed", DiagnosticCategory.BorrowChecking),
        [DiagnosticCode.E7002] = new("Cannot borrow moved value", DiagnosticCategory.BorrowChecking),
        [DiagnosticCode.E7003] = new("Cannot mutably borrow while already borrowed", DiagnosticCategory.BorrowChecking),
        [DiagnosticCode.E7004] = new("Cannot immutably borrow while mutably borrowed", DiagnosticCategory.BorrowChecking),
        [DiagnosticCode.E7005] = new("Borrow outlives its referent", DiagnosticCategory.BorrowChecking),
        [DiagnosticCode.E7006] = new("Dangling reference", DiagnosticCategory.BorrowChecking),

        // Pattern Matching (E8xxx)
        [DiagnosticCode.E8000] = new("Match expression has no arms", DiagnosticCategory.Patterns),
        [DiagnosticCode.E8001] = new("Non-exhaustive match", DiagnosticCategory.Patterns),
        [DiagnosticCode.E8002] = new("Unreachable pattern", DiagnosticCategory.Patterns),
        [DiagnosticCode.E8003] = new("Invalid pattern for type", DiagnosticCategory.Patterns),

        // MIR/Backend (E9xxx)
        [DiagnosticCode.E9000] = new("Invalid MIR structure", DiagnosticCategory.MIR),
        [DiagnosticCode.E9001] = new("Codegen error", DiagnosticCategory.Codegen),
        [DiagnosticCode.E9002] = new("Optimization error", DiagnosticCategory.MIR),

        // Warnings
        [DiagnosticCode.W0001] = new("Unreachable pattern", DiagnosticCategory.Patterns),
        [DiagnosticCode.W0002] = new("Unused variable", DiagnosticCategory.Lint),
        [DiagnosticCode.W0003] = new("Unused function", DiagnosticCategory.Lint),
        [DiagnosticCode.W0004] = new("Dead code", DiagnosticCategory.Lint),
        [DiagnosticCode.W0005] = new("Deprecated item", DiagnosticCategory.Lint),
        [DiagnosticCode.W1000] = new("Style violation", DiagnosticCategory.Lint),
        [DiagnosticCode.W1001] = new("Naming convention", DiagnosticCategory.Lint),
        [DiagnosticCode.W1002] = new("Missing documentation", DiagnosticCategory.Lint),
        [DiagnosticCode.W2000] = new("Missing dependency", DiagnosticCategory.Package),
        [DiagnosticCode.W2001] = new("Version conflict", DiagnosticCategory.Package),

        // Info
        [DiagnosticCode.I0001] = new("Inferred type", DiagnosticCategory.TypeSystem),
        [DiagnosticCode.I0002] = new("Optimization applied", DiagnosticCategory.MIR),
        [DiagnosticCode.I0003] = new("Information message", DiagnosticCategory.Internal),

        // Internal
        [DiagnosticCode.E0000] = new("Internal compiler error", DiagnosticCategory.Internal),
        [DiagnosticCode.E0001] = new("Assertion failed", DiagnosticCategory.Internal),
    };

    /// <summary>Get metadata for a diagnostic code.</summary>
    public static DiagnosticMetadata? GetMetadata(string code)
    {
        return _registry.TryGetValue(code, out var metadata) ? metadata : null;
    }

    /// <summary>Get the category for a diagnostic code.</summary>
    public static DiagnosticCategory GetCategory(string code)
    {
        return GetMetadata(code)?.Category ?? DiagnosticCategory.Internal;
    }

    /// <summary>Get the title for a diagnostic code.</summary>
    public static string GetTitle(string code)
    {
        return GetMetadata(code)?.Title ?? "Unknown diagnostic";
    }

    /// <summary>Check if a code is registered.</summary>
    public static bool IsRegistered(string code)
    {
        return _registry.ContainsKey(code);
    }

    /// <summary>Get all registered codes.</summary>
    public static IEnumerable<string> GetAllCodes()
    {
        return _registry.Keys;
    }
}

/// <summary>
/// Metadata for a diagnostic code.
/// </summary>
public sealed class DiagnosticMetadata
{
    public string Title { get; }
    public DiagnosticCategory Category { get; }

    public DiagnosticMetadata(string title, DiagnosticCategory category)
    {
        Title = title;
        Category = category;
    }
}
