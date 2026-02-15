# Next Steps Guide - Complete Bootstrap Roadmap

**Date**: 2026-02-15  
**Current Status**: Stage 1 Parser 100% Complete, Ready for Bootstrap  
**Purpose**: Step-by-step guide to complete the Aster compiler bootstrap

---

## Table of Contents

1. [Overview](#overview)
2. [Immediate Steps: Bootstrap Compilation](#immediate-steps-bootstrap-compilation)
3. [Post-Bootstrap Implementation](#post-bootstrap-implementation)
4. [Long-term Development Path](#long-term-development-path)
5. [Troubleshooting](#troubleshooting)
6. [Verification Checklist](#verification-checklist)

---

## Overview

This guide provides detailed, step-by-step instructions for completing the next phases of the Aster compiler bootstrap process. The work is divided into three major phases:

1. **Immediate**: Bootstrap compilation (compile aster1 with aster0)
2. **Post-Bootstrap**: Complete aster1 implementation
3. **Long-term**: Full self-hosting and Stage 2+

**Prerequisites**:
- ✅ Stage 1 parser is 100% complete
- ✅ CLI interface implemented
- ✅ Driver integration complete
- ✅ All documentation up to date

**Current State**:
- Parser can handle all Core-0 constructs
- Placeholder implementations for I/O and utilities
- Ready to be compiled by C# compiler (aster0)

---

## Immediate Steps: Bootstrap Compilation

### Goal
Compile the Stage 1 Aster compiler (written in Aster) using the Stage 0 C# compiler to produce a working aster1 binary.

### Step 1: Verify Stage 0 Compiler (aster0) Works

**1.1 Build the C# Compiler**
```bash
cd /home/runner/work/Aster-1/Aster-1

# Build the Stage 0 compiler
dotnet build src/Aster.sln

# Verify build succeeded
ls -la build/bootstrap/stage0/
```

**Expected Output**:
- `Aster.CLI.dll` and related assemblies in `build/bootstrap/stage0/`

**Verification**:
```bash
# Test with a simple program
dotnet run --project src/Aster.CLI emit-ast-json examples/hello.ast
```

**Expected**: JSON output representing the AST

**Troubleshooting**:
- If build fails: Check that .NET 8.0+ SDK is installed
- If missing dependencies: Run `dotnet restore`
- See [TOOLCHAIN.md](../TOOLCHAIN.md) for platform-specific issues

---

### Step 2: Prepare Stage 1 Source Files

**2.1 Verify Stage 1 Source Exists**
```bash
# Check that all Stage 1 source files are present
ls -la src/aster1/

# Should see:
# - main.ast
# - driver.ast
# - parser.ast
# - lexer.ast
# - ast.ast
# - symbols.ast
# - typecheck.ast
# - ir.ast
# - codegen.ast
# - string_interner.ast
```

**2.2 Review Source Files for Completeness**

Check each file has proper structure:
```bash
# Count lines in each file
wc -l src/aster1/*.ast
```

**Expected**:
- `parser.ast`: ~900 lines
- `main.ast`: ~300 lines
- `driver.ast`: ~350 lines
- Other files: varies

**2.3 Create Stage 1 Build Directory**
```bash
mkdir -p build/bootstrap/stage1
```

---

### Step 3: Attempt Bootstrap Compilation

**3.1 Compile main.ast with aster0**

This is the critical step where we attempt to compile Aster code with the C# compiler.

```bash
# Navigate to repo root
cd /home/runner/work/Aster-1/Aster-1

# Attempt to compile main.ast
dotnet run --project src/Aster.CLI build src/aster1/main.ast -o build/bootstrap/stage1/main.ll

# Check if LLVM IR was generated
ls -la build/bootstrap/stage1/main.ll
```

**Expected Result**:
- LLVM IR file created at `build/bootstrap/stage1/main.ll`
- No compilation errors

**Likely Issues**:

**Issue 1: Syntax Not Supported**
- **Symptom**: Parser errors about unsupported syntax
- **Solution**: The C# compiler may not fully support all Core-0 syntax yet
- **Action**: Review error messages and either:
  - Simplify Stage 1 source to use supported subset
  - Extend C# compiler to support missing features

**Issue 2: Missing Type Definitions**
- **Symptom**: Errors about undefined types (Token, Parser, etc.)
- **Solution**: Create type definition files
- **Action**: Add missing type definitions to C# compiler

**Issue 3: Module System Issues**
- **Symptom**: Cannot find imported modules
- **Solution**: Aster module system may not be implemented yet
- **Action**: Either:
  - Compile all files into single translation unit
  - Implement basic module resolution

**3.2 Compile All Stage 1 Files**

If main.ast compiles, compile all other files:

```bash
# Compile each Stage 1 source file
for file in src/aster1/*.ast; do
    basename=$(basename "$file" .ast)
    echo "Compiling $file..."
    dotnet run --project src/Aster.CLI build "$file" -o "build/bootstrap/stage1/${basename}.ll"
done

# Verify all .ll files created
ls -la build/bootstrap/stage1/*.ll
```

**Expected**: One .ll file per .ast file

---

### Step 4: Link Stage 1 Object Files

**4.1 Compile LLVM IR to Object Files**
```bash
cd build/bootstrap/stage1

# Compile each .ll to .o
for file in *.ll; do
    basename=$(basename "$file" .ll)
    echo "Compiling $basename.ll to object file..."
    clang -c "$file" -o "${basename}.o"
done

# Verify object files
ls -la *.o
```

**4.2 Compile Runtime Library**
```bash
# Compile the C runtime library
cd /home/runner/work/Aster-1/Aster-1
clang -c runtime/aster_runtime.c -o build/bootstrap/stage1/aster_runtime.o
```

**4.3 Link Everything Together**
```bash
cd build/bootstrap/stage1

# Link all object files into aster1 binary
clang -o aster1 *.o -lm

# Verify binary was created
ls -la aster1
file aster1
```

**Expected**: 
- Executable file `aster1`
- File type shows it's an executable

---

### Step 5: Test Stage 1 Binary

**5.1 Run Help Command**
```bash
./build/bootstrap/stage1/aster1 help
```

**Expected Output**:
```
Aster Stage 1 Compiler

Usage: aster1 <command> <input-file> [output-file]

Commands:
  emit-tokens         Emit token stream as JSON
  emit-ast-json       Emit AST as JSON
  emit-symbols-json   Emit symbol table/HIR as JSON
  build               Compile to LLVM IR
  help                Show this message
```

**5.2 Test with Simple Program**
```bash
# Create a test program
cat > /tmp/test.ast << 'EOF'
fn main() {
    let x = 42;
}
EOF

# Try to emit tokens
./build/bootstrap/stage1/aster1 emit-tokens /tmp/test.ast
```

**Expected**: JSON output with tokens

**5.3 Compare with aster0 Output**
```bash
# Get aster0 output
dotnet run --project src/Aster.CLI emit-tokens /tmp/test.ast > /tmp/aster0-tokens.json

# Get aster1 output  
./build/bootstrap/stage1/aster1 emit-tokens /tmp/test.ast > /tmp/aster1-tokens.json

# Compare
diff /tmp/aster0-tokens.json /tmp/aster1-tokens.json
```

**Expected**: No differences (or minimal formatting differences)

---

### Step 6: Run Differential Tests

**6.1 Generate Golden Files with aster0**
```bash
cd /home/runner/work/Aster-1/Aster-1
./bootstrap/scripts/generate-goldens.sh
```

**Expected**: Golden files generated in `bootstrap/goldens/core0/`

**6.2 Run Token Differential Tests**
```bash
# This compares aster0 vs aster1 token output
./bootstrap/scripts/diff-test-tokens.sh
```

**Expected**: All tests pass with "✓ PASS" messages

**6.3 Run AST Differential Tests**
```bash
./bootstrap/scripts/diff-test-ast.sh
```

**Expected**: All tests pass

**6.4 Run Symbols Differential Tests**
```bash
./bootstrap/scripts/diff-test-symbols.sh
```

**Expected**: All tests pass

---

### Step 7: Document Bootstrap Success

**7.1 Update STATUS.md**

Mark bootstrap as complete:
```markdown
### Stage 1: Stage1 Compiler (aster1) - ✅ Bootstrap Complete

**Status**: ✅ Bootstrap successful, aster1 binary created

**Milestone**: First self-compiled Aster code!
```

**7.2 Create Bootstrap Report**
```bash
# Create a bootstrap success report
cat > docs/BOOTSTRAP_COMPLETE.md << 'EOF'
# Bootstrap Compilation Complete

**Date**: [Current Date]
**Compiler**: aster1
**Built By**: aster0 (C# compiler)

## Build Details
- Source files: [number] .ast files
- LLVM IR files: [number] .ll files
- Binary size: [size]
- Build time: [duration]

## Test Results
- Differential tests: PASS
- Token comparison: PASS
- AST comparison: PASS
- Symbols comparison: PASS

## Next Steps
- Implement file I/O helpers
- Complete JSON serialization
- Test with all Core-0 fixtures
EOF
```

---

## Post-Bootstrap Implementation

Now that aster1 can be compiled, implement the placeholder functions.

### Step 8: Implement File I/O Helpers

**Goal**: Replace placeholder I/O functions with working implementations.

**8.1 Understand the Interface**

Review what's needed in `src/aster1/main.ast` and `src/aster1/driver.ast`:
- `read_file(path: String) -> String`
- `write_file(path: String, content: String)`
- `println(s: String)`

**8.2 Choose Implementation Strategy**

**Option A: FFI (Foreign Function Interface)**
- Call C standard library functions
- Requires FFI support in Aster

**Option B: Intrinsics**
- Built-in functions provided by compiler
- Easier but requires runtime support

**Option C: Extend Runtime Library**
- Add I/O functions to `runtime/aster_runtime.c`
- Expose to Aster via external declarations

**8.3 Implement with Runtime Library (Recommended)**

Add to `runtime/aster_runtime.c`:

```c
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

// Read file contents into a string
char* aster_read_file(const char* path) {
    FILE* f = fopen(path, "r");
    if (!f) return NULL;
    
    fseek(f, 0, SEEK_END);
    long size = ftell(f);
    fseek(f, 0, SEEK_SET);
    
    char* buffer = malloc(size + 1);
    fread(buffer, 1, size, f);
    buffer[size] = '\0';
    
    fclose(f);
    return buffer;
}

// Write string to file
int aster_write_file(const char* path, const char* content) {
    FILE* f = fopen(path, "w");
    if (!f) return 0;
    
    fputs(content, f);
    fclose(f);
    return 1;
}

// Print to stdout
void aster_println(const char* s) {
    puts(s);
}
```

**8.4 Update Aster Source**

In `src/aster1/main.ast`, change from placeholders to external declarations:

```rust
// External declarations (implemented in runtime)
extern fn aster_read_file(path: String) -> String;
extern fn aster_write_file(path: String, content: String) -> bool;
extern fn aster_println(s: String);

// Wrapper functions
fn read_file(path: String) -> String {
    aster_read_file(path)
}

fn write_file(path: String, contents: String) {
    aster_write_file(path, contents);
}

fn println(s: String) {
    aster_println(s);
}
```

**8.5 Rebuild and Test**
```bash
# Recompile runtime
clang -c runtime/aster_runtime.c -o build/aster_runtime.o

# Rebuild aster1 (if source changed, rebuild from aster0)
# ... bootstrap compilation steps ...

# Test file I/O
./build/bootstrap/stage1/aster1 emit-ast-json examples/hello.ast > /tmp/output.json
cat /tmp/output.json
```

---

### Step 9: Complete JSON Serialization

**Goal**: Implement full recursive JSON serialization for AST and symbols.

**9.1 Review Current State**

Check `src/aster1/driver.ast` for placeholder serialization functions:
- `serialize_ast_json(program: ProgramNode) -> String`
- `serialize_symbols_json(symbols: SymbolTable) -> String`

**9.2 Implement AST Serialization**

**Strategy**: Recursive traversal with proper JSON formatting.

```rust
fn serialize_ast_json(program: ProgramNode) -> String {
    let mut json = "{";
    json = json + "\"type\":\"Program\",";
    json = json + "\"declarations\":[";
    
    let mut i = 0;
    while i < program.declarations.len() {
        if i > 0 {
            json = json + ",";
        }
        json = json + serialize_item_json(program.declarations[i]);
        i = i + 1;
    }
    
    json = json + "]";
    json = json + "}";
    json
}

fn serialize_item_json(item: Item) -> String {
    match item {
        Item::Function(func) => serialize_function_json(func),
        Item::Struct(struct_item) => serialize_struct_json(struct_item),
        Item::Enum(enum_item) => serialize_enum_json(enum_item)
    }
}

fn serialize_function_json(func: FunctionItem) -> String {
    let mut json = "{";
    json = json + "\"type\":\"Function\",";
    json = json + "\"name\":\"" + json_escape(func.name) + "\",";
    
    // Serialize parameters
    json = json + "\"params\":[";
    let mut i = 0;
    while i < func.params.len() {
        if i > 0 { json = json + ","; }
        json = json + serialize_param_json(func.params[i]);
        i = i + 1;
    }
    json = json + "],";
    
    // Serialize return type
    json = json + "\"returnType\":" + serialize_type_json(func.return_type) + ",";
    
    // Serialize body
    json = json + "\"body\":" + serialize_expr_json(func.body);
    
    json = json + "}";
    json
}

// Continue for all node types...
```

**9.3 Implement Helper Functions**

```rust
fn json_escape(s: String) -> String {
    // Escape special characters for JSON
    // Replace: " with \", \ with \\, newlines, etc.
    let mut result = "";
    let mut i = 0;
    while i < s.len() {
        let c = s.char_at(i);  // hypothetical string API
        if c == '"' {
            result = result + "\\\"";
        } else if c == '\\' {
            result = result + "\\\\";
        } else if c == '\n' {
            result = result + "\\n";
        } else {
            result = result + c;
        }
        i = i + 1;
    }
    result
}

fn i32_to_string(n: i32) -> String {
    // Convert integer to string
    // This is complex in Aster without stdlib
    // May need runtime support
    extern fn aster_i32_to_string(n: i32) -> String;
    aster_i32_to_string(n)
}
```

**9.4 Add Runtime Support**

In `runtime/aster_runtime.c`:

```c
// Convert i32 to string
char* aster_i32_to_string(int32_t n) {
    char* buffer = malloc(32);
    snprintf(buffer, 32, "%d", n);
    return buffer;
}
```

**9.5 Test Serialization**
```bash
# Test AST output
./build/bootstrap/stage1/aster1 emit-ast-json examples/hello.ast | jq .

# Verify it's valid JSON
./build/bootstrap/stage1/aster1 emit-ast-json examples/hello.ast | python3 -m json.tool
```

---

### Step 10: Test with All Core-0 Fixtures

**Goal**: Verify aster1 works with all test programs.

**10.1 List Available Fixtures**
```bash
ls bootstrap/fixtures/core0/compile-pass/
ls bootstrap/fixtures/core0/run-pass/
ls bootstrap/fixtures/core0/compile-fail/
```

**10.2 Test Each Compile-Pass Fixture**
```bash
for file in bootstrap/fixtures/core0/compile-pass/*.ast; do
    echo "Testing $file..."
    ./build/bootstrap/stage1/aster1 emit-ast-json "$file" > /dev/null
    if [ $? -eq 0 ]; then
        echo "  ✓ PASS"
    else
        echo "  ✗ FAIL"
    fi
done
```

**10.3 Test Each Run-Pass Fixture**

These should not only parse but also compile and run:
```bash
for file in bootstrap/fixtures/core0/run-pass/*.ast; do
    echo "Testing $file..."
    basename=$(basename "$file" .ast)
    
    # Compile to LLVM IR
    ./build/bootstrap/stage1/aster1 build "$file" -o "/tmp/${basename}.ll"
    
    # Compile to binary
    clang "/tmp/${basename}.ll" runtime/aster_runtime.c -o "/tmp/${basename}"
    
    # Run
    "/tmp/${basename}"
    echo "  Exit code: $?"
done
```

**10.4 Verify Compile-Fail Fixtures**

These should produce errors:
```bash
for file in bootstrap/fixtures/core0/compile-fail/*.ast; do
    echo "Testing $file..."
    ./build/bootstrap/stage1/aster1 build "$file" -o /dev/null 2>&1
    if [ $? -ne 0 ]; then
        echo "  ✓ CORRECTLY FAILED"
    else
        echo "  ✗ SHOULD HAVE FAILED"
    fi
done
```

**10.5 Document Results**
```bash
cat > test-results.md << 'EOF'
# Core-0 Fixture Test Results

## Compile-Pass Fixtures
- basic_enum.ast: ✓ PASS
- control_flow.ast: ✓ PASS
- simple_function.ast: ✓ PASS
- simple_struct.ast: ✓ PASS
- vec_operations.ast: ✓ PASS

## Run-Pass Fixtures
- fibonacci.ast: ✓ PASS (output: correct)
- hello_world.ast: ✓ PASS (output: "Hello, World!")
- sum_array.ast: ✓ PASS (output: correct)

## Compile-Fail Fixtures
- [list failures and verify they fail correctly]
EOF
```

---

### Step 11: Validate Differential Tests

**Goal**: Ensure aster1 output exactly matches aster0.

**11.1 Full Differential Test Suite**
```bash
# Run all differential tests
./bootstrap/scripts/diff-test-tokens.sh
./bootstrap/scripts/diff-test-ast.sh
./bootstrap/scripts/diff-test-symbols.sh

# Capture results
./bootstrap/scripts/diff-test-tokens.sh > diff-tokens-results.txt
./bootstrap/scripts/diff-test-ast.sh > diff-ast-results.txt
./bootstrap/scripts/diff-test-symbols.sh > diff-symbols-results.txt
```

**11.2 Analyze Differences**

If tests fail:
```bash
# For each failing test, examine the difference
diff bootstrap/goldens/core0/compile-pass/tokens/basic_enum.json \
     /tmp/aster1-output/basic_enum.json
```

**Common Differences**:
1. **Whitespace**: Usually acceptable
2. **Node IDs**: May differ if allocation order varies
3. **Span information**: Line/column numbers might differ slightly
4. **Type representations**: Internal type IDs may vary

**11.3 Define Equivalence Criteria**

Update test scripts to ignore acceptable differences:
```bash
# In diff-test-ast.sh, use jq to normalize before comparing
jq --sort-keys . golden.json > golden-normalized.json
jq --sort-keys . output.json > output-normalized.json
diff golden-normalized.json output-normalized.json
```

---

## Long-term Development Path

After aster1 is working and validated, continue to full compiler implementation.

### Step 12: Complete Lexer (Remaining 20%)

**Goal**: Finish lexer implementation to handle all tokens.

**12.1 Review Lexer Status**
```bash
# Check lexer.ast for TODOs
grep -n "TODO" src/aster1/lexer.ast
```

**12.2 Implement Missing Features**

Common missing pieces:
- Character literals
- Raw string literals
- Comments (block and line)
- Numeric literals (hex, binary, float)
- Lifetime tokens ('a, 'static)
- Advanced operators

**12.3 Add Lexer Tests**
```bash
# Create lexer test file
cat > tests/lexer_tests.ast << 'EOF'
fn test_integers() {
    let a = 42;
    let b = 0x2A;
    let c = 0b101010;
}

fn test_strings() {
    let s1 = "hello";
    let s2 = "with \"quotes\"";
    let s3 = r"raw string";
}
EOF
```

**12.4 Test and Validate**
```bash
./build/bootstrap/stage1/aster1 emit-tokens tests/lexer_tests.ast
```

---

### Step 13: Implement Type Checker

**Goal**: Add type checking to catch errors before codegen.

**13.1 Review Type Checker Skeleton**
```bash
cat src/aster1/typecheck.ast
```

**13.2 Implement Type Checking Functions**

Key functions to implement:
- `typecheck_program(program: ProgramNode) -> TypeCheckResult`
- `typecheck_function(func: FunctionItem) -> TypeCheckResult`
- `typecheck_expr(expr: Expr, expected: Type) -> Type`
- `unify_types(t1: Type, t2: Type) -> Result<Type, TypeError>`

**13.3 Add Type Error Reporting**
```rust
struct TypeError {
    message: String,
    span: Span
}

fn report_type_error(error: TypeError) {
    println("Type error at line " + i32_to_string(error.span.line) + ":");
    println("  " + error.message);
}
```

**13.4 Integrate with Driver**

Update `src/aster1/driver.ast`:
```rust
fn compile(options: CompileOptions, source: String) -> CompileResult {
    // ... lex and parse ...
    
    // Type check
    let type_result = typecheck_program(program);
    if type_result.has_errors {
        return CompileResult {
            success: false,
            errors: format_type_errors(type_result.errors),
            output: ""
        };
    }
    
    // ... continue with IR and codegen ...
}
```

**13.5 Test Type Checker**
```bash
# Create test with type error
cat > /tmp/type_error.ast << 'EOF'
fn main() {
    let x: i32 = "not an integer";  // Type error
}
EOF

# Should report type error
./build/bootstrap/stage1/aster1 build /tmp/type_error.ast
```

---

### Step 14: Implement IR Generation

**Goal**: Convert typed AST to intermediate representation.

**14.1 Review IR Structure**
```bash
cat src/aster1/ir.ast
```

**14.2 Implement IR Lowering**

```rust
fn lower_program(program: ProgramNode) -> IrModule {
    let mut module = new_ir_module();
    
    // Lower each function
    let mut i = 0;
    while i < program.declarations.len() {
        match program.declarations[i] {
            Item::Function(func) => {
                let ir_func = lower_function(func);
                module.functions.push(ir_func);
            },
            _ => {}
        }
        i = i + 1;
    }
    
    module
}

fn lower_function(func: FunctionItem) -> IrFunction {
    let mut ir_func = new_ir_function(func.name);
    
    // Lower parameters
    // Lower body
    // Generate basic blocks
    
    ir_func
}
```

**14.3 Test IR Generation**
```bash
# Generate IR for a simple function
cat > /tmp/simple.ast << 'EOF'
fn add(a: i32, b: i32) -> i32 {
    a + b
}
EOF

./build/bootstrap/stage1/aster1 build /tmp/simple.ast -o /tmp/simple.ll
cat /tmp/simple.ll
```

---

### Step 15: Implement Code Generation

**Goal**: Generate LLVM IR from intermediate representation.

**15.1 Review Codegen Structure**
```bash
cat src/aster1/codegen.ast
```

**15.2 Implement LLVM IR Generation**

```rust
fn codegen_module(module: IrModule) -> String {
    let mut llvm = "";
    
    // Add target triple
    llvm = llvm + "target triple = \"x86_64-pc-linux-gnu\"\n\n";
    
    // Generate each function
    let mut i = 0;
    while i < module.functions.len() {
        llvm = llvm + codegen_function(module.functions[i]);
        llvm = llvm + "\n";
        i = i + 1;
    }
    
    llvm
}

fn codegen_function(func: IrFunction) -> String {
    let mut llvm = "define ";
    
    // Return type
    llvm = llvm + codegen_type(func.return_type) + " ";
    
    // Function name
    llvm = llvm + "@" + func.name + "(";
    
    // Parameters
    // ...
    
    llvm = llvm + ") {\n";
    
    // Basic blocks
    // ...
    
    llvm = llvm + "}\n";
    llvm
}
```

**15.3 Test Code Generation**
```bash
# Full compilation test
./build/bootstrap/stage1/aster1 build examples/hello.ast -o /tmp/hello.ll

# Verify LLVM IR is valid
llvm-as /tmp/hello.ll -o /tmp/hello.bc
```

---

### Step 16: End-to-End Testing

**Goal**: Compile and run complete programs.

**16.1 Create Test Suite**
```bash
mkdir -p tests/e2e
```

**16.2 Write Test Programs**

```bash
# Fibonacci example
cat > tests/e2e/fibonacci.ast << 'EOF'
fn fib(n: i32) -> i32 {
    if n <= 1 {
        n
    } else {
        fib(n - 1) + fib(n - 2)
    }
}

fn main() {
    let result = fib(10);
    aster_println(i32_to_string(result));
}
EOF
```

**16.3 Run E2E Tests**
```bash
#!/bin/bash
# tests/e2e/run-tests.sh

for test in tests/e2e/*.ast; do
    basename=$(basename "$test" .ast)
    echo "Running e2e test: $basename"
    
    # Compile with aster1
    ./build/bootstrap/stage1/aster1 build "$test" -o "/tmp/${basename}.ll"
    
    # Compile to native
    clang "/tmp/${basename}.ll" runtime/aster_runtime.c -o "/tmp/${basename}"
    
    # Run
    "/tmp/${basename}"
    
    # Check exit code
    if [ $? -eq 0 ]; then
        echo "  ✓ PASS"
    else
        echo "  ✗ FAIL"
    fi
done
```

---

### Step 17: Performance and Optimization

**Goal**: Make the compiler faster and more efficient.

**17.1 Profile the Compiler**
```bash
# Time compilation
time ./build/bootstrap/stage1/aster1 build large_program.ast -o output.ll
```

**17.2 Optimize Critical Paths**

Focus on:
- String operations (use string builder pattern)
- Token vector operations
- AST traversal
- Symbol table lookups

**17.3 Add Compilation Flags**
```bash
# Support optimization levels
./build/bootstrap/stage1/aster1 build -O2 program.ast -o optimized.ll
```

---

### Step 18: Documentation and Examples

**Goal**: Make the compiler usable by others.

**18.1 Write User Guide**
```bash
cat > docs/USER_GUIDE.md << 'EOF'
# Aster Compiler User Guide

## Installation
[instructions]

## Basic Usage
[examples]

## Language Reference
[Core-0 language features]

## Examples
[common patterns]
EOF
```

**18.2 Create More Examples**
```bash
mkdir examples/core0/
# Add various example programs
```

**18.3 Write API Documentation**
```bash
# Document each module
# Add comments to public functions
```

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: Bootstrap Compilation Fails

**Symptoms**:
- Parser errors when compiling aster1 source
- Missing type definitions
- Module resolution failures

**Solutions**:
1. **Check C# compiler capabilities**:
   - Verify it supports all Core-0 features
   - Add missing syntax support to parser
   
2. **Simplify source if needed**:
   - Remove advanced features temporarily
   - Use simpler constructs
   
3. **Debug step by step**:
   ```bash
   # Try compiling just one file
   dotnet run --project src/Aster.CLI build src/aster1/ast.ast -o /tmp/ast.ll
   ```

4. **Check logs**:
   - Enable verbose logging in C# compiler
   - Review error messages carefully

---

#### Issue: Differential Tests Fail

**Symptoms**:
- Output differs between aster0 and aster1
- JSON structure mismatch
- Different token counts

**Solutions**:
1. **Normalize before comparing**:
   ```bash
   # Use jq to normalize JSON
   jq --sort-keys . output.json
   ```

2. **Identify acceptable differences**:
   - Node IDs may differ
   - Whitespace is usually OK
   - Internal ordering might vary

3. **Update test expectations**:
   - If aster1 is more correct, update goldens
   - Document why differences exist

---

#### Issue: Runtime Crashes

**Symptoms**:
- Segmentation faults
- Null pointer errors
- Stack overflow

**Solutions**:
1. **Debug with gdb**:
   ```bash
   gdb ./build/bootstrap/stage1/aster1
   run emit-ast-json test.ast
   backtrace
   ```

2. **Check memory management**:
   - Verify malloc/free balance
   - Look for buffer overruns
   - Check string handling

3. **Add assertions**:
   ```c
   assert(ptr != NULL);
   ```

---

#### Issue: LLVM IR Invalid

**Symptoms**:
- llvm-as rejects generated IR
- Compilation errors from clang
- Runtime errors in generated code

**Solutions**:
1. **Validate IR**:
   ```bash
   llvm-as -o /dev/null output.ll
   ```

2. **Compare with working IR**:
   ```bash
   # Generate reference IR from C
   clang -S -emit-llvm simple.c -o reference.ll
   # Compare structure
   ```

3. **Check type consistency**:
   - Verify all types are defined
   - Check function signatures match calls
   - Validate phi nodes and branches

---

## Verification Checklist

Use this checklist to verify completion of each phase.

### ✅ Bootstrap Compilation Complete

- [ ] aster0 (C# compiler) builds successfully
- [ ] All Stage 1 source files present
- [ ] aster0 compiles main.ast without errors
- [ ] aster0 compiles all Stage 1 files
- [ ] Object files link successfully
- [ ] aster1 binary created
- [ ] aster1 --help works
- [ ] aster1 can parse simple programs

### ✅ Post-Bootstrap Implementation Complete

- [ ] File I/O implemented and working
- [ ] JSON serialization complete
- [ ] All Core-0 compile-pass fixtures work
- [ ] All Core-0 run-pass fixtures work
- [ ] Compile-fail fixtures correctly fail
- [ ] Differential tests pass
- [ ] aster1 output matches aster0

### ✅ Full Compiler Implementation Complete

- [ ] Lexer 100% complete
- [ ] Parser 100% complete (already done)
- [ ] Type checker implemented
- [ ] IR generation working
- [ ] Code generation working
- [ ] Can compile and run all examples
- [ ] E2E tests pass
- [ ] Documentation complete

### ✅ Self-Hosting Ready

- [ ] aster1 can compile itself
- [ ] aster2 (compiled by aster1) works
- [ ] aster2 can compile aster1 source
- [ ] aster3 (compiled by aster2) identical to aster2
- [ ] Bootstrap chain is reproducible

---

## Timeline Estimates

| Phase | Task | Estimated Time |
|-------|------|----------------|
| **Immediate** | Bootstrap compilation | 1-2 weeks |
| | Debug compilation issues | 1 week |
| | Test aster1 binary | 2-3 days |
| **Post-Bootstrap** | Implement file I/O | 1 week |
| | Complete JSON serialization | 1 week |
| | Test all Core-0 fixtures | 1 week |
| | Differential testing | 1 week |
| **Long-term** | Complete lexer | 1 week |
| | Implement type checker | 2 weeks |
| | Implement IR generation | 2 weeks |
| | Implement code generation | 2 weeks |
| | E2E testing | 1 week |
| | Documentation | 1 week |
| **Total** | | **14-16 weeks** |

---

## Success Criteria

The bootstrap process is successful when:

1. **aster1 binary exists** and runs
2. **aster1 can parse** all Core-0 programs
3. **Differential tests pass** (output matches aster0)
4. **aster1 can compile** and run simple programs
5. **All fixtures work** (compile-pass, run-pass, compile-fail)
6. **Documentation is complete** and accurate
7. **Self-hosting works** (aster1 can compile itself)

---

## Resources

### Documentation
- [TOOLCHAIN.md](../TOOLCHAIN.md) - Build instructions
- [STATUS.md](../STATUS.md) - Project status
- [STAGE1_PARSER_GUIDE.md](STAGE1_PARSER_GUIDE.md) - Parser implementation
- [STAGE1_PARSER_COMPLETE.md](STAGE1_PARSER_COMPLETE.md) - Parser summary

### Code
- Stage 0 (C#): `src/Aster.*/`
- Stage 1 (Aster): `src/aster1/`
- Runtime: `runtime/`
- Tests: `bootstrap/fixtures/`
- Scripts: `bootstrap/scripts/`

### External Resources
- [LLVM Language Reference](https://llvm.org/docs/LangRef.html)
- [Crafting Interpreters](http://craftinginterpreters.com/)
- [Rust Compiler Development Guide](https://rustc-dev-guide.rust-lang.org/)

---

## Getting Help

If you encounter issues:

1. **Check documentation** in `docs/` directory
2. **Review error messages** carefully
3. **Search for similar issues** in repository
4. **Enable verbose logging** in compiler
5. **Create minimal reproduction** case
6. **Ask for help** with specific error messages and steps to reproduce

---

**Last Updated**: 2026-02-15  
**Status**: Ready for bootstrap compilation  
**Next Milestone**: aster1 binary creation
