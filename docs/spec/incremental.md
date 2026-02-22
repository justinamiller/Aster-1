# Incremental Compilation

**Status**: Phase 7 ✅  
**Updated**: 2026-02-22

---

## Overview

Aster supports **incremental compilation** via a content-hash–based `CompilationCache`.
When a source file is compiled, the resulting LLVM IR is cached alongside a SHA-256 hash
of the source text.  On subsequent compilations, if the source hash matches, the cached
IR is returned immediately without re-running any pipeline phase.

---

## How It Works

```
source text  ──→  SHA-256 hash  ──→  cache lookup
                                    ├── HIT:  return cached LLVM IR
                                    └── MISS: run full pipeline → store result
```

The cache is keyed by `"<filename>:<sha256-of-source>"`.  If the source changes even by
a single character, the hash changes and the entry is treated as a miss.

---

## Usage

### Programmatic API

```csharp
var cache = new CompilationCache();

// First compile: runs full pipeline, caches result
var driver1 = new CompilationDriver(cache: cache);
var ir1 = driver1.Compile(source, "main.ast");

// Second compile of same source: cache hit, instant return
var driver2 = new CompilationDriver(cache: cache);
var ir2 = driver2.Compile(source, "main.ast");  // retrieved from cache
```

### CLI Flags (planned)

```
aster build --incremental     # enable incremental mode (uses ~/.aster/cache/)
aster build --no-cache        # disable incremental mode
aster clean                   # evict all cached entries
```

---

## Cache Invalidation

The cache is automatically invalidated for a file when:

1. The source text changes (different SHA-256 hash → different key)
2. `CompilationCache.Invalidate(fileName)` is called explicitly
3. `CompilationCache.Clear()` is called

The cache does **not** yet track transitive dependencies (module A depends on module B).
If B changes, A must be invalidated manually.  Dependency tracking is planned for Phase 8.

---

## Implementation Details

| Class / Method                  | Description                                              |
|---------------------------------|----------------------------------------------------------|
| `CompilationCache`              | In-memory SHA-256–keyed cache                            |
| `CompilationCache.TryGet`       | Check cache; returns `true` + `CachedModule` on hit     |
| `CompilationCache.Put`          | Store compiled LLVM IR for a source/filename pair        |
| `CompilationCache.Invalidate`   | Evict all versions of a file                             |
| `CompilationCache.Clear`        | Evict all entries                                        |
| `CachedModule`                  | Record: source hash + LLVM IR + timestamp                |
| `CompilationDriver(cache: ...)`  | Pass a cache to enable incremental mode                  |

---

## Security

SHA-256 is collision-resistant.  An attacker who can substitute source files could
produce a collision to poison the cache, but in practice this requires access to
the source directory, which implies full code-execution capability anyway.
The cache is in-memory only; on-disk persistence is planned for Phase 8.

---

## Planned Enhancements (Phase 8)

- [ ] On-disk cache storage (`~/.aster/cache/`)
- [ ] Dependency graph tracking (module-level granularity)
- [ ] Partial re-compilation (only re-check changed modules)
- [ ] Cache versioning (invalidate on compiler version bump)

---

## References

- `src/Aster.Compiler/Driver/CompilationCache.cs`
- `src/Aster.Compiler/Driver/CompilationDriver.cs`
