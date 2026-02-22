# Diagnostics Engine

**Status**: Phase 7 ✅  
**Updated**: 2026-02-22

---

## Overview

The Aster compiler produces rich, structured diagnostics similar to those of the Rust compiler.
Every diagnostic has a stable code (e.g. `E3000`), a severity level, a primary source span,
optional secondary spans, help text, notes, and (as of Phase 7) machine-applicable **suggestions**.

---

## Diagnostic Structure

```
error[E3000]: type mismatch
 --> src/main.ast:12:5
12 | let x: i32 = "hello";
   |               ^^^^^^^ expected `i32`, found `str`
   |
help: remove the string literal or change the type annotation
suggestion (machine-applicable): change the type to `str`
  --> replace `i32` at 12:8 with: str
note: type annotations are checked at the variable binding site
```

### Fields

| Field           | Type                            | Description                                       |
|-----------------|---------------------------------|---------------------------------------------------|
| `Code`          | `string`                        | Stable diagnostic code (`E3000`, `W0002`, ...)    |
| `Severity`      | `DiagnosticSeverity`            | Error / Warning / Info / Hint                     |
| `Title`         | `string`                        | Short one-line summary                            |
| `Message`       | `string`                        | Detailed message (may differ from title)          |
| `PrimarySpan`   | `Span`                          | File:line:col of the primary site                 |
| `SecondarySpans`| `IReadOnlyList<SecondarySpan>`  | Additional context spans with labels              |
| `Help`          | `string?`                       | Free-text help hint                               |
| `Notes`         | `IReadOnlyList<string>`         | Additional context notes                          |
| `Suggestions`   | `IReadOnlyList<DiagnosticSuggestion>` | Fix-it patches (Phase 7+)               |
| `Category`      | `DiagnosticCategory`            | Grouping enum (Syntax, Type, Borrow, ...)         |

---

## Fix-It Suggestions

A `DiagnosticSuggestion` describes a source patch:

```csharp
public sealed class DiagnosticSuggestion
{
    public Span   Span               { get; } // span to replace
    public string Replacement        { get; } // replacement text
    public string Message            { get; } // human description
    public bool   IsMachineApplicable{ get; } // safe to auto-apply
}
```

**Machine-applicable** suggestions can be applied by an IDE or `aster fix` without human review.
Non-machine-applicable suggestions require manual confirmation.

---

## Lint Levels

Every warning-category diagnostic is associated with a **lint name** that has a
configurable level via `#[allow(...)]`, `#[warn(...)]`, `#[deny(...)]`, and `#[forbid(...)]`
attributes.

### Levels

| Level    | Effect                                                           |
|----------|------------------------------------------------------------------|
| `Allow`  | Suppress the lint entirely (no output)                           |
| `Warn`   | Emit a warning (default for most lints)                          |
| `Deny`   | Treat as a hard error                                            |
| `Forbid` | Like Deny but cannot be downgraded by inner `allow`/`warn`/`deny`|

### Built-in Lints

| Lint Name                  | Default Level | Description                                      |
|----------------------------|---------------|--------------------------------------------------|
| `unused_variables`         | `Warn`        | Variable declared but never used                 |
| `unused_imports`           | `Warn`        | Import is never referenced                       |
| `dead_code`                | `Warn`        | Unreachable or unused code                       |
| `deprecated`               | `Warn`        | Item marked `#[deprecated]`                      |
| `non_snake_case`           | `Warn`        | Identifier not in snake_case                     |
| `non_camel_case_types`     | `Warn`        | Type not in CamelCase                            |
| `missing_docs`             | `Allow`       | Public item lacks doc comment                    |
| `overflow_literals`        | `Deny`        | Numeric literal overflows its declared type      |
| `unconditional_recursion`  | `Warn`        | Function always recurses without base case       |

### Example

```aster
#[allow(unused_variables)]
fn compute() -> i32 {
    let unused = 42;  // no warning
    0
}

#[deny(dead_code)]
fn helper() { }      // error if never called
```

---

## Structured JSON Output

When the `--error-format=json` CLI flag is used, the `JsonDiagnosticRenderer` produces
one JSON object per line:

```json
{
  "code": "E3000",
  "severity": "error",
  "title": "type mismatch",
  "message": "expected `i32`, found `str`",
  "file": "src/main.ast",
  "line": 12,
  "column": 5
}
```

---

## Rendering

The `HumanDiagnosticRenderer` supports:

- **ANSI color** (red for errors, yellow for warnings, cyan for notes, green for suggestions)
- **Source context** (line + caret marker)
- **Secondary span labels**
- **Help text**
- **Suggestions** (with machine-applicability marker)
- **Notes**

Disable colors with `useColor: false` for CI / log output.

---

## References

- `src/Aster.Compiler/Diagnostics/Diagnostic.cs`
- `src/Aster.Compiler/Diagnostics/DiagnosticSuggestion.cs`
- `src/Aster.Compiler/Diagnostics/LintRegistry.cs`
- `src/Aster.Compiler/Diagnostics/Rendering/HumanDiagnosticRenderer.cs`
- `src/Aster.Compiler/Diagnostics/Rendering/JsonDiagnosticRenderer.cs`
