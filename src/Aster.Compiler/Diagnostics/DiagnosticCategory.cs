namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Categories of compiler diagnostics, used for grouping and filtering.
/// </summary>
public enum DiagnosticCategory
{
    /// <summary>Syntax errors during lexing and parsing.</summary>
    Syntax,

    /// <summary>Name resolution and scope errors.</summary>
    NameResolution,

    /// <summary>Type system errors.</summary>
    TypeSystem,

    /// <summary>Trait resolution and implementation errors.</summary>
    Traits,

    /// <summary>Effect system errors.</summary>
    Effects,

    /// <summary>Ownership errors.</summary>
    Ownership,

    /// <summary>Borrow checking errors.</summary>
    BorrowChecking,

    /// <summary>Pattern matching errors.</summary>
    Patterns,

    /// <summary>MIR errors.</summary>
    MIR,

    /// <summary>Code generation errors.</summary>
    Codegen,

    /// <summary>Lint warnings.</summary>
    Lint,

    /// <summary>Package management.</summary>
    Package,

    /// <summary>Toolchain issues.</summary>
    Toolchain,

    /// <summary>Internal compiler errors.</summary>
    Internal
}
