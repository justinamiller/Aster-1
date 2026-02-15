# Quick Start Guide: Building and Running the Aster Compiler

This guide will help you build the .NET compiler (Stage 0) and compile Aster code.

## Prerequisites

- .NET 10 SDK or later
- Linux, macOS, or Windows with WSL

## Step 1: Build the .NET Compiler (Stage 0)

The Aster compiler bootstrap process starts with a C# implementation (Stage 0) that can compile Aster code.

### Option 1: Using the build script (Recommended)

```bash
./build.sh
```

### Option 2: Using dotnet directly

```bash
dotnet build Aster.slnx --configuration Release
```

## Step 2: Verify the Build

Run the smoke test to ensure everything is working:

```bash
./scripts/smoke_test.sh
```

You should see:
```
✓ Smoke test PASSED
The Aster compiler is working correctly!
```

## Step 3: Compile Aster Code

Now you can compile Aster programs to LLVM IR.

### Basic Usage

```bash
# Compile an Aster file to LLVM IR
dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll build <file.ast>

# Type-check only (no code generation)
dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll check <file.ast>

# Emit LLVM IR to stdout
dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll emit-llvm <file.ast>
```

### Try the Examples

```bash
# Simple hello world
dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll build examples/simple_hello.ast

# More complex example with structs and functions
dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll build aster/stage1_demo.ast

# Using the standard library
dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll build examples/stdlib_hello.ast
```

The compiled output will be saved as `<filename>.ll` (LLVM IR).

## Step 4: View Generated Code

After compilation, you can view the generated LLVM IR:

```bash
cat examples/simple_hello.ll
```

## Available Commands

```
Commands:
  build         Compile source to LLVM IR
  check         Type-check without compiling
  emit-llvm     Emit LLVM IR to stdout
  emit-tokens   Emit token stream as JSON (for bootstrap)
  run           Compile and prepare for execution
  fmt           Format source files
  lint          Lint source files
  init          Initialize a new package
  doc           Generate documentation
  test          Run tests
  lsp           Start language server
  --help        Show help message
  --version     Show version
```

## Example Aster Program

Create a file `hello.ast`:

```rust
// Simple hello world program

fn main() {
    print("Hello from Aster!")
}
```

Compile it:

```bash
dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll build hello.ast
```

This generates `hello.ll` containing the LLVM IR.

## Running Tests

Run the compiler test suite:

```bash
dotnet test --configuration Release
```

## Bootstrap Stages

The Aster compiler uses a multi-stage bootstrap process:

- **Stage 0** (Current): C# seed compiler - ✅ COMPLETE
- **Stage 1**: Minimal Aster compiler (in Aster) - 🚧 IN PROGRESS
- **Stage 2**: Expanded Aster compiler - ⚙️ PLANNED
- **Stage 3**: Full self-hosting compiler - ⚙️ PLANNED

For more information on the bootstrap process, see:
- `README_BOOTSTRAP.md` - Bootstrap overview
- `bootstrap/README.md` - Bootstrap infrastructure
- `BOOTSTRAP_COMPLETE_SUMMARY.md` - Current status

## Next Steps

- Explore examples in `examples/` directory
- Read the language documentation in `docs/`
- Check the standard library in `aster/stdlib/`
- Learn about the compiler architecture in `README.md`

## Getting Help

- Run `dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll --help` for command help
- See `README.md` for detailed architecture documentation
- Check `examples/README.md` for example programs
