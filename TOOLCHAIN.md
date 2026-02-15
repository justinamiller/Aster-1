# Aster Compiler Toolchain Guide

This document describes how to use the Aster compiler toolchain to compile `.ast` source files to native executables.

## Prerequisites

### Required Tools

1. **.NET 10 SDK** — For building the Stage 0 C# compiler
   - Download: https://dotnet.microsoft.com/download/dotnet/10.0
   - Verify: `dotnet --version` (should be 10.0.x)

2. **LLVM 18+** — For compiling LLVM IR to native code
   - Ubuntu/Debian: `sudo apt-get install llvm-18 clang-18`
   - macOS: `brew install llvm@18`
   - Windows: Download from https://releases.llvm.org/
   - Verify: `llc --version` (should show version 18 or higher)

3. **Clang 18+** — C compiler for linking
   - Usually installed with LLVM
   - Verify: `clang --version` (should show version 18 or higher)

### Optional Tools

- **lli** — LLVM interpreter for testing (installed with LLVM)
- **opt** — LLVM optimizer (installed with LLVM)

## Building the Compiler

### Stage 0: C# Seed Compiler

Build the Stage 0 compiler from C# source:

```bash
# Clone the repository
git clone https://github.com/justinamiller/Aster-1.git
cd Aster-1

# Build the compiler
dotnet build Aster.slnx --configuration Release

# Run tests
dotnet test --configuration Release
```

The Stage 0 compiler will be available at:
- Debug: `build/bootstrap/stage0/Aster.CLI.dll` (via bootstrap scripts)
- Direct: `src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll`

## Compilation Pipeline

### Step 1: Aster Source → LLVM IR

Compile an Aster source file to LLVM IR:

```bash
# Using dotnet run (development)
dotnet run --project src/Aster.CLI -- build examples/simple_hello.ast --emit-llvm

# Using built compiler (production)
dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll build examples/simple_hello.ast --emit-llvm
```

This produces: `examples/simple_hello.ll`

### Step 2: LLVM IR → Native Executable

Compile LLVM IR to a native executable:

```bash
# Option A: Direct compilation with clang
clang examples/simple_hello.ll -o examples/simple_hello

# Option B: Compile to object file, then link
llc examples/simple_hello.ll -filetype=obj -o examples/simple_hello.o
clang examples/simple_hello.o -o examples/simple_hello

# Option C: With optimizations
clang -O3 examples/simple_hello.ll -o examples/simple_hello
```

### Step 3: Run the Executable

```bash
./examples/simple_hello
# Output: Hello from Aster!
```

## Complete Example

Here's a complete workflow from source to execution:

```bash
# 1. Create a simple Aster program
cat > hello.ast << 'EOF'
fn main() {
    print("Hello, World!")
}
EOF

# 2. Compile to LLVM IR
dotnet run --project src/Aster.CLI -- build hello.ast --emit-llvm

# 3. Compile LLVM IR to native executable
clang hello.ll -o hello

# 4. Run the program
./hello
# Output: Hello, World!
```

## Compiler Commands

### Build Commands

```bash
# Compile to LLVM IR (default output: <input>.ll)
dotnet run --project src/Aster.CLI -- build <file.ast> --emit-llvm

# Specify output file
dotnet run --project src/Aster.CLI -- build <file.ast> --emit-llvm -o output.ll

# Type-check only (no code generation)
dotnet run --project src/Aster.CLI -- check <file.ast>

# Emit token stream (for debugging/testing)
dotnet run --project src/Aster.CLI -- emit-tokens <file.ast>
```

### Output Formats

The compiler currently supports:

- **LLVM IR Text** (`.ll`) — Human-readable LLVM intermediate representation
- **Token Stream** (JSON) — Lexical analysis output for testing

**Note**: Direct native executable output is not yet implemented. Use the two-step process (`.ast` → `.ll` → native) described above.

## Runtime Requirements

### External Functions

Aster programs depend on these C standard library functions, which are automatically declared in the generated LLVM IR:

- `puts(ptr)` — Print string to stdout
- `printf(ptr, ...)` — Formatted output
- `malloc(i64)` — Allocate memory
- `free(ptr)` — Free memory
- `exit(i32)` — Terminate program

These are provided by the system C library and linked automatically by clang.

### Future Runtime ABI

The following runtime intrinsics are planned for future releases:

- `aster_panic(msg: ptr, len: i64)` — Panic handler
- `aster_write_stdout(ptr: ptr, len: i64)` — Direct stdout write
- Custom allocator interface for `@alloc` effect tracking

## Optimization Levels

When compiling LLVM IR to native code, you can use clang optimization flags:

```bash
# No optimization (fastest compilation, slowest execution)
clang hello.ll -o hello

# Optimize for speed
clang -O2 hello.ll -o hello

# Maximum optimization
clang -O3 hello.ll -o hello

# Optimize for size
clang -Os hello.ll -o hello
```

## Debugging

### Viewing LLVM IR

```bash
# Generated LLVM IR is human-readable
cat examples/simple_hello.ll

# Run through LLVM interpreter (no compilation needed)
lli examples/simple_hello.ll
```

### Optimizing LLVM IR

```bash
# Apply LLVM optimizations to IR
opt -O3 examples/simple_hello.ll -S -o examples/simple_hello_opt.ll

# Then compile optimized IR
clang examples/simple_hello_opt.ll -o examples/simple_hello
```

### Disassembly

```bash
# Compile to assembly instead of object code
llc examples/simple_hello.ll -o examples/simple_hello.s

# View the assembly
cat examples/simple_hello.s
```

## Troubleshooting

### Error: `dotnet: command not found`

Install .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0

### Error: `clang: command not found`

Install LLVM/Clang:
- Ubuntu/Debian: `sudo apt-get install clang-18`
- macOS: `brew install llvm@18`
- Windows: Add LLVM bin directory to PATH

### Error: `undefined reference to 'puts'`

The C standard library is not being linked. This should not happen with clang by default. If you're using `llc` + custom linker, you need to explicitly link the C runtime:

```bash
llc hello.ll -filetype=obj -o hello.o
clang hello.o -lc -o hello
```

### Error: LLVM IR compilation fails

Check that your LLVM version is 18 or higher:
```bash
llc --version
clang --version
```

If using an older version, the IR syntax may be incompatible.

## Platform Support

### Tested Platforms

- ✅ Linux x86_64 (Ubuntu 22.04+)
- ✅ macOS x86_64 (Intel)
- ✅ macOS ARM64 (Apple Silicon)
- ⚠️ Windows x86_64 (requires WSL or LLVM for Windows)

### Cross-Compilation

LLVM IR is platform-independent. You can compile on one platform and run the IR on another:

```bash
# On Linux, generate IR
dotnet run --project src/Aster.CLI -- build hello.ast --emit-llvm

# Copy hello.ll to macOS
# On macOS, compile to native
clang hello.ll -o hello
./hello
```

For true cross-compilation to a different target architecture:

```bash
# Compile for ARM64 on x86_64
llc -march=aarch64 hello.ll -filetype=obj -o hello_arm64.o
clang --target=aarch64-linux-gnu hello_arm64.o -o hello_arm64
```

## Bootstrap Stages

### Stage 0 (Current): C# Compiler

- **Source**: C# code in `/src/`
- **Output**: LLVM IR (`.ll` files)
- **Build**: `dotnet build Aster.slnx`
- **Status**: ✅ Complete and operational

### Stage 1 (In Progress): Minimal Aster Compiler

- **Source**: Aster code in `/src/aster1/`
- **Compiled by**: Stage 0 (C# compiler)
- **Output**: LLVM IR
- **Language subset**: Core-0 (no traits, no generics, no methods)
- **Status**: 🚧 Partial implementation

### Stage 2 (Planned): Expanded Aster Compiler

- **Source**: Aster code (with generics, traits)
- **Compiled by**: Stage 1
- **Output**: LLVM IR
- **Status**: ⚙️ Infrastructure ready

### Stage 3 (Planned): Full Self-Hosted Compiler

- **Source**: Full Aster language
- **Compiled by**: Stage 2
- **Output**: LLVM IR
- **Status**: ⚙️ Infrastructure ready

For detailed bootstrap information, see [README_BOOTSTRAP.md](README_BOOTSTRAP.md).

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Test Aster

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Install LLVM
        run: |
          sudo apt-get update
          sudo apt-get install -y llvm-18 clang-18
      
      - name: Build Compiler
        run: dotnet build Aster.slnx --configuration Release
      
      - name: Run Tests
        run: dotnet test --configuration Release
      
      - name: Compile Example to LLVM IR
        run: dotnet run --project src/Aster.CLI -- build examples/simple_hello.ast --emit-llvm
      
      - name: Compile LLVM IR to Native
        run: clang-18 examples/simple_hello.ll -o examples/simple_hello
      
      - name: Run Example
        run: ./examples/simple_hello
      
      - name: Verify Output
        run: |
          OUTPUT=$(./examples/simple_hello)
          echo "Output: $OUTPUT"
          test "$OUTPUT" = "Hello from Aster!"
```

## Next Steps

1. **Implement Runtime Library** — Create a minimal runtime in C with required intrinsics
2. **Native Compilation** — Add direct native executable output (bypassing manual LLVM compilation)
3. **Package Manager** — Integrate with system package managers for easier installation
4. **IDE Support** — Language server protocol implementation for editor integration

## Resources

- [LLVM Documentation](https://llvm.org/docs/)
- [Clang Compiler User's Manual](https://clang.llvm.org/docs/UsersManual.html)
- [Aster Language Specification](docs/spec/)
- [Bootstrap Guide](README_BOOTSTRAP.md)
- [Status Tracking](STATUS.md)

## License

This toolchain and documentation are part of the Aster compiler project.
