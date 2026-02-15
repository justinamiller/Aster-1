# Aster Runtime Library

This directory contains the minimal runtime library for Aster programs.

## Overview

The Aster runtime provides essential intrinsics that compiled Aster programs depend on. The runtime is implemented in C for portability and simplicity.

## Runtime ABI

The runtime exposes the following intrinsics:

### Memory Management

- `void* aster_malloc(size_t size)` — Allocate memory
- `void aster_free(void* ptr)` — Free memory

### I/O Operations

- `void aster_write_stdout(const char* ptr, size_t len)` — Write raw bytes to stdout
- `int aster_puts(const char* str)` — Print null-terminated string (wrapper for `puts`)
- `void aster_print_int(long long value)` — Print an integer
- `void aster_println(void)` — Print a newline

### Program Control

- `void aster_panic(const char* msg, size_t len)` — Panic handler (aborts with message)
- `void aster_exit(int code)` — Exit the program with exit code

## Building the Runtime

### Build as Static Library

```bash
# Compile runtime to object file
gcc -c runtime/aster_runtime.c -o runtime/aster_runtime.o

# Create static library
ar rcs runtime/libaster_runtime.a runtime/aster_runtime.o
```

### Build as Shared Library

```bash
# Linux
gcc -shared -fPIC runtime/aster_runtime.c -o runtime/libaster_runtime.so

# macOS
gcc -dynamiclib runtime/aster_runtime.c -o runtime/libaster_runtime.dylib

# Windows (MinGW)
gcc -shared runtime/aster_runtime.c -o runtime/aster_runtime.dll
```

## Using the Runtime

### Linking with Compiled Aster Programs

When compiling Aster programs to LLVM IR and then to native executables, link with the runtime:

```bash
# Step 1: Compile Aster to LLVM IR
dotnet run --project src/Aster.CLI -- build examples/hello.ast --emit-llvm

# Step 2: Compile runtime
gcc -c runtime/aster_runtime.c -o runtime/aster_runtime.o

# Step 3: Compile LLVM IR to object file
llc examples/hello.ll -filetype=obj -o examples/hello.o

# Step 4: Link with runtime
gcc examples/hello.o runtime/aster_runtime.o -o examples/hello

# Step 5: Run
./examples/hello
```

### One-Step Compilation (with runtime)

```bash
# Compile runtime once
gcc -c runtime/aster_runtime.c -o runtime/aster_runtime.o

# Compile and link Aster program
dotnet run --project src/Aster.CLI -- build examples/hello.ast --emit-llvm
clang examples/hello.ll runtime/aster_runtime.o -o examples/hello
./examples/hello
```

## LLVM IR Declarations

The Aster compiler automatically declares these runtime functions in the generated LLVM IR. You do not need to manually declare them.

Example LLVM IR declarations:

```llvm
; Runtime function declarations
declare void @aster_panic(ptr, i64)
declare ptr @aster_malloc(i64)
declare void @aster_free(ptr)
declare void @aster_write_stdout(ptr, i64)
declare void @aster_exit(i32)
declare i32 @aster_puts(ptr)
declare void @aster_print_int(i64)
declare void @aster_println()
```

## Platform Support

The runtime is written in standard C and should work on any platform with a C compiler:

- ✅ Linux (glibc, musl)
- ✅ macOS
- ✅ Windows (MSVC, MinGW)
- ✅ BSD systems
- ✅ Other POSIX systems

## Dependencies

The runtime depends only on the C standard library:
- `<stdio.h>` — I/O operations
- `<stdlib.h>` — Memory allocation and program control
- `<string.h>` — String operations
- `<unistd.h>` — POSIX APIs (if available)

No external dependencies are required.

## Future Enhancements

Planned additions to the runtime:

1. **Custom allocator interface** — Support for effect-tracked allocation
2. **Async runtime** — Support for async/await
3. **Thread support** — Concurrency primitives
4. **Error handling** — Structured exception handling
5. **FFI helpers** — Foreign function interface utilities

## License

This runtime library is part of the Aster compiler project.
