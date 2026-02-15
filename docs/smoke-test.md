# One-Command Smoke Test

## Quick Start

Test the compiler in one command:

```bash
./scripts/smoke_test.sh
```

This verifies:
1. ✅ Compiler builds successfully
2. ✅ Can compile simple programs
3. ✅ Generates valid LLVM IR
4. ✅ Output structure is correct

## Expected Output

```
======================================
  Aster Compiler Smoke Test
======================================

[1/4] Building compiler...
✓ Compiler built
[2/4] Compiling hello world...
✓ Compilation successful
[3/4] Verifying output...
✓ LLVM IR generated (25 lines)
[4/4] Validating LLVM IR...
✓ Valid LLVM IR structure

======================================
✓ Smoke test PASSED
======================================

The Aster compiler is working correctly!
Output: /tmp/hello.ll

To compile your own programs:
  aster build my_program.ast
```

## Running Compiled Programs (Future)

Once LLVM backend is fully integrated:

```bash
# Compile to LLVM IR
aster build hello.ast -o hello.ll

# Compile to executable with LLVM
llc hello.ll -o hello.s
clang hello.s -o hello

# Run
./hello
```

Or use the `run` command (when implemented):

```bash
aster run hello.ast
```

## What the Smoke Test Does

### 1. Build Compiler
Runs `dotnet build` to ensure the C# compiler builds.

### 2. Compile Example
Compiles `examples/simple_hello.ast` to LLVM IR.

### 3. Verify Output
Checks that:
- Output file exists
- Has reasonable size (> 0 lines)

### 4. Validate Structure
Verifies LLVM IR contains:
- Function definition (`define`)
- Main function (`@main`)

## Troubleshooting

### Build Fails

**Error**: "Failed to build compiler"

**Solution**:
```bash
# Restore dependencies
dotnet restore

# Build manually to see errors
dotnet build
```

### Compilation Fails

**Error**: "Compilation failed"

**Solution**:
```bash
# Run manually to see error messages
dotnet run --project src/Aster.CLI -- build examples/simple_hello.ast
```

### Invalid Output

**Error**: "Invalid LLVM IR structure"

**Solution**:
```bash
# Check output directly
cat /tmp/hello.ll

# Should contain:
# define i32 @main() {
#   ...
# }
```

## CI Integration

Add to GitHub Actions:

```yaml
- name: Smoke Test
  run: ./scripts/smoke_test.sh
```

## Alternative: Quick Manual Test

```bash
# Build
dotnet build

# Compile an example
dotnet run --project src/Aster.CLI -- build examples/simple_hello.ast

# Check output
ls -lh simple_hello.ll
cat simple_hello.ll
```

## Success Criteria

Smoke test passes if:
1. Compiler builds without errors
2. Example compiles without errors
3. LLVM IR is generated
4. IR contains expected structure

## What Smoke Test Does NOT Check

- ❌ Full test suite (use `dotnet test`)
- ❌ Execution of compiled code (would need LLVM backend)
- ❌ Complex programs (just simple hello world)
- ❌ All language features (just basic compilation)

For comprehensive testing, use:
```bash
dotnet test  # Run all 119 tests
```

## See Also

- [README.md](../README.md) - Full project documentation
- [CI Workflow](../.github/workflows/ci.yml) - Automated testing
- [Examples](../examples/) - More example programs

---

**Last Updated**: 2026-02-15  
**Status**: Smoke test available for Stage0 compiler
