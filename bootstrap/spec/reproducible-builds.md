# Reproducible Builds Specification

## Overview

This document defines the rules and mechanisms for achieving deterministic, reproducible builds in the Aster compiler. A build is "reproducible" if compiling the same source code with the same compiler version produces bit-for-bit identical outputs, regardless of time, location, or build environment.

## Why Reproducible Builds Matter

1. **Verification**: Anyone can verify that a binary matches claimed source code
2. **Security**: Detect tampering or backdoors in compiled binaries
3. **Caching**: Incremental compilation relies on stable hashes
4. **Trust**: Build the self-hosting trust chain (aster3 == aster3')
5. **Debugging**: Eliminate "works on my machine" issues

## Reproducibility Requirements

### MUST BE DETERMINISTIC

The following MUST produce identical results every time:

1. ✅ **Hash Functions**
   - SHA-256 for content hashing
   - Same input → same hash, always
   - No randomness, no UUIDs

2. ✅ **Symbol Ordering**
   - Functions, types, modules sorted deterministically
   - Alphabetical or hash-based ordering
   - Not insertion order or pointer order

3. ✅ **File Ordering**
   - Module compilation order is deterministic
   - Directory traversal is sorted
   - No filesystem-dependent ordering

4. ✅ **Code Generation**
   - LLVM IR generation is deterministic
   - SSA value numbering is stable
   - MIR instruction ordering is deterministic

5. ✅ **Optimization**
   - Optimization passes preserve determinism
   - No heuristics based on pointer addresses
   - No randomness in pass ordering

### MUST NOT VARY

The following MUST NOT affect output:

1. ❌ **Timestamps**
   - Build time
   - File modification times
   - System clock

2. ❌ **File Paths** (without --path-map)
   - Absolute paths in debug info
   - Build directory location
   - User home directory

3. ❌ **Machine-Specific Data**
   - CPU count (affects parallelism, not output)
   - Available memory
   - Host architecture (when cross-compiling)

4. ❌ **Random Numbers**
   - No `rand()` in code generation
   - No UUIDs
   - No entropy sources

5. ❌ **Pointer Addresses**
   - No sorting by pointer value
   - No hash codes based on object addresses
   - No iteration over HashSet (unordered)

---

## Stable Hashing

### Hash Algorithm

**Standard**: SHA-256 (256-bit output)

**Rationale**:
- Cryptographically strong (collision-resistant)
- Standardized (FIPS 180-4)
- Fast on modern CPUs
- 256 bits is sufficient for uniqueness

**Implementation**: See `/src/Aster.Compiler.Incremental/StableHasher.cs`

### Hash Inputs

When hashing source code or IR:

```csharp
public static class StableHasher
{
    public static string HashBytes(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string HashString(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return HashBytes(bytes);
    }
}
```

**Canonical Encoding**:
- UTF-8 for all text
- Big-endian for multi-byte integers
- Newlines normalized to `\n` (LF)
- No BOM (Byte Order Mark)

### Hash Stability

**MUST**:
- ✅ Hash function never changes (SHA-256 forever)
- ✅ Hash encoding never changes (hex lowercase)
- ✅ Input canonicalization never changes (UTF-8, big-endian)

**Migration**:
- If hash algorithm must change (e.g., SHA-256 broken):
  - Version the hash (`v2:sha3-256:...`)
  - Invalidate all caches
  - Document migration

---

## Stable Symbol Naming

### Symbol ID Assignment

**Problem**: Symbols need unique IDs, but IDs must be deterministic.

**Solution**: Hash-based IDs

```rust
// BAD: Pointer-based ID
let symbol_id = SymbolId::new(ptr_address);

// GOOD: Hash-based ID
let symbol_id = SymbolId::from_hash(
    hash_bytes(&[module_name, symbol_name, symbol_kind])
);
```

**Properties**:
- ✅ Same symbol → same ID, always
- ✅ Different symbols → different IDs (with high probability)
- ✅ IDs are globally unique (across modules)
- ✅ IDs are stable across builds

### Module Ordering

**Rule**: Modules are processed in deterministic order.

**Implementation**:
```csharp
// BAD: Arbitrary filesystem order
var modules = Directory.GetFiles("src/*.ast");

// GOOD: Sorted order
var modules = Directory.GetFiles("src/*.ast")
                       .OrderBy(path => path)
                       .ToList();
```

### Type ID Assignment

**Rule**: Type IDs are derived from type structure, not allocation order.

```csharp
// Type ID is hash of type structure
public class TypeId
{
    public static TypeId FromType(IType type)
    {
        var hash = type switch
        {
            PrimitiveType p => $"prim:{p.Name}",
            StructType s => $"struct:{s.Module}::{s.Name}",
            FunctionType f => $"fn:({string.Join(",", f.ParamTypes)})->{f.ReturnType}",
            _ => throw new Exception($"Unknown type: {type}")
        };
        return new TypeId(StableHasher.HashString(hash));
    }
}
```

### Symbol Table Ordering

**Rule**: When emitting symbols, sort deterministically.

```csharp
// BAD: Dictionary iteration order (non-deterministic)
foreach (var (name, symbol) in symbolTable)
{
    EmitSymbol(symbol);
}

// GOOD: Sorted by name
foreach (var (name, symbol) in symbolTable.OrderBy(kv => kv.Key))
{
    EmitSymbol(symbol);
}
```

---

## Stable Module Ordering

### Compilation Order

**Rule**: Modules are compiled in topological order (dependencies first), with ties broken alphabetically.

```csharp
public static List<Module> TopologicalSort(List<Module> modules)
{
    // 1. Build dependency graph
    var graph = BuildDependencyGraph(modules);
    
    // 2. Topological sort
    var sorted = TopSort(graph);
    
    // 3. Break ties alphabetically (deterministic)
    var result = new List<Module>();
    foreach (var tier in sorted)
    {
        var tierSorted = tier.OrderBy(m => m.Name).ToList();
        result.AddRange(tierSorted);
    }
    
    return result;
}
```

### Parallel Compilation

**Challenge**: Parallel compilation must produce same output as sequential.

**Solution**: Deterministic output ordering

```csharp
public static async Task<List<CompiledModule>> CompileParallel(List<Module> modules)
{
    // Compile in parallel (any order)
    var tasks = modules.Select(m => CompileModuleAsync(m)).ToList();
    var results = await Task.WhenAll(tasks);
    
    // Sort results deterministically before combining
    var sortedResults = results.OrderBy(r => r.ModuleName).ToList();
    
    return sortedResults;
}
```

**Key Insight**: Parallelism affects *speed*, not *output*.

---

## Deterministic Compilation Rules

### Compiler Flags

**Reproducible Flags**:
```bash
# Recommended flags for reproducibility
aster build \
  --reproducible \
  --path-map "/home/user/project=/source" \
  --llvm-flags "-relocation-model=pic -code-model=medium" \
  --seed 0
```

**Flag Meanings**:
- `--reproducible`: Enable all reproducibility features
- `--path-map`: Remap absolute paths to canonical paths
- `--llvm-flags`: Pass stable flags to LLVM
- `--seed`: Deterministic seed for any pseudo-random operations (debugging only)

### LLVM IR Generation

**Rules**:
1. ✅ No timestamps in IR comments
2. ✅ No absolute file paths (use remapped paths)
3. ✅ Stable value numbering (SSA)
4. ✅ Stable basic block ordering
5. ✅ Stable function ordering
6. ✅ Stable global ordering

**Example**:
```llvm
; BAD: Timestamp in comment
; Generated on 2026-02-14 at 10:30:45

; GOOD: No timestamp
; Generated by Aster compiler v0.1.0

; BAD: Absolute path
!1 = !DIFile(filename: "/home/user/project/src/main.ast")

; GOOD: Remapped path
!1 = !DIFile(filename: "/source/src/main.ast")
```

### Debug Information

**Challenge**: Debug info includes file paths and line numbers.

**Solution**: Path remapping

```bash
# Remap /home/user/project to /source
--path-map "/home/user/project=/source"
```

**DWARF Generation**:
- ✅ Use remapped paths
- ✅ No timestamps
- ✅ Stable line number tables
- ✅ Stable inline info

**Example**:
```
DW_AT_name: "/source/src/main.ast"
DW_AT_producer: "Aster compiler 0.1.0"
DW_AT_language: DW_LANG_Rust  # Closest match
```

---

## Toolchain Pinning

### LLVM Version

**Rule**: Pin exact LLVM version in build scripts.

**Pinned Version**: LLVM 19.1.0

**Rationale**:
- Different LLVM versions may generate different code
- Optimization passes evolve
- ABI compatibility

**Enforcement**:
```bash
# Check LLVM version
llc --version | grep "LLVM version 19.1"
if [ $? -ne 0 ]; then
    echo "Error: LLVM 19.1 required"
    exit 1
fi
```

**Documentation**: `/bootstrap/seed/aster-seed-version.txt`

### Clang Version (for C runtime)

**Pinned Version**: Clang 19.1.0 (matches LLVM)

**Flags**:
```bash
clang -O2 -fPIC -fno-omit-frame-pointer \
      -fno-strict-aliasing \
      -fdebug-prefix-map=/home/user/project=/source \
      runtime.c -o runtime.o
```

**Key Flags**:
- `-fdebug-prefix-map`: Remap paths
- `-fno-strict-aliasing`: Deterministic aliasing
- No `-march=native` (not reproducible across machines)

### .NET Version (for C# compiler)

**Pinned Version**: .NET 10

**Enforcement**:
```xml
<!-- global.json -->
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "disable"
  }
}
```

**Rationale**:
- C# compiler (seed) must be reproducible
- .NET SDK version affects output

---

## Path Remapping

### Problem

Absolute paths embed machine-specific information:
- `/home/alice/aster/src/main.ast` (Alice's machine)
- `/Users/bob/aster/src/main.ast` (Bob's machine)
- `/workspace/aster/src/main.ast` (CI machine)

These make builds non-reproducible.

### Solution: --path-map

**Syntax**:
```bash
--path-map <from>=<to>
```

**Examples**:
```bash
# Map build directory to /source
--path-map "/home/alice/aster=/source"

# Multiple mappings
--path-map "/home/alice/aster=/source" \
           "/usr/lib/aster=/stdlib"
```

**Effect**:
- All paths starting with `<from>` are replaced with `<to>`
- Applied to:
  - Debug info (DWARF)
  - Diagnostic messages
  - Compiler internal paths
  - Generated comments

**Implementation**:
```csharp
public class PathMapper
{
    private List<(string From, string To)> mappings;
    
    public string Remap(string path)
    {
        foreach (var (from, to) in mappings)
        {
            if (path.StartsWith(from))
            {
                return to + path.Substring(from.Length);
            }
        }
        return path;  // No mapping found
    }
}
```

### Canonical Paths

**Rule**: Diagnostics use canonical (remapped) paths.

```bash
# BAD: Absolute path
error[E0001]: undefined variable `x`
  --> /home/alice/aster/src/main.ast:5:10

# GOOD: Remapped path
error[E0001]: undefined variable `x`
  --> /source/src/main.ast:5:10
```

---

## Stable Timestamps

### Problem

Embedding timestamps breaks reproducibility:
- Build time
- File modification time
- Compilation time

### Solution: No Timestamps

**Rule**: Never embed timestamps in output.

**Exception**: If timestamp is needed (e.g., for licensing):
- Use `SOURCE_DATE_EPOCH` environment variable
- Falls back to a fixed epoch (e.g., 2000-01-01)

**Example**:
```csharp
public static DateTime GetBuildTime()
{
    // Check environment variable
    if (Environment.GetEnvironmentVariable("SOURCE_DATE_EPOCH") is string epoch)
    {
        return DateTimeOffset.FromUnixTimeSeconds(long.Parse(epoch)).DateTime;
    }
    
    // Fixed epoch for reproducibility
    return new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
```

**LLVM IR**:
```llvm
; BAD: Current timestamp
@build_time = constant [20 x i8] c"2026-02-14 10:30:45\00"

; GOOD: No timestamp, or SOURCE_DATE_EPOCH
@build_time = constant [20 x i8] c"2000-01-01 00:00:00\00"
```

---

## Verification

### Reproducibility Tests

**Test 1: Same Source, Same Output**

```bash
# Build 1
aster build --reproducible src/*.ast -o aster1

# Build 2 (identical source)
aster build --reproducible src/*.ast -o aster2

# Verify
diff <(sha256sum aster1) <(sha256sum aster2)
# Should be identical
```

**Test 2: Different Paths, Same Output**

```bash
# Build in /tmp/build1
cd /tmp/build1
aster build --reproducible --path-map "/tmp/build1=/source" src/*.ast -o aster1

# Build in /tmp/build2
cd /tmp/build2
aster build --reproducible --path-map "/tmp/build2=/source" src/*.ast -o aster2

# Verify
diff <(sha256sum /tmp/build1/aster1) <(sha256sum /tmp/build2/aster2)
# Should be identical
```

**Test 3: Different Times, Same Output**

```bash
# Build 1
SOURCE_DATE_EPOCH=0 aster build --reproducible src/*.ast -o aster1

# Sleep 10 seconds
sleep 10

# Build 2
SOURCE_DATE_EPOCH=0 aster build --reproducible src/*.ast -o aster2

# Verify
diff <(sha256sum aster1) <(sha256sum aster2)
# Should be identical
```

**Test 4: Parallel vs Sequential, Same Output**

```bash
# Build 1 (sequential)
aster build --reproducible --jobs 1 src/*.ast -o aster1

# Build 2 (parallel)
aster build --reproducible --jobs 8 src/*.ast -o aster2

# Verify
diff <(sha256sum aster1) <(sha256sum aster2)
# Should be identical
```

### Build Manifest

**Purpose**: Record all inputs and outputs for verification.

**Format**: JSON

```json
{
  "version": "1.0",
  "compiler_version": "0.1.0",
  "llvm_version": "19.1.0",
  "build_flags": [
    "--reproducible",
    "--path-map", "/workspace=/source",
    "--optimization", "2"
  ],
  "inputs": [
    {
      "file": "/source/src/main.ast",
      "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
    },
    {
      "file": "/source/src/lexer.ast",
      "sha256": "a4e1c2d3f4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1"
    }
  ],
  "outputs": [
    {
      "file": "aster",
      "sha256": "5a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b"
    }
  ],
  "build_time_utc": "2000-01-01T00:00:00Z",
  "build_duration_seconds": 42.5
}
```

**Storage**: `aster-manifest.json` alongside binary

**Usage**:
- Verify inputs haven't changed
- Reproduce exact build
- Audit trail

---

## Escape Hatches

### Non-Reproducible Builds

**Use Case**: Development builds where reproducibility is not required.

**Flag**: `--non-reproducible` (default)

**Differences**:
- ❌ May embed timestamps
- ❌ May use absolute paths
- ❌ May use random seeds (for debugging)
- ✅ Faster (no sorting overhead)

**When to Use**:
- Local development
- Debug builds
- Quick iterations

**When NOT to Use**:
- CI/CD builds
- Release builds
- Bootstrap builds
- Verification builds

---

## Reproducibility Checklist

Before releasing a compiler stage:

- [ ] `--reproducible` flag implemented and tested
- [ ] All hash functions use SHA-256
- [ ] Symbol IDs are hash-based, not pointer-based
- [ ] Module compilation order is deterministic
- [ ] Parallel builds produce identical output to sequential
- [ ] No timestamps in output (or use SOURCE_DATE_EPOCH)
- [ ] Path remapping implemented (--path-map)
- [ ] LLVM version pinned in build scripts
- [ ] Reproducibility tests pass (same source → same binary)
- [ ] Build manifest generated and includes all inputs/outputs
- [ ] Documentation updated with reproducibility instructions

---

## Verification Commands

### Automated Verification

```bash
# Run reproducibility test suite
./bootstrap/scripts/verify.sh --reproducibility

# Test specific stage
./bootstrap/scripts/verify.sh --reproducibility --stage 2

# Verify self-hosting (aster3 == aster3')
./bootstrap/scripts/verify.sh --self-check
```

### Manual Verification

```bash
# Build twice, compare
aster build --reproducible src/*.ast -o aster1
aster build --reproducible src/*.ast -o aster2
sha256sum aster1 aster2

# Expected output:
# 5a2b3c4d... aster1
# 5a2b3c4d... aster2  (same hash)
```

---

## Common Pitfalls

### ❌ Using Dictionary Iteration

```csharp
// BAD: Dictionary order is non-deterministic
foreach (var symbol in symbolTable.Values)
{
    Emit(symbol);
}

// GOOD: Sort before iteration
foreach (var symbol in symbolTable.OrderBy(kv => kv.Key).Select(kv => kv.Value))
{
    Emit(symbol);
}
```

### ❌ Using DateTime.Now

```csharp
// BAD: Current time
var buildTime = DateTime.Now;

// GOOD: Fixed epoch or SOURCE_DATE_EPOCH
var buildTime = GetReproducibleBuildTime();
```

### ❌ Using Guid.NewGuid()

```csharp
// BAD: Random GUID
var id = Guid.NewGuid();

// GOOD: Hash-based ID
var id = ComputeStableId(name);
```

### ❌ Using GetHashCode()

```csharp
// BAD: Object hash code (non-deterministic)
var hash = obj.GetHashCode();

// GOOD: Stable hash
var hash = ComputeStableHash(obj);
```

---

## Performance Considerations

**Myth**: Reproducible builds are slower.

**Reality**: Minimal overhead (<5%)
- Sorting symbols: O(n log n) (small n)
- Stable hashing: SHA-256 is fast (~500 MB/s)
- Path remapping: String operations (negligible)

**Optimization**: Use --non-reproducible for development, --reproducible for releases.

---

## Standards Compliance

This specification aligns with:
- **Reproducible Builds Project**: https://reproducible-builds.org/
- **SOURCE_DATE_EPOCH**: https://reproducible-builds.org/specs/source-date-epoch/
- **LLVM Reproducibility**: https://releases.llvm.org/19.1.0/docs/DeterministicBuilds.html

---

## Conclusion

Reproducible builds are essential for:
- ✅ **Verification**: Anyone can verify binaries
- ✅ **Security**: Detect tampering
- ✅ **Trust**: Self-hosting requires reproducibility
- ✅ **Caching**: Incremental compilation relies on it
- ✅ **Debugging**: Eliminate environment-specific bugs

The Aster compiler achieves reproducibility through:
- ✅ Stable hashing (SHA-256)
- ✅ Deterministic symbol ordering
- ✅ Path remapping
- ✅ No timestamps
- ✅ Pinned toolchain versions
- ✅ Comprehensive verification tests

**Philosophy**:
> "If you can't reproduce it, you can't verify it. If you can't verify it, you can't trust it."
