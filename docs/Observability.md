# Aster Compiler Observability & Diagnostics

This document describes the comprehensive observability and diagnostics system for the Aster compiler.

## Overview

The Aster compiler provides world-class diagnostics and observability features comparable to Rust and Swift:

- **Precise, actionable diagnostics** with stable error codes
- **Deep compiler introspection** with selective tracing
- **Performance telemetry** for all compilation phases
- **Crash reporting** with detailed context
- **IDE integration** via LSP
- **Human and machine-readable** output formats

## Architecture

### Projects

- **Aster.Compiler.Diagnostics** - Core diagnostic model
- **Aster.Compiler.Telemetry** - Performance metrics
- **Aster.Compiler.Observability** - Tracing and crash reporting
- **Aster.Cli.Diagnostics** - CLI utilities and explain mode

### Components

1. **Diagnostic Model**
   - Stable error codes (E####, W####, I####)
   - Rich context (primary/secondary spans, help text, notes)
   - Categorization by compiler phase
   - Builder API for fluent construction

2. **Rendering Engine**
   - Human-readable format with ANSI colors
   - JSON format for tooling integration
   - Source context with syntax highlighting
   - Customizable output options

3. **Telemetry System**
   - Per-phase timing and metrics
   - Memory allocation tracking
   - Cache hit/miss statistics
   - Human and JSON output

4. **Tracing System**
   - Zero-overhead when disabled
   - Selective category tracing
   - Automatic scope indentation
   - Thread-safe output

5. **Crash Reporting**
   - Comprehensive crash reports
   - Version and environment info
   - Recent diagnostics context
   - Reproduction commands

## Usage

### Basic Compilation

```bash
# Compile with default settings
aster build hello.ast

# Type-check only
aster check hello.ast
```

### Diagnostic Output

```bash
# Human-readable output (default)
aster build --format=human hello.ast

# Machine-readable JSON
aster build --format=json hello.ast

# Disable colors
aster build --color=never hello.ast

# Force colors even in non-TTY
aster build --color=always hello.ast
```

### Performance Analysis

```bash
# Show compilation timing
aster build --time hello.ast

# Show detailed metrics
aster build --metrics hello.ast

# JSON metrics for tooling
aster build --metrics --format=json hello.ast
```

### Tracing

```bash
# Trace type checking
aster build --trace=types hello.ast

# Trace borrow checker
aster build --trace=borrow hello.ast

# Trace MIR optimizations
aster build --trace=mir hello.ast

# Multiple traces
aster build --trace=types --trace=borrow hello.ast
```

### Error Explanation

```bash
# Get detailed explanation for an error code
aster explain E3124
aster explain E6000
aster explain W0001
```

### Crash Reports

```bash
# View a crash report
aster crash-report aster_crash_20260214_123456.txt
```

## Diagnostic Codes

### Error Code Categories

- **E1xxx** - Syntax errors
- **E2xxx** - Name resolution
- **E3xxx** - Type system
- **E4xxx** - Traits
- **E5xxx** - Effects
- **E6xxx** - Ownership
- **E7xxx** - Borrow checking
- **E8xxx** - Pattern matching
- **E9xxx** - MIR/Backend

- **W0xxx-W1xxx** - Warnings
- **I0xxx** - Info messages

See [docs/diagnostics/README.md](diagnostics/README.md) for the complete list.

### Stable Error Codes

Error codes are **never reused**. Once assigned, a code remains stable across all compiler versions, ensuring:
- Consistent tooling integration
- Searchable documentation
- Reliable CI/CD filtering
- Long-term reproducibility

## Diagnostic Quality

### Every Diagnostic Includes

✅ **Stable code** - Never changes across versions  
✅ **Severity** - Error, warning, or info  
✅ **Title** - Short summary of the issue  
✅ **Message** - Detailed explanation  
✅ **Primary span** - Main source location  
✅ **Secondary spans** - Additional context (optional)  
✅ **Help text** - How to fix it (when known)  
✅ **Notes** - Additional information  
✅ **Category** - Compiler phase

### Example Diagnostic Output

```
test.ast:12:9
error[E3124]: cannot unify types
  --> test.ast:12:9
   |
12 | let x: i32 = "hello"
   |              ^^^^^^^ expected i32, found string
   |
help: consider parsing the string to an integer
```

### JSON Format

```json
{
  "code": "E3124",
  "severity": "error",
  "title": "cannot unify types",
  "message": "expected i32, found string",
  "category": "TypeSystem",
  "spans": {
    "primary": {
      "file": "test.ast",
      "line": 12,
      "column": 9,
      "start": 100,
      "length": 7
    },
    "secondary": []
  },
  "help": "consider parsing the string to an integer",
  "notes": []
}
```

## API Usage

### Diagnostic Builder

```csharp
using Aster.Compiler.Diagnostics;

// Create a diagnostic using the fluent API
var diag = DiagnosticBuilder.Error("E3124")
    .Title("Cannot unify types")
    .Message("Expected {0}, found {1}", expected, found)
    .Primary(span)
    .Secondary(otherSpan, "defined here")
    .Help("Consider converting the value")
    .Note("Type inference failed")
    .Category(DiagnosticCategory.TypeSystem)
    .Build();

// Report to a diagnostic bag
bag.Report(diag);

// Or build and report in one step
DiagnosticBuilder.Error("E3000")
    .Title("Type mismatch")
    .Primary(span)
    .Report(bag);
```

### Rendering

```csharp
using Aster.Compiler.Diagnostics.Rendering;

// Human-readable output
var humanRenderer = new HumanDiagnosticRenderer(
    useColor: true,
    sourceReader: new FileSourceReader());

Console.WriteLine(humanRenderer.Render(diagnostic));

// JSON output
var jsonRenderer = new JsonDiagnosticRenderer();
var json = jsonRenderer.RenderAll(diagnostics);
```

### Telemetry

```csharp
using Aster.Compiler.Telemetry;

var telemetry = new CompilationTelemetry();

// Time a phase automatically
var metrics = telemetry.GetOrCreatePhase("type-checking");
using (var timer = new PhaseTimer(metrics))
{
    // Do type checking...
    timer.IncrementNodes(nodeCount);
    timer.RecordCacheHit();
}

// Output results
Console.WriteLine(telemetry.FormatHuman());
Console.WriteLine(telemetry.FormatJson());
```

### Tracing

```csharp
using Aster.Compiler.Observability;

// Enable specific traces
CompilerTrace.Enable(TraceCategory.Types, TraceCategory.Borrow);

// Write trace messages
CompilerTrace.Write(TraceCategory.Types, "Unifying {0} with {1}", t1, t2);

// Use scopes for automatic indentation
using (CompilerTrace.Scope(TraceCategory.Types, "Type checking function"))
{
    CompilerTrace.Write(TraceCategory.Types, "Checking parameter types");
    // ...
}

// Specialized helpers
TypeTrace.UnificationStep("i32", "string", success: false);
BorrowTrace.RegionCreated("'a", "function body");
MirTrace.OptimizationApplied("DCE", "removed 5 dead stores");
```

### Crash Reporting

```csharp
using Aster.Compiler.Observability;

// Install global crash handler (in Program.Main)
CrashReporter.InstallGlobalHandler(
    version: "0.2.0",
    getCommand: () => string.Join(" ", args),
    getLastPhase: () => currentPhase,
    getRecentDiagnostics: () => recentDiags);

// Manual crash reporting
var reporter = new CrashReporter(version, command, phase, diagnostics);
var reportPath = reporter.WriteCrashReport(exception);
CrashReporter.DisplayCrashMessage(reportPath);
```

## Testing

### Running Tests

```bash
# All tests
dotnet test

# Diagnostic tests only
dotnet test tests/Aster.Diagnostics.Tests

# Observability tests only
dotnet test tests/Aster.Observability.Tests
```

### Test Coverage

- **Diagnostic Builder** - Fluent API, formatting, metadata
- **Registry** - Code lookup, uniqueness, categories
- **Renderers** - Human and JSON output formats
- **Telemetry** - Timing, metrics, formatters
- **Tracing** - Enable/disable, scopes, output
- **Crash Reporting** - Report generation, file creation

## Quality Gates

### CI Checks

1. **No duplicate codes** - Every code is unique
2. **All codes registered** - Registry is complete
3. **Golden tests** - Output format stability
4. **Explanation coverage** - E* codes documented

### Code Standards

- Zero panics for user errors
- Deterministic diagnostic ordering
- Thread-safe diagnostic collection
- Minimal overhead when disabled

## LSP Integration

The diagnostic system integrates with the Language Server Protocol for IDE support:

- Live diagnostic updates
- Code action suggestions
- Hover explanations
- Quick fixes

See [Aster.Lsp](../src/Aster.Lsp) for implementation details.

## Future Enhancements

- Interactive diagnostic viewer
- Diagnostic suppression annotations
- Custom diagnostic plugins
- Machine learning for better error messages
- Cross-compilation diagnostic aggregation

## References

- [Rust Compiler Errors](https://doc.rust-lang.org/error_codes/)
- [Swift Diagnostics](https://www.swift.org/documentation/swift-book/documentation/the-swift-programming-language/)
- [LSP Diagnostic Specification](https://microsoft.github.io/language-server-protocol/specifications/specification-current/#diagnostic)
