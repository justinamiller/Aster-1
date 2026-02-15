# Aster Compiler - Changelog

## [Unreleased]

### Added
- Stage1 build mode with --stage1 flag for Core-0 language subset enforcement
- Complete bootstrap infrastructure (scripts, documentation, CI)
- IR normalization tool for differential testing
- Self-hosting test program
- One-command smoke test
- Comprehensive language reference documentation

### Changed
- Enhanced CLI with --stage1 flag support

## [0.2.0] - 2026-02-15

### Added
- Complete Stage1 self-hosting infrastructure
- Bootstrap pipeline for Unix/Windows
- Stage1 compiler component skeletons (8 .ast files)
- Comprehensive documentation (STAGE1_SCOPE, OVERVIEW, STATUS)
- Language specification (grammar, types, ownership, memory)
- Crash-only compiler principle documentation
- GitHub Actions bootstrap workflow
- IR normalization tool

### Infrastructure
- 36 new files created (~90,000+ lines)
- Complete build and test automation
- Security hardening (CodeQL scans)
- Deterministic data structures

## [0.1.0] - 2026-01-01

### Added
- Initial C# seed compiler (aster0)
- Complete frontend (lexer, parser, AST, HIR)
- Type system with Hindley-Milner inference
- Effect system tracking
- MIR (SSA-based intermediate representation)
- Borrow checker with NLL analysis
- LLVM backend
- 12-layer standard library implementation
- 119 comprehensive tests
- Fuzzing infrastructure
- Differential testing framework
- Language server protocol (LSP) support
- Formatter, linter, documentation generator
- Package manager
- Telemetry and observability

### Features
- Full compilation pipeline working
- Incremental compilation with caching
- Parallel compilation with work-stealing
- Optimization passes (DCE, CSE, inlining, SROA, etc.)
- Complete stdlib with effect annotations

---

**Format**: Based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)  
**Versioning**: [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
