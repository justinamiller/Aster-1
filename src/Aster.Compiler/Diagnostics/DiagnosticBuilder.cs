namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Fluent builder for creating diagnostics with rich formatting support.
/// Centralizes all diagnostic formatting to avoid string concatenation at call sites.
/// </summary>
public sealed class DiagnosticBuilder
{
    private readonly string _code;
    private readonly DiagnosticSeverity _severity;
    private string? _title;
    private string? _message;
    private Span _primarySpan = Span.Unknown;
    private DiagnosticCategory _category = DiagnosticCategory.Internal;
    private readonly List<SecondarySpan> _secondarySpans = new();
    private string? _help;
    private readonly List<string> _notes = new();

    private DiagnosticBuilder(string code, DiagnosticSeverity severity)
    {
        _code = code;
        _severity = severity;
    }

    /// <summary>Create an error diagnostic builder.</summary>
    public static DiagnosticBuilder Error(string code)
    {
        return new DiagnosticBuilder(code, DiagnosticSeverity.Error);
    }

    /// <summary>Create a warning diagnostic builder.</summary>
    public static DiagnosticBuilder Warning(string code)
    {
        return new DiagnosticBuilder(code, DiagnosticSeverity.Warning);
    }

    /// <summary>Create an info diagnostic builder.</summary>
    public static DiagnosticBuilder Info(string code)
    {
        return new DiagnosticBuilder(code, DiagnosticSeverity.Info);
    }

    /// <summary>Create a hint diagnostic builder.</summary>
    public static DiagnosticBuilder Hint(string code)
    {
        return new DiagnosticBuilder(code, DiagnosticSeverity.Hint);
    }

    /// <summary>Set the diagnostic title.</summary>
    public DiagnosticBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>Set the diagnostic message with format support.</summary>
    public DiagnosticBuilder Message(string message, params object[] args)
    {
        _message = args.Length > 0 ? string.Format(message, args) : message;
        return this;
    }

    /// <summary>Set the primary span.</summary>
    public DiagnosticBuilder Primary(Span span)
    {
        _primarySpan = span;
        return this;
    }

    /// <summary>Add a secondary span with optional label.</summary>
    public DiagnosticBuilder Secondary(Span span, string? label = null)
    {
        _secondarySpans.Add(new SecondarySpan(span, label));
        return this;
    }

    /// <summary>Set the help text.</summary>
    public DiagnosticBuilder Help(string help)
    {
        _help = help;
        return this;
    }

    /// <summary>Add a note.</summary>
    public DiagnosticBuilder Note(string note)
    {
        _notes.Add(note);
        return this;
    }

    /// <summary>Set the diagnostic category.</summary>
    public DiagnosticBuilder Category(DiagnosticCategory category)
    {
        _category = category;
        return this;
    }

    /// <summary>Build the final diagnostic.</summary>
    public Diagnostic Build()
    {
        var title = _title ?? _message ?? "Unknown error";
        var message = _message ?? title;

        return new Diagnostic(
            _code,
            _severity,
            title,
            message,
            _primarySpan,
            _category,
            _secondarySpans,
            _help,
            _notes);
    }

    /// <summary>Build and report the diagnostic to a bag.</summary>
    public void Report(DiagnosticBag bag)
    {
        bag.Report(Build());
    }
}
