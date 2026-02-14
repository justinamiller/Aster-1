# Bootstrap Golden Outputs

## Overview

This directory contains **expected outputs** (golden files) for differential testing during the bootstrap process.

## Purpose

Golden files serve as:
1. **Reference outputs** from the trusted seed compiler (aster0)
2. **Comparison targets** for later compiler stages
3. **Regression detection** - changes to output are immediately visible

## Directory Structure

```
goldens/
├── core0/          # Core-0 expected outputs
│   ├── compile-pass/
│   │   ├── tokens/     # Expected token streams
│   │   ├── ast/        # Expected AST dumps
│   │   └── symbols/    # Expected symbol tables
│   ├── compile-fail/
│   │   └── errors/     # Expected error messages
│   └── run-pass/
│       ├── stdout/     # Expected stdout
│       └── stderr/     # Expected stderr
├── core1/          # Core-1 expected outputs
│   ├── compile-pass/
│   │   ├── tokens/
│   │   ├── ast/
│   │   ├── symbols/
│   │   ├── types/      # Expected type inference results
│   │   └── effects/    # Expected effect annotations
│   ├── compile-fail/
│   │   └── errors/
│   └── run-pass/
│       ├── stdout/
│       └── stderr/
├── core2/          # Core-2 expected outputs
│   ├── compile-pass/
│   │   ├── tokens/
│   │   ├── ast/
│   │   ├── symbols/
│   │   ├── types/
│   │   ├── mir/        # Expected MIR dumps
│   │   └── llvm/       # Expected LLVM IR
│   ├── compile-fail/
│   │   └── errors/
│   └── run-pass/
│       ├── stdout/
│       └── stderr/
└── README.md       # This file
```

## Golden File Types

### Token Streams
**Format**: JSON

**Example**: `goldens/core0/compile-pass/tokens/simple_struct.json`

```json
[
  {"kind": "Keyword", "text": "struct", "span": {"start": 0, "end": 6}},
  {"kind": "Identifier", "text": "Point", "span": {"start": 7, "end": 12}},
  {"kind": "LBrace", "text": "{", "span": {"start": 13, "end": 14}},
  ...
]
```

### AST Dumps
**Format**: S-expression or JSON

**Example**: `goldens/core0/compile-pass/ast/simple_struct.sexp`

```lisp
(Module
  (Struct "Point"
    (Field "x" (Type "i32"))
    (Field "y" (Type "i32")))
  (Function "main" ()
    (Let "p" (StructInit "Point"
      ("x" (Literal 10))
      ("y" (Literal 20))))
    (Call "print" ((FieldAccess (Var "p") "x")))))
```

### Symbol Tables
**Format**: JSON

**Example**: `goldens/core0/compile-pass/symbols/simple_struct.json`

```json
{
  "symbols": [
    {"id": "sym_0001", "name": "Point", "kind": "Struct", "module": "main"},
    {"id": "sym_0002", "name": "main", "kind": "Function", "module": "main"},
    {"id": "sym_0003", "name": "p", "kind": "Variable", "scope": "main"}
  ]
}
```

### Type Inference Results
**Format**: JSON

**Example**: `goldens/core1/compile-pass/types/generic_function.json`

```json
{
  "functions": {
    "create_box": {
      "type_params": ["T"],
      "param_types": ["T"],
      "return_type": "Box<T>"
    }
  },
  "variables": {
    "value": "i32",
    "boxed": "Box<i32>"
  }
}
```

### Error Messages
**Format**: Plain text with ANSI codes stripped

**Example**: `goldens/core0/compile-fail/errors/undefined_variable.txt`

```
error[E0001]: undefined variable `x`
  --> /source/fixtures/core0/compile-fail/undefined_variable.ast:2:11
   |
2  |     print(x);
   |           ^ not found in this scope
```

### Runtime Output
**Format**: Plain text

**Example**: `goldens/core0/run-pass/stdout/hello.txt`

```
hello world
```

## Creating Golden Files

### Initial Creation

1. **Run seed compiler** (aster0) on a fixture:
   ```bash
   aster0 build fixtures/core0/compile-pass/simple_struct.ast \
          --emit-tokens tokens.json \
          --emit-ast ast.sexp \
          --emit-symbols symbols.json
   ```

2. **Review output** for correctness

3. **Copy to goldens**:
   ```bash
   cp tokens.json goldens/core0/compile-pass/tokens/simple_struct.json
   cp ast.sexp goldens/core0/compile-pass/ast/simple_struct.sexp
   cp symbols.json goldens/core0/compile-pass/symbols/simple_struct.json
   ```

4. **Commit to repository**

### Updating Golden Files

When the seed compiler changes (rare):

1. **Regenerate all goldens**:
   ```bash
   ./bootstrap/scripts/regenerate-goldens.sh
   ```

2. **Review diff** carefully:
   ```bash
   git diff goldens/
   ```

3. **Verify changes are intentional**

4. **Update and commit**

## Comparison Rules

### Exact Match
- Token streams (exact JSON match)
- AST structure (exact S-expression match)
- Symbol IDs (exact hash match)

### Semantic Equivalence
- LLVM IR (ignore timestamps, file paths)
- MIR (stable ordering, ignore debug info)
- Diagnostics (ignore ANSI codes, allow minor wording changes)

### Tolerance
- Runtime output (allow whitespace differences)
- Floating point values (allow epsilon differences)

## Golden File Requirements

Each golden file should:
- ✅ Be **deterministic** - same input always produces this output
- ✅ Be **canonical** - use stable formatting (sorted, no random IDs)
- ✅ Be **minimal** - no unnecessary information
- ✅ Be **readable** - formatted for human review
- ✅ Be **versioned** - committed to Git

## File Naming

Golden files should match fixture names:

```
fixtures/core0/compile-pass/simple_struct.ast
  → goldens/core0/compile-pass/tokens/simple_struct.json
  → goldens/core0/compile-pass/ast/simple_struct.sexp
  → goldens/core0/compile-pass/symbols/simple_struct.json
```

## Regeneration

Golden files should be regenerated only when:
- ✅ Seed compiler has a **verified fix** (bug correction)
- ✅ Output format **intentionally changes** (e.g., AST node addition)
- ✅ New **language feature** is added

**Never** regenerate to make tests pass without understanding why they failed!

## Verification Process

```bash
# 1. Generate output from aster1
aster1 build fixtures/core0/compile-pass/simple_struct.ast --emit-tokens output.json

# 2. Compare with golden
diff output.json goldens/core0/compile-pass/tokens/simple_struct.json

# 3. If identical: PASS
# 4. If different: FAIL (investigate why)
```

## CI Integration

In CI, golden file tests run:

```yaml
- name: Differential Tests
  run: |
    ./bootstrap/scripts/verify.sh --all-stages
```

Any mismatch fails the build.

## Future Work

- [ ] Generate goldens for Core-0 (Stage 1)
- [ ] Generate goldens for Core-1 (Stage 2)
- [ ] Generate goldens for Core-2 (Stage 3)
- [ ] Add golden regeneration script
- [ ] Add golden comparison utilities

## See Also

- `/bootstrap/fixtures/README.md` - Test fixtures
- `/bootstrap/spec/bootstrap-stages.md` - Testing requirements
- `/bootstrap/scripts/verify.sh` - Verification script
