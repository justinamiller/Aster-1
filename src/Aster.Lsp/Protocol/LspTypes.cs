using System.Text.Json.Serialization;

namespace Aster.Lsp.Protocol;

public sealed class InitializeParams
{
    [JsonPropertyName("rootUri")]
    public string? RootUri { get; set; }
}

public sealed class InitializeResult
{
    [JsonPropertyName("capabilities")]
    public ServerCapabilities Capabilities { get; set; } = new();
}

public sealed class ServerCapabilities
{
    [JsonPropertyName("textDocumentSync")]
    public int TextDocumentSync { get; set; } = 1; // Full sync

    [JsonPropertyName("hoverProvider")]
    public bool HoverProvider { get; set; } = true;

    [JsonPropertyName("definitionProvider")]
    public bool DefinitionProvider { get; set; } = true;

    [JsonPropertyName("referencesProvider")]
    public bool ReferencesProvider { get; set; } = true;

    [JsonPropertyName("documentSymbolProvider")]
    public bool DocumentSymbolProvider { get; set; } = true;

    [JsonPropertyName("documentFormattingProvider")]
    public bool DocumentFormattingProvider { get; set; } = true;

    [JsonPropertyName("renameProvider")]
    public bool RenameProvider { get; set; } = true;

    [JsonPropertyName("signatureHelpProvider")]
    public SignatureHelpOptions? SignatureHelpProvider { get; set; } = new();
}

public sealed class SignatureHelpOptions
{
    [JsonPropertyName("triggerCharacters")]
    public string[] TriggerCharacters { get; set; } = ["(", ","];
}

public sealed class TextDocumentIdentifier
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";
}

public sealed class VersionedTextDocumentIdentifier
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";

    [JsonPropertyName("version")]
    public int Version { get; set; }
}

public sealed class TextDocumentItem
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";

    [JsonPropertyName("languageId")]
    public string LanguageId { get; set; } = "";

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public sealed class LspPosition
{
    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("character")]
    public int Character { get; set; }
}

public sealed class LspRange
{
    [JsonPropertyName("start")]
    public LspPosition Start { get; set; } = new();

    [JsonPropertyName("end")]
    public LspPosition End { get; set; } = new();
}

public sealed class LspLocation
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";

    [JsonPropertyName("range")]
    public LspRange Range { get; set; } = new();
}

public sealed class LspDiagnostic
{
    [JsonPropertyName("range")]
    public LspRange Range { get; set; } = new();

    [JsonPropertyName("severity")]
    public int Severity { get; set; } = 1;

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "aster";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}

public sealed class PublishDiagnosticsParams
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = "";

    [JsonPropertyName("diagnostics")]
    public List<LspDiagnostic> Diagnostics { get; set; } = new();
}

public sealed class DocumentSymbol
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("kind")]
    public int Kind { get; set; }

    [JsonPropertyName("range")]
    public LspRange Range { get; set; } = new();

    [JsonPropertyName("selectionRange")]
    public LspRange SelectionRange { get; set; } = new();

    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DocumentSymbol>? Children { get; set; }
}

public sealed class Hover
{
    [JsonPropertyName("contents")]
    public MarkupContent Contents { get; set; } = new();
}

public sealed class MarkupContent
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "markdown";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

public sealed class TextEdit
{
    [JsonPropertyName("range")]
    public LspRange Range { get; set; } = new();

    [JsonPropertyName("newText")]
    public string NewText { get; set; } = "";
}

public sealed class WorkspaceEdit
{
    [JsonPropertyName("changes")]
    public Dictionary<string, List<TextEdit>>? Changes { get; set; }
}
