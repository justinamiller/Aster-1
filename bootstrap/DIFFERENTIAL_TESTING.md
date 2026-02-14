# Differential Testing Infrastructure

## Overview

The differential testing infrastructure validates Stage 1 bootstrap by comparing outputs from **aster0** (seed C# compiler) and **aster1** (Aster Core-0 compiler) to ensure they produce identical results.

## Architecture

```
┌─────────────┐
│  Fixtures   │ Test programs in Core-0 subset
└──────┬──────┘
       │
       ├─────► aster0 (C#) ────► Golden files (.json)
       │
       └─────► aster1 (Aster) ──► Test output (.json)
                                    │
                                    ▼
                              Diff comparator
                                    │
                                    ▼
                              ✓ Pass / ✗ Fail
```

## Components

### 1. Emission Flags (C# Compiler)

Added to `/src/Aster.CLI/Program.cs`:

**`emit-tokens` command** - Outputs token stream as JSON:
```bash
aster emit-tokens file.ast > tokens.json
```

**Output format**:
```json
[
  {
    "kind": "Fn",
    "value": "fn",
    "span": {
      "file": "test.ast",
      "line": 1,
      "column": 1,
      "start": 0,
      "length": 2
    }
  },
  ...
]
```

### 2. Golden File Generator

**Scripts**:
- `bootstrap/scripts/generate-goldens.sh` (Unix/Linux/macOS)
- `bootstrap/scripts/generate-goldens.ps1` (Windows)

**Usage**:
```bash
cd bootstrap/scripts
./generate-goldens.sh
```

**What it does**:
1. Reads all fixtures from `bootstrap/fixtures/core0/`
2. Runs aster0 with `emit-tokens` on each fixture
3. Saves output to `bootstrap/goldens/core0/{category}/tokens/*.json`
4. Creates golden files for:
   - `compile-pass/` fixtures
   - `compile-fail/` fixtures (marks as expected errors)
   - `run-pass/` fixtures

**Output structure**:
```
bootstrap/goldens/core0/
├── compile-pass/
│   └── tokens/
│       ├── simple_struct.json
│       ├── basic_enum.json
│       └── ...
├── compile-fail/
│   └── tokens/
│       └── ... (error markers)
└── run-pass/
    └── tokens/
        ├── hello_world.json
        └── ...
```

### 3. Differential Test Harness

**Scripts**:
- `bootstrap/scripts/diff-test-tokens.sh` (Unix/Linux/macOS)
- `bootstrap/scripts/diff-test-tokens.ps1` (Windows)

**Usage**:
```bash
cd bootstrap/scripts
./diff-test-tokens.sh

# With verbose output
VERBOSE=true ./diff-test-tokens.sh
```

**What it does**:
1. Checks if aster0 exists
2. Checks if aster1 exists (if not, verifies golden files only)
3. For each fixture:
   - Runs aster1 with `emit-tokens`
   - Compares output to golden file
   - Reports match/mismatch
4. Generates summary report

**Output**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Differential Testing - Aster Stage 1 Bootstrap
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Testing compile-pass fixtures...
  ✓ simple_struct
  ✓ basic_enum
  ✓ simple_function
  ✓ control_flow
  ✓ vec_operations
  Result: 5/5 passed

Testing run-pass fixtures...
  ✓ hello_world
  ✓ fibonacci
  ✓ sum_array
  Result: 3/3 passed

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
All differential tests passed!
aster0 and aster1 produce identical token streams.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 4. Integration with Verify Scripts

The differential testing is integrated into:
- `bootstrap/scripts/verify.sh`
- `bootstrap/scripts/verify.ps1`

**Usage**:
```bash
# Verify all stages
./bootstrap/scripts/verify.sh --all-stages

# Verify Stage 1 only
./bootstrap/scripts/verify.sh --stage 1
```

## Workflow

### Step 1: Build Seed Compiler (aster0)

```bash
cd bootstrap/scripts
./bootstrap.sh
```

This builds the C# compiler (aster0) to `build/bootstrap/stage0/`.

### Step 2: Generate Golden Files

```bash
cd bootstrap/scripts
./generate-goldens.sh
```

This creates reference outputs in `bootstrap/goldens/core0/`.

### Step 3: Build aster1 (Stage 1 Compiler)

*This step is pending - aster1 not yet built*

```bash
# Future: Build aster1 from Aster source
# aster0 compile aster/compiler/frontend/*.ast -o aster1
```

### Step 4: Run Differential Tests

```bash
cd bootstrap/scripts
./diff-test-tokens.sh
```

If aster1 doesn't exist yet, this script verifies golden files only.
Once aster1 is built, it performs full differential comparison.

### Step 5: Verify Bootstrap

```bash
cd bootstrap/scripts
./verify.sh --stage 1
```

Runs all Stage 1 verification tests.

## Comparison Criteria

### Token Stream Equivalence

Two token streams are **equivalent** if they match exactly in:

1. **Token kinds** - Same TokenKind enum values
2. **Token values** - Same lexeme strings (identifiers, literals, etc.)
3. **Spans** - Same source locations:
   - File name
   - Line number (1-indexed)
   - Column number (1-indexed)
   - Start offset (0-indexed byte position)
   - Length (in bytes)

### Allowed Differences

None. Token streams must match **exactly** (bit-for-bit).

### Reasons for Mismatch

If tests fail, possible causes:

1. **Lexer bug in aster1** - Logic differs from aster0
2. **Core-0 workaround issue** - HashMap→Vec conversion has bugs
3. **Position tracking bug** - Line/column calculation differs
4. **String interning bug** - Different string handling
5. **Escape sequence bug** - Different escape processing

## Debugging Failed Tests

### 1. Check Golden Files

```bash
cat bootstrap/goldens/core0/compile-pass/tokens/simple_struct.json
```

Verify the golden file looks correct.

### 2. Run aster1 Manually

```bash
# Once aster1 is built:
aster1 emit-tokens bootstrap/fixtures/core0/compile-pass/simple_struct.ast
```

Compare output to golden file manually.

### 3. Enable Verbose Mode

```bash
VERBOSE=true ./diff-test-tokens.sh
```

Shows detailed diffs for mismatches.

### 4. Compare Specific Fixture

```bash
# Generate test output
aster1 emit-tokens fixtures/core0/compile-pass/simple_struct.ast > test.json

# Compare to golden
diff -u goldens/core0/compile-pass/tokens/simple_struct.json test.json
```

### 5. Check Token Counts

```bash
# Count tokens in golden
jq '. | length' goldens/core0/compile-pass/tokens/simple_struct.json

# Count tokens in test
jq '. | length' test.json
```

## Future Enhancements

### Phase 2: AST Comparison

Add `emit-ast` command and compare AST dumps:

```bash
aster emit-ast file.ast > ast.json
```

**Scripts**:
- `diff-test-ast.sh`
- `diff-test-ast.ps1`

### Phase 3: Symbol Table Comparison

Add `emit-symbols` command and compare symbol tables:

```bash
aster emit-symbols file.ast > symbols.json
```

**Scripts**:
- `diff-test-symbols.sh`
- `diff-test-symbols.ps1`

### Phase 4: Runtime Output Comparison

For `run-pass` fixtures, compare runtime outputs:

```bash
# Compile and run with aster0
aster0 build fixture.ast -o prog0
./prog0 > output0.txt

# Compile and run with aster1
aster1 build fixture.ast -o prog1
./prog1 > output1.txt

# Compare
diff output0.txt output1.txt
```

## Testing in CI

### GitHub Actions Integration

Add to `.github/workflows/bootstrap.yml`:

```yaml
- name: Generate golden files
  run: ./bootstrap/scripts/generate-goldens.sh

- name: Run differential tests
  run: ./bootstrap/scripts/diff-test-tokens.sh

- name: Upload golden files
  uses: actions/upload-artifact@v3
  with:
    name: golden-files
    path: bootstrap/goldens/
```

### CI Success Criteria

All differential tests must pass:
- 5/5 compile-pass fixtures ✓
- 3/3 run-pass fixtures ✓
- 0 failures

## Maintenance

### Updating Golden Files

If the C# compiler changes (new features, bug fixes):

1. Review changes in aster0
2. Regenerate golden files:
   ```bash
   ./generate-goldens.sh
   ```
3. Review diffs:
   ```bash
   git diff bootstrap/goldens/
   ```
4. Commit updated goldens if intentional

### Adding New Fixtures

1. Create fixture in `bootstrap/fixtures/core0/{category}/`
2. Generate golden:
   ```bash
   ./generate-goldens.sh
   ```
3. Verify golden is correct:
   ```bash
   cat bootstrap/goldens/core0/{category}/tokens/{fixture}.json
   ```
4. Commit both fixture and golden

### Fixture Categories

- **compile-pass** - Valid Core-0 programs that should compile
- **compile-fail** - Invalid programs that should produce errors
- **run-pass** - Valid programs with expected runtime output

## References

- **Bootstrap Stages**: `/bootstrap/spec/bootstrap-stages.md`
- **Core-0 Subset**: `/bootstrap/spec/aster-core-subsets.md`
- **Fixtures**: `/bootstrap/fixtures/core0/README.md`
- **Golden Files**: `/bootstrap/goldens/core0/README.md`
- **Lexer Implementation**: `/aster/compiler/frontend/README.md`

---

**Status**: Infrastructure complete, ready for aster1 testing  
**Next**: Build aster1 and run full differential validation
