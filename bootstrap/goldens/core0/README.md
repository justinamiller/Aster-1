# Golden Files for Core-0 Fixtures

## Overview

This directory will contain **expected outputs** from the aster0 (seed compiler) when compiling Core-0 test fixtures.

## Status

🚧 **Pending Generation** - Golden files will be created once aster0 can emit the required formats

## Directory Structure

```
goldens/core0/
├── compile-pass/
│   ├── tokens/          # Token stream JSON dumps
│   ├── ast/             # AST S-expression dumps
│   └── symbols/         # Symbol table JSON dumps
├── compile-fail/
│   └── errors/          # Expected error messages
└── run-pass/
    ├── stdout/          # Expected stdout output
    └── stderr/          # Expected stderr output
```

## Generating Golden Files

### Step 1: Implement Output Formats in aster0

The C# seed compiler (aster0) needs to support these output modes:

```bash
# Emit token stream
aster0 compile --emit-tokens fixture.ast > tokens.json

# Emit AST dump
aster0 compile --emit-ast fixture.ast > ast.sexp

# Emit symbol table
aster0 compile --emit-symbols fixture.ast > symbols.json
```

### Step 2: Generate Golden Files

Run this script to generate all golden files:

```bash
# For each compile-pass fixture
for f in bootstrap/fixtures/core0/compile-pass/*.ast; do
    name=$(basename $f .ast)
    aster0 compile --emit-tokens $f > bootstrap/goldens/core0/compile-pass/tokens/$name.json
    aster0 compile --emit-ast $f > bootstrap/goldens/core0/compile-pass/ast/$name.sexp
    aster0 compile --emit-symbols $f > bootstrap/goldens/core0/compile-pass/symbols/$name.json
done

# For each compile-fail fixture
for f in bootstrap/fixtures/core0/compile-fail/*.ast; do
    name=$(basename $f .ast)
    aster0 compile $f 2> bootstrap/goldens/core0/compile-fail/errors/$name.txt || true
done

# For each run-pass fixture
for f in bootstrap/fixtures/core0/run-pass/*.ast; do
    name=$(basename $f .ast)
    aster0 build $f -o /tmp/$name
    /tmp/$name > bootstrap/goldens/core0/run-pass/stdout/$name.txt 2> bootstrap/goldens/core0/run-pass/stderr/$name.txt
done
```

## Expected Output Formats

### Token Stream (JSON)

**File**: `compile-pass/tokens/simple_struct.json`

```json
[
  {"kind": "Struct", "value": "struct", "span": {"file": "simple_struct.ast", "line": 1, "column": 1, "start": 0, "length": 6}},
  {"kind": "Identifier", "value": "Point", "span": {"file": "simple_struct.ast", "line": 1, "column": 8, "start": 7, "length": 5}},
  {"kind": "LeftBrace", "value": "{", "span": {"file": "simple_struct.ast", "line": 1, "column": 14, "start": 13, "length": 1}},
  {"kind": "Identifier", "value": "x", "span": {"file": "simple_struct.ast", "line": 2, "column": 5, "start": 19, "length": 1}},
  {"kind": "Colon", "value": ":", "span": {"file": "simple_struct.ast", "line": 2, "column": 6, "start": 20, "length": 1}},
  ...
]
```

### AST Dump (S-Expression)

**File**: `compile-pass/ast/simple_struct.sexp`

```lisp
(Module
  (Struct "Point"
    (Field "x" (Type "i32"))
    (Field "y" (Type "i32")))
  (Function "main" ()
    (Body
      (Let "p" (StructInit "Point"
        (Field "x" (Literal 10))
        (Field "y" (Literal 20))))
      (Let "sum" (BinaryOp Add
        (FieldAccess (Var "p") "x")
        (FieldAccess (Var "p") "y")))
      (Call "print" (Var "sum")))))
```

### Symbol Table (JSON)

**File**: `compile-pass/symbols/simple_struct.json`

```json
{
  "symbols": [
    {
      "id": "sym_00000001",
      "name": "Point",
      "kind": "Struct",
      "module": "simple_struct",
      "span": {"file": "simple_struct.ast", "line": 1, "column": 8, "start": 7, "length": 5}
    },
    {
      "id": "sym_00000002",
      "name": "main",
      "kind": "Function",
      "module": "simple_struct",
      "span": {"file": "simple_struct.ast", "line": 7, "column": 4, "start": 65, "length": 4}
    },
    {
      "id": "sym_00000003",
      "name": "p",
      "kind": "Variable",
      "scope": "main",
      "span": {"file": "simple_struct.ast", "line": 8, "column": 9, "start": 83, "length": 1}
    }
  ]
}
```

### Error Messages (Plain Text)

**File**: `compile-fail/errors/undefined_variable.txt`

```
error[E0001]: undefined variable `x`
  --> bootstrap/fixtures/core0/compile-fail/undefined_variable.ast:3:11
   |
3  |     print(x);
   |           ^ not found in this scope
```

### Runtime Output (Plain Text)

**File**: `run-pass/stdout/hello_world.txt`

```
Hello, World!
```

## Using Golden Files for Verification

### Differential Testing

Compare aster0 and aster1 outputs:

```bash
# Generate output from aster1
aster1 compile --emit-tokens simple_struct.ast > /tmp/aster1_tokens.json

# Compare with golden (aster0 output)
diff bootstrap/goldens/core0/compile-pass/tokens/simple_struct.json /tmp/aster1_tokens.json

# If identical: PASS
# If different: FAIL (investigate discrepancy)
```

### Verification Script

The `verify.sh` script automates this:

```bash
./bootstrap/scripts/verify.sh --stage 1

# Runs:
# 1. For each fixture, compile with aster0 and aster1
# 2. Compare token streams
# 3. Compare AST dumps
# 4. Compare symbol tables
# 5. Compare runtime outputs
# 6. Report pass/fail
```

## Equivalence Criteria

### Exact Match Required
- Token streams (kind, value, span must match exactly)
- Symbol IDs (hash-based, should be deterministic)
- Error codes (E0001, E0303, etc.)

### Semantic Equivalence Allowed
- AST node IDs (internal IDs may differ if semantically equivalent)
- Whitespace in error messages
- Diagnostic wording (as long as error code matches)

### Floating Point Tolerance
- Float literal comparisons allow epsilon differences (1e-10)

## Regenerating Golden Files

Golden files should be regenerated only when:
- ✅ Bug fix in aster0 (seed compiler) is verified
- ✅ Language feature addition approved
- ✅ Output format intentionally changes

**Never** regenerate to make tests pass without understanding why they failed!

## CI Integration

In CI, golden file tests run automatically:

```yaml
- name: Generate Golden Files
  run: |
    ./bootstrap/scripts/generate-goldens.sh
  if: github.event_name == 'push' && github.ref == 'refs/heads/main'

- name: Differential Tests
  run: |
    ./bootstrap/scripts/verify.sh --stage 1
```

Any mismatch fails the CI build.

## TODO

- [ ] Implement `--emit-tokens` flag in aster0 (C# compiler)
- [ ] Implement `--emit-ast` flag in aster0
- [ ] Implement `--emit-symbols` flag in aster0
- [ ] Create `generate-goldens.sh` script
- [ ] Run generation and commit golden files
- [ ] Integrate with verify.sh script

## References

- Fixtures: `/bootstrap/fixtures/core0/`
- Verify Script: `/bootstrap/scripts/verify.sh`
- Bootstrap Spec: `/bootstrap/spec/bootstrap-stages.md`
- Reproducible Builds: `/bootstrap/spec/reproducible-builds.md`

---

**Status**: Directory structure ready, awaiting aster0 output implementation  
**Next**: Implement --emit-* flags in C# compiler
