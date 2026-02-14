namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Severity level for compiler diagnostics.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational hint.</summary>
    Hint,

    /// <summary>Warning that does not prevent compilation.</summary>
    Warning,

    /// <summary>Error that prevents compilation.</summary>
    Error,
}
