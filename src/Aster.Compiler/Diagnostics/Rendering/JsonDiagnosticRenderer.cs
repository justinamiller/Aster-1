using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aster.Compiler.Diagnostics.Rendering;

/// <summary>
/// Renders diagnostics in JSON format for machine consumption.
/// Compatible with common tooling and IDE integration.
/// </summary>
public sealed class JsonDiagnosticRenderer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Render(Diagnostic diagnostic)
    {
        var dto = CreateDto(diagnostic);
        return JsonSerializer.Serialize(dto, _jsonOptions);
    }

    public string RenderAll(IEnumerable<Diagnostic> diagnostics)
    {
        var dtos = diagnostics.Select(CreateDto).ToList();
        return JsonSerializer.Serialize(dtos, _jsonOptions);
    }

    private DiagnosticDto CreateDto(Diagnostic diagnostic)
    {
        return new DiagnosticDto
        {
            Code = diagnostic.Code,
            Severity = diagnostic.Severity.ToString().ToLowerInvariant(),
            Title = diagnostic.Title,
            Message = diagnostic.Message,
            Category = diagnostic.Category.ToString(),
            Spans = new SpanDto
            {
                Primary = CreateSpanDto(diagnostic.PrimarySpan),
                Secondary = diagnostic.SecondarySpans
                    .Select(s => new SecondarySpanDto
                    {
                        Span = CreateSpanDto(s.Span),
                        Label = s.Label
                    })
                    .ToList()
            },
            Help = diagnostic.Help,
            Notes = diagnostic.Notes.ToList()
        };
    }

    private SpanLocationDto CreateSpanDto(Span span)
    {
        return new SpanLocationDto
        {
            File = span.File,
            Line = span.Line,
            Column = span.Column,
            Start = span.Start,
            Length = span.Length
        };
    }

    // DTOs for JSON serialization
    private class DiagnosticDto
    {
        public string Code { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Category { get; set; } = "";
        public SpanDto Spans { get; set; } = new();
        public string? Help { get; set; }
        public List<string> Notes { get; set; } = new();
    }

    private class SpanDto
    {
        public SpanLocationDto Primary { get; set; } = new();
        public List<SecondarySpanDto> Secondary { get; set; } = new();
    }

    private class SpanLocationDto
    {
        public string File { get; set; } = "";
        public int Line { get; set; }
        public int Column { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
    }

    private class SecondarySpanDto
    {
        public SpanLocationDto Span { get; set; } = new();
        public string? Label { get; set; }
    }
}
