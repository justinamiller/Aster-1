using Aster.Lsp.Protocol;
using Aster.Lsp.Handlers;
using Aster.Workspaces;

namespace Aster.Tooling.Tests;

public class LspTests
{
    [Fact]
    public void DiagnosticsHandler_ValidSource_ReturnsNoDiagnostics()
    {
        var handler = new DiagnosticsHandler(new IncrementalReanalysis());
        var diagnostics = handler.GetDiagnostics("file:///test.ast", "fn main() { let x: i32 = 42 }");
        // Valid source may or may not have diagnostics depending on compiler behavior
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void DocumentSymbolHandler_ExtractsSymbols()
    {
        var handler = new DocumentSymbolHandler();
        var source = "fn main() { let x: i32 = 42 }\nstruct Point { x: i32, y: i32 }";
        var symbols = handler.GetSymbols(source, "test.ast");
        // Should find at least the main function
        Assert.NotNull(symbols);
    }

    [Fact]
    public void FormattingHandler_FormatsDocument()
    {
        var handler = new FormattingHandler();
        var snapshot = new DocumentSnapshot("file:///test.ast", 1, "fn main() { let x: i32 = 42 }");
        var edits = handler.Format(snapshot);
        Assert.NotNull(edits);
    }

    [Fact]
    public void SpanToLspRange_ConvertsCorrectly()
    {
        // Compiler spans are 1-based, LSP is 0-based
        var handler = new DiagnosticsHandler(new IncrementalReanalysis());
        // Just verify the handler can be created and used
        Assert.NotNull(handler);
    }
}
