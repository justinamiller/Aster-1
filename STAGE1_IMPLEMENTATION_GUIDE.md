# Stage 1 (Phase 1) Implementation Guide

**Date**: 2026-02-17  
**Status**: IMPLEMENTATION GUIDE  
**Estimated Timeline**: 2-3 months for full implementation

## Executive Summary

Stage 1 (Core-0 minimal compiler) has **significant infrastructure** (lexer 85%, parser 90%, AST 100%) but lacks the critical compilation logic to actually compile Aster code. This guide provides a concrete implementation plan.

## Current Status

### What Exists ✅

**Lexer** (850 LOC, 85% complete):
- ✅ Token recognition for all keywords, operators, literals
- ✅ Span tracking (file, line, column, position)
- ✅ String/char/number literal scanning
- ✅ Comment handling
- ✅ Basic error recovery
- ⚠️ Missing: Full UTF-8 support, advanced error recovery

**Parser** (1,581 LOC, 90% complete):
- ✅ Recursive descent structure (57 functions)
- ✅ Expression parsing (binary, unary, literals, calls)
- ✅ Statement parsing (let, return, if, while, for)
- ✅ Declaration parsing (functions, structs, enums, type aliases)
- ✅ Helper functions (peek, advance, check, expect)
- ✅ Error handling framework
- ⚠️ Well-implemented but not integrated into compilation pipeline

**AST Definitions** (284 LOC, 100% complete):
- ✅ All node types defined
- ✅ Expression/statement/declaration enums
- ✅ Complete IR model

**Infrastructure** (590 LOC, 90% complete):
- ✅ Token types and spans
- ✅ Diagnostic bag
- ✅ Parser result types
- ⚠️ Missing: Actual diagnostic reporting

**Total**: ~3,305 LOC with ~2,700 LOC functional infrastructure

### What's Missing ❌

**Type Checking** (~800 LOC, 0% complete):
- ❌ Symbol table construction
- ❌ Scope management
- ❌ Type inference (Hindley-Milner)
- ❌ Constraint generation and solving
- ❌ Error reporting

**Name Resolution** (~500 LOC, 0% complete):
- ❌ Symbol resolution
- ❌ Import/module handling
- ❌ Scope chain management

**IR Generation** (~400 LOC, 0% complete):
- ❌ AST → HIR lowering
- ❌ Expression lowering
- ❌ Statement lowering
- ❌ Declaration lowering

**Code Generation** (~500 LOC, 0% complete):
- ❌ HIR → LLVM IR
- ❌ Function prologues/epilogues
- ❌ Operator lowering
- ❌ Control flow codegen

**CLI Integration** (~100 LOC, 5% complete):
- ⚠️ main() is intentional no-op
- ❌ Command-line argument parsing
- ❌ File I/O integration
- ❌ Output generation

**Total Missing**: ~2,300 LOC of critical compiler logic

## Implementation Plan

### Phase 1A: Type Checking Foundation (Week 1-2, ~800 LOC)

**Goal**: Basic type checking for Core-0 subset

**Components**:

1. **Symbol Table** (~200 LOC):
```aster
struct SymbolTable {
    scopes: Vec<Scope>,
    current_scope: i32
}

struct Scope {
    symbols: Vec<Symbol>,
    parent: i32
}

struct Symbol {
    name: String,
    symbol_type: SymbolType,
    is_mutable: bool
}

enum SymbolType {
    Function(FunctionSymbol),
    Variable(TypeRef),
    Struct(StructSymbol),
    Enum(EnumSymbol)
}

fn new_symbol_table() -> SymbolTable { ... }
fn enter_scope(mut st: SymbolTable) -> SymbolTable { ... }
fn exit_scope(mut st: SymbolTable) -> SymbolTable { ... }
fn declare_symbol(mut st: SymbolTable, name: String, sym_type: SymbolType) -> SymbolTable { ... }
fn lookup_symbol(st: SymbolTable, name: String) -> OptionSymbol { ... }
```

2. **Type Inference** (~400 LOC):
```aster
struct TypeChecker {
    symbol_table: SymbolTable,
    diagnostics: DiagnosticBag,
    current_function: OptionString
}

fn new_type_checker() -> TypeChecker { ... }

// Check declarations
fn check_function(mut tc: TypeChecker, func: FunctionDecl) -> TypeChecker { ... }
fn check_struct(mut tc: TypeChecker, struc: StructDecl) -> TypeChecker { ... }
fn check_enum(mut tc: TypeChecker, enum_decl: EnumDecl) -> TypeChecker { ... }

// Check statements
fn check_statement(mut tc: TypeChecker, stmt: Statement) -> CheckStatementResult { ... }
fn check_let_statement(mut tc: TypeChecker, let_stmt: LetStatement) -> CheckStatementResult { ... }

// Check expressions
fn check_expression(mut tc: TypeChecker, expr: Expression) -> CheckExpressionResult { ... }
fn infer_expression_type(tc: TypeChecker, expr: Expression) -> TypeRef { ... }

// Type unification
fn unify_types(mut tc: TypeChecker, t1: TypeRef, t2: TypeRef) -> UnifyResult { ... }
fn types_compatible(t1: TypeRef, t2: TypeRef) -> bool { ... }
```

3. **Result Types** (~200 LOC):
```aster
struct CheckStatementResult {
    type_checker: TypeChecker,
    statement_type: TypeRef
}

struct CheckExpressionResult {
    type_checker: TypeChecker,
    expression_type: TypeRef
}

struct UnifyResult {
    type_checker: TypeChecker,
    unified_type: TypeRef,
    success: bool
}
```

**Testing**: Create simple test programs with type errors and verify detection.

### Phase 1B: Name Resolution (Week 3, ~500 LOC)

**Goal**: Resolve identifiers to declarations

**Components**:

1. **Name Resolver** (~300 LOC):
```aster
struct NameResolver {
    symbol_table: SymbolTable,
    diagnostics: DiagnosticBag,
    current_module: String
}

fn new_name_resolver() -> NameResolver { ... }

fn resolve_module(mut nr: NameResolver, module: Module) -> ResolveModuleResult { ... }
fn resolve_function(mut nr: NameResolver, func: FunctionDecl) -> ResolveFunctionResult { ... }
fn resolve_statement(mut nr: NameResolver, stmt: Statement) -> ResolveStatementResult { ... }
fn resolve_expression(mut nr: NameResolver, expr: Expression) -> ResolveExpressionResult { ... }
```

2. **Resolution Context** (~200 LOC):
```aster
struct ResolvedSymbol {
    name: String,
    declaration_span: Span,
    symbol_type: SymbolType
}

fn resolve_identifier(nr: NameResolver, name: String, span: Span) -> OptionResolvedSymbol { ... }
fn bind_identifier(mut nr: NameResolver, name: String, symbol: ResolvedSymbol) -> NameResolver { ... }
```

**Testing**: Verify undefined variable errors, scope resolution.

### Phase 1C: IR Generation (Week 4, ~400 LOC)

**Goal**: Lower AST to HIR (High-level Intermediate Representation)

**Components**:

1. **HIR Types** (~100 LOC):
```aster
// High-level IR (simpler than AST, more structured)
struct HirModule {
    functions: Vec<HirFunction>,
    types: Vec<HirType>
}

struct HirFunction {
    name: String,
    params: Vec<HirParameter>,
    return_type: HirType,
    body: Vec<HirStatement>
}

enum HirStatement {
    Let(HirLetStatement),
    Assign(HirAssignStatement),
    Return(HirReturnStatement),
    Expression(HirExpressionStatement)
}

enum HirExpression {
    Literal(HirLiteral),
    Variable(String),
    BinaryOp(HirBinaryOp),
    Call(HirCall)
}
```

2. **Lowering Logic** (~300 LOC):
```aster
struct IrGenerator {
    current_module: HirModule,
    diagnostics: DiagnosticBag
}

fn new_ir_generator() -> IrGenerator { ... }

fn lower_module(mut ig: IrGenerator, module: Module) -> LowerModuleResult { ... }
fn lower_function(mut ig: IrGenerator, func: FunctionDecl) -> LowerFunctionResult { ... }
fn lower_statement(mut ig: IrGenerator, stmt: Statement) -> LowerStatementResult { ... }
fn lower_expression(mut ig: IrGenerator, expr: Expression) -> LowerExpressionResult { ... }
```

**Testing**: Verify HIR structure matches AST semantics.

### Phase 1D: Code Generation (Week 5-6, ~500 LOC)

**Goal**: Generate LLVM IR from HIR

**Components**:

1. **LLVM IR Builder** (~200 LOC):
```aster
struct CodeGenerator {
    output: String,  // LLVM IR as text
    indent_level: i32,
    temp_counter: i32,
    label_counter: i32
}

fn new_code_generator() -> CodeGenerator { ... }

fn emit_line(mut cg: CodeGenerator, line: String) -> CodeGenerator { ... }
fn emit_function_prologue(mut cg: CodeGenerator, func: HirFunction) -> CodeGenerator { ... }
fn emit_function_epilogue(mut cg: CodeGenerator) -> CodeGenerator { ... }
fn new_temp(mut cg: CodeGenerator) -> TempResult { ... }
fn new_label(mut cg: CodeGenerator) -> LabelResult { ... }
```

2. **Code Generation** (~300 LOC):
```aster
fn generate_module(mut cg: CodeGenerator, module: HirModule) -> GenerateResult { ... }
fn generate_function(mut cg: CodeGenerator, func: HirFunction) -> GenerateResult { ... }
fn generate_statement(mut cg: CodeGenerator, stmt: HirStatement) -> GenerateResult { ... }
fn generate_expression(mut cg: CodeGenerator, expr: HirExpression) -> GenerateExprResult { ... }

// Specific codegen for operators
fn generate_binary_op(mut cg: CodeGenerator, op: BinaryOp, left: String, right: String, result_type: HirType) -> GenerateExprResult { ... }
fn generate_call(mut cg: CodeGenerator, call: HirCall) -> GenerateExprResult { ... }
```

**Testing**: Verify LLVM IR syntax, compile with llc/clang.

### Phase 1E: Integration & CLI (Week 7-8, ~100 LOC)

**Goal**: Wire everything together and add CLI

**Components**:

1. **Main Pipeline** (~50 LOC in main.ast):
```aster
fn compile_file(path: String, output_path: String) -> CompilationResult {
    // 1. Read file
    let source = read_file(path);
    
    // 2. Lex
    let compiler = new_compiler();
    let lex_result = lex_source(compiler, path, source);
    let tokens = lex_result.tokens;
    compiler = lex_result.compiler;
    
    // 3. Parse
    let parse_result = parse_source(compiler, tokens);
    let module = parse_result.parsed_module;
    compiler = parse_result.compiler;
    
    // 4. Name resolution
    let nr = new_name_resolver();
    let resolve_result = resolve_module(nr, module);
    
    // 5. Type check
    let tc = new_type_checker();
    let check_result = check_module(tc, module);
    
    // 6. IR generation
    let ig = new_ir_generator();
    let lower_result = lower_module(ig, module);
    let hir_module = lower_result.hir_module;
    
    // 7. Code generation
    let cg = new_code_generator();
    let gen_result = generate_module(cg, hir_module);
    let llvm_ir = gen_result.output;
    
    // 8. Write output
    write_file(output_path, llvm_ir);
    
    CompilationResult {
        success: true,
        parsed_module: OptionModule::SomeModule(module),
        diagnostics: compiler.diagnostics
    }
}
```

2. **CLI Argument Parsing** (~50 LOC):
```aster
// Note: Core-0 doesn't have command-line args
// Will need to use extern declarations to C# runtime
extern fn get_arg(index: i32) -> String;
extern fn get_arg_count() -> i32;

fn main() {
    let arg_count = get_arg_count();
    
    if arg_count < 2 {
        print_usage();
        return;
    }
    
    let command = get_arg(1);
    
    // Match on command
    if command == "build" {
        if arg_count < 3 {
            print_error("build requires input file");
            return;
        }
        let input_file = get_arg(2);
        let output_file = if arg_count >= 4 { get_arg(3) } else { "output.ll" };
        
        let result = compile_file(input_file, output_file);
        
        if result.success {
            print_success();
        } else {
            print_errors(result.diagnostics);
        }
    } else {
        print_usage();
    }
}
```

**Testing**: Compile simple Core-0 programs end-to-end.

## Critical Path Dependencies

```
Lexer (✅ Done) 
  ↓
Parser (✅ Done)
  ↓
Name Resolution (❌ Needed) ← BLOCKER
  ↓
Type Checking (❌ Needed) ← BLOCKER
  ↓
IR Generation (❌ Needed) ← BLOCKER
  ↓
Code Generation (❌ Needed) ← BLOCKER
  ↓
CLI Integration (❌ Needed) ← BLOCKER
```

**All components must be implemented sequentially.**

## Testing Strategy

### Unit Tests (Per Component)

Create test files for each phase:

**Type Checking Tests**:
```aster
// test_typecheck_simple.ast
fn test_basic_types() -> i32 {
    let x: i32 = 42;
    let y: bool = true;
    x
}

// Expected: Type checks successfully
```

```aster
// test_typecheck_error.ast
fn test_type_error() -> i32 {
    let x: i32 = true;  // Type error: bool assigned to i32
    x
}

// Expected: Type error reported
```

**End-to-End Tests**:
```aster
// test_simple_program.ast
fn factorial(n: i32) -> i32 {
    if n <= 1 {
        1
    } else {
        n * factorial(n - 1)
    }
}

fn main() -> i32 {
    factorial(5)
}

// Expected: Compiles to LLVM IR, produces 120
```

### Integration Testing

Use Stage 0 (C#) as oracle:
```bash
# Compile with Stage 0
dotnet run --project src/Aster.CLI -- build test.ast -o test_stage0.ll

# Compile with Stage 1 (when ready)
./build/bootstrap/stage1/aster1 build test.ast -o test_stage1.ll

# Compare outputs (should be similar, not necessarily identical)
diff test_stage0.ll test_stage1.ll
```

## Success Criteria

### Minimal (MVP)

✅ Stage 1 compiles this program:
```aster
fn main() -> i32 {
    42
}
```

Output: Valid LLVM IR that returns 42.

### Basic (Phase 1 Complete)

✅ Stage 1 compiles Core-0 programs with:
- Functions with parameters
- Integer/boolean/string literals
- Binary operations (+, -, *, /, ==, !=, <, >, etc.)
- If/else statements
- Simple structs
- Let bindings

### Ideal (Stage 1 Complete)

✅ Stage 1 compiles Core-0 programs with:
- Everything in Basic +
- Enums (simple, no complex pattern matching)
- While loops
- Function calls
- Return statements
- Type aliases

### Full (Bootstrap Ready)

✅ Stage 1 can compile Stage 2 source files
✅ Stage 2 binary is produced and functional

## Timeline Estimates

| Phase | Component | LOC | Time | Dependencies |
|-------|-----------|-----|------|--------------|
| 1A | Type Checking | ~800 | 2 weeks | Parser (done) |
| 1B | Name Resolution | ~500 | 1 week | Type Checking |
| 1C | IR Generation | ~400 | 1 week | Name Resolution |
| 1D | Code Generation | ~500 | 2 weeks | IR Generation |
| 1E | Integration | ~100 | 1 week | All above |
| | **Total** | **~2,300** | **7-8 weeks** | Sequential |

**With Buffer**: 10-12 weeks (2.5-3 months) for one experienced engineer

**With Team**: Could parallelize some work, maybe 6-8 weeks

## Current Blockers

1. **Core-0 Language Limitations**:
   - No tuple destructuring
   - No closures
   - Limited pattern matching
   - Makes some code awkward to write

2. **Bootstrap Circular Dependency Risk**:
   - If Stage 1 actually compiles, could cause issues during bootstrap
   - Need careful testing to avoid infinite loops

3. **No Command-Line Args**:
   - Core-0 doesn't support CLI args natively
   - Need extern declarations to C# runtime
   - Makes CLI interface awkward

4. **File I/O Limitations**:
   - Core-0 file I/O is limited
   - Need extern declarations for read/write
   - Error handling is basic

## Getting Started

### For Implementers

1. **Start with Type Checking**:
   - Create `aster/compiler/typecheck.ast`
   - Implement SymbolTable and TypeChecker structs
   - Add check_function, check_statement, check_expression functions

2. **Test Incrementally**:
   - Create small test programs
   - Compile with Stage 0 to verify syntax
   - Add type checking logic piece by piece

3. **Follow the Plan**:
   - Don't skip ahead
   - Each phase builds on previous
   - Test thoroughly before moving on

### For Reviewers

1. **Check Completeness**:
   - Does it handle all Core-0 constructs?
   - Are error messages clear?
   - Are edge cases handled?

2. **Verify Testing**:
   - Are there unit tests?
   - Are there integration tests?
   - Does it match Stage 0 behavior?

3. **Review Performance**:
   - Is compilation reasonably fast?
   - Are there obvious inefficiencies?

## Conclusion

**Phase 1 (Stage 1) Completion Status**:
- Infrastructure: ✅ 90% complete (~2,700 LOC)
- Compilation Logic: ❌ 0% complete (~2,300 LOC needed)
- Timeline: 7-8 weeks minimum, 10-12 weeks realistic

**Recommendation**:
1. Implement type checking first (highest priority)
2. Follow sequential dependencies
3. Test thoroughly at each stage
4. Use Stage 0 as oracle for validation

**NOT Recommended**:
- Skipping type checking
- Partial implementations
- Shortcuts that break later

---

**Ready to implement?** Start with `aster/compiler/typecheck.ast` and follow Phase 1A!
