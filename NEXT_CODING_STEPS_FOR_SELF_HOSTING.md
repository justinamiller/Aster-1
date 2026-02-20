# Next Coding Steps for Aster True Self-Hosting

**Date**: 2026-02-18  
**Status**: Analysis Complete - Ready for Implementation  
**Estimated Timeline**: 12-18 months for full self-hosting

---

## Executive Summary

This document provides a comprehensive, actionable roadmap for achieving **true self-hosting** of the Aster compiler. The analysis shows that while significant infrastructure exists, **approximately 6,000-11,000 lines of Aster code** need to be written to complete the compiler pipeline.

### Current State
- ✅ **Stage 0 (C#)**: Fully functional production compiler with 119 passing tests
- 🚧 **Bootstrap Stages 1-3**: Infrastructure complete but only stub implementations exist
- ❌ **Self-Hosting**: Not achieved - Stage 3 cannot compile itself

### What's Been Done
1. **Lexer**: ~85% complete (~850 LOC in `aster/compiler/frontend/lexer.ast`)
2. **Parser**: ~90% complete (~1,581 LOC in `aster/compiler/frontend/parser.ast`)
3. **AST Definitions**: 100% complete (~284 LOC in `aster/compiler/ir/ast.ast`)
4. **Skeleton Infrastructure**: All stages have stub files with type definitions

### What Needs to Be Done
The critical path requires implementing these components in Aster:
1. **Type Checker** (~800 LOC) - Currently only 101 LOC stub
2. **Name Resolution** (~500 LOC) - Currently only 52 LOC stub
3. **IR Generation** (~400 LOC) - Currently only 81 LOC stub
4. **Code Generation** (~500 LOC) - Currently only 70 LOC stub
5. **Lexer Completion** (~150 LOC) - Finish remaining 15%
6. **Parser Integration** (~100 LOC) - Connect to downstream stages
7. **CLI & I/O** (~100 LOC) - File reading/writing, command-line handling

**Total Critical Path**: ~2,630 LOC minimum for Stage 1

---

## Part 1: Immediate Next Steps (Stage 1 Completion)

### Priority 1: Complete the Lexer (1 week)

**File**: `aster/compiler/frontend/lexer.ast`  
**Current**: 850 LOC, 85% complete  
**Remaining**: ~150 LOC

#### Missing Features
1. **Character Literals** (`'a'`, `'\n'`, `'\''`)
   ```rust
   fn lex_char_literal(lexer: Lexer) -> Token {
       // Consume opening '
       // Handle escape sequences: \n, \r, \t, \\, \'
       // Handle unicode: \u{1F600}
       // Consume closing '
       // Return CharLiteral token
   }
   ```

2. **Raw String Literals** (`r"no\nescape"`, `r#"can contain ""#`)
   ```rust
   fn lex_raw_string_literal(lexer: Lexer) -> Token {
       // Count opening hashes (r###"...")
       // Consume until matching closing ("###)
       // No escape processing
       // Return StringLiteral token
   }
   ```

3. **Numeric Literal Variants**
   - Hexadecimal: `0x2A`, `0xFF`
   - Binary: `0b1010`, `0b11111111`
   - Octal: `0o52`, `0o377`
   - Floating point: `1.0`, `1.5e10`, `1.5E-10`
   - Type suffixes: `42i64`, `1.0f32`
   
   ```rust
   fn lex_hex_literal(lexer: Lexer) -> Token {
       // After seeing '0x', consume hex digits [0-9a-fA-F]
       // Handle underscores for readability: 0xFF_FF
       // Check for type suffix: i32, i64, u32, u64
       // Return IntegerLiteral token with value
   }
   ```

4. **Comment Handling** (currently missing)
   ```rust
   fn lex_line_comment(lexer: Lexer) -> Token {
       // After seeing '//', consume until '\n'
       // Return Comment token (or skip if not preserving)
   }
   
   fn lex_block_comment(lexer: Lexer) -> Token {
       // After seeing '/*', consume until '*/'
       // Handle nested: /* outer /* inner */ outer */
       // Track nesting depth
       // Return Comment token (or skip if not preserving)
   }
   ```

5. **Lifetime Tokens** (`'a`, `'static`, `'_`)
   ```rust
   fn lex_lifetime(lexer: Lexer) -> Token {
       // After seeing ' followed by identifier
       // Must not be CharLiteral context
       // Return Lifetime token
   }
   ```

6. **Unicode Handling** (UTF-8 validation)
   ```rust
   fn is_valid_utf8(lexer: Lexer) -> bool {
       // Validate UTF-8 byte sequences
       // Handle 1-4 byte characters properly
       // Reject invalid sequences
   }
   ```

#### Testing Strategy
```bash
# Create test file for each feature
cat > tests/lexer/char_literals.ast << 'EOF'
let c1 = 'a';
let c2 = '\n';
let c3 = '\'';
let c4 = '\u{1F600}';
EOF

# Run lexer on test files
dotnet run --project src/Aster.CLI emit-tokens tests/lexer/char_literals.ast
```

---

### Priority 2: Implement Name Resolution (2 weeks)

**File**: `aster/compiler/resolve.ast`  
**Current**: 52 LOC stub  
**Target**: ~500 LOC

#### Core Functionality Needed

1. **Scope Management**
   ```rust
   struct Scope {
       parent: Option<Box<Scope>>,
       bindings: Vec<Binding>,
       binding_count: i32
   }
   
   struct Binding {
       name: String,
       binding_kind: BindingKind,
       defined_at: Span
   }
   
   enum BindingKind {
       Variable,
       Function,
       Struct,
       Enum,
       EnumVariant,
       TypeParameter
   }
   
   fn enter_scope(resolver: NameResolver) -> NameResolver {
       // Create new scope with current as parent
       // Push onto scope stack
   }
   
   fn exit_scope(resolver: NameResolver) -> NameResolver {
       // Pop scope from stack
       // Return to parent
   }
   
   fn define_in_scope(resolver: NameResolver, name: String, kind: BindingKind) -> NameResolver {
       // Add binding to current scope
       // Check for duplicate definitions
       // Report error if duplicate
   }
   ```

2. **Name Lookup**
   ```rust
   fn lookup_name(resolver: NameResolver, name: String) -> LookupResult {
       // Search current scope
       // If not found, search parent scopes
       // If still not found, check imports
       // Return resolution with definition site
   }
   
   struct LookupResult {
       found: bool,
       binding: Option<Binding>,
       scope_depth: i32
   }
   ```

3. **Module Resolution**
   ```rust
   fn resolve_import(resolver: NameResolver, import: Import) -> NameResolver {
       // Parse module path (e.g., "std::io::println")
       // Load module from filesystem or cache
       // Add imported names to current scope
       // Handle visibility (pub vs private)
   }
   
   fn resolve_module_path(resolver: NameResolver, path: Vec<String>) -> Module {
       // Navigate module tree
       // Search for .ast files
       // Return module definition
   }
   ```

4. **Declaration Processing**
   ```rust
   fn resolve_function_decl(resolver: NameResolver, func: Function) -> NameResolver {
       // Define function name in current scope
       // Enter function scope
       // Define parameters in function scope
       // Resolve type annotations
       // Resolve body expressions
       // Exit function scope
   }
   
   fn resolve_struct_decl(resolver: NameResolver, struct_item: Struct) -> NameResolver {
       // Define struct name in current scope
       // Enter struct scope
       // Resolve field types
       // Exit struct scope
   }
   ```

5. **Path Resolution** (for qualified names like `Vec::new`)
   ```rust
   fn resolve_path(resolver: NameResolver, path: Path) -> PathResolution {
       // First segment: lookup in scope or module
       // Subsequent segments: lookup in namespace of previous
       // Handle type vs value namespaces
       // Return fully resolved path
   }
   
   struct PathResolution {
       segments: Vec<ResolvedSegment>,
       final_binding: Binding
   }
   ```

#### Implementation Steps
```
Week 1:
  Day 1-2: Scope management (enter/exit scope, define binding)
  Day 3-4: Name lookup (search scopes, report undefined)
  Day 5: Testing with simple functions

Week 2:
  Day 1-2: Module imports and resolution
  Day 3: Struct and enum declarations
  Day 4: Path resolution (qualified names)
  Day 5: Integration testing with parser output
```

---

### Priority 3: Implement Type Checker (2-3 weeks)

**File**: `aster/compiler/typecheck.ast`  
**Current**: 101 LOC stub  
**Target**: ~800 LOC

#### Core Type System

1. **Type Representation**
   ```rust
   enum Type {
       Primitive(PrimitiveType),
       Struct(StructType),
       Enum(EnumType),
       Function(FunctionType),
       Generic(GenericType),
       Array(Box<Type>, i32),
       Pointer(Box<Type>),
       Reference(Box<Type>, Mutability),
       Unknown,
       Error
   }
   
   enum PrimitiveType {
       I32, I64, F32, F64, Bool, Char, String, Void
   }
   
   struct StructType {
       name: String,
       fields: Vec<Field>,
       type_params: Vec<TypeParameter>
   }
   
   struct FunctionType {
       params: Vec<Type>,
       return_type: Box<Type>
   }
   ```

2. **Type Inference (Hindley-Milner)**
   ```rust
   struct TypeChecker {
       type_env: TypeEnvironment,
       constraints: Vec<TypeConstraint>,
       substitutions: Vec<Substitution>,
       next_type_var: i32
   }
   
   fn fresh_type_var(checker: TypeChecker) -> (TypeChecker, Type) {
       // Generate new type variable T0, T1, T2, ...
       // Return updated checker and variable
   }
   
   fn infer_expr(checker: TypeChecker, expr: Expression) -> (TypeChecker, Type) {
       match expr {
           Expression::Literal(lit) => infer_literal(checker, lit),
           Expression::Variable(name) => lookup_type(checker, name),
           Expression::BinaryOp(op, left, right) => infer_binary_op(checker, op, left, right),
           Expression::FunctionCall(func, args) => infer_call(checker, func, args),
           Expression::If(cond, then_br, else_br) => infer_if(checker, cond, then_br, else_br),
           // ... handle all expression types
       }
   }
   ```

3. **Constraint Generation**
   ```rust
   struct TypeConstraint {
       left: Type,
       right: Type,
       reason: String,
       span: Span
   }
   
   fn generate_constraint(checker: TypeChecker, t1: Type, t2: Type, reason: String) -> TypeChecker {
       // Add constraint t1 == t2
       // Used later during unification
   }
   
   fn infer_binary_op(checker: TypeChecker, op: BinaryOp, left: Expr, right: Expr) -> (TypeChecker, Type) {
       // Infer type of left
       let (checker, left_type) = infer_expr(checker, left);
       // Infer type of right
       let (checker, right_type) = infer_expr(checker, right);
       
       // Generate constraints based on operator
       match op {
           BinaryOp::Add | BinaryOp::Sub | BinaryOp::Mul | BinaryOp::Div => {
               // left_type == right_type == result_type
               // Must be numeric type
               checker = generate_constraint(checker, left_type, right_type, "operands must match");
               (checker, left_type)
           },
           BinaryOp::Eq | BinaryOp::Ne => {
               // left_type == right_type
               // result_type == Bool
               checker = generate_constraint(checker, left_type, right_type, "comparison operands");
               (checker, Type::Primitive(PrimitiveType::Bool))
           },
           // ... handle all operators
       }
   }
   ```

4. **Unification**
   ```rust
   fn unify(checker: TypeChecker, t1: Type, t2: Type) -> Result<TypeChecker, TypeError> {
       match (t1, t2) {
           // Same primitive types unify
           (Type::Primitive(p1), Type::Primitive(p2)) if p1 == p2 => Ok(checker),
           
           // Type variable unifies with anything (create substitution)
           (Type::Unknown, t) | (t, Type::Unknown) => {
               checker = add_substitution(checker, Type::Unknown, t);
               Ok(checker)
           },
           
           // Structs unify if same name and type params unify
           (Type::Struct(s1), Type::Struct(s2)) if s1.name == s2.name => {
               // Recursively unify type parameters
               unify_type_params(checker, s1.type_params, s2.type_params)
           },
           
           // Functions unify if params and return type unify
           (Type::Function(f1), Type::Function(f2)) => {
               checker = unify_params(checker, f1.params, f2.params)?;
               unify(checker, f1.return_type, f2.return_type)
           },
           
           // Otherwise, types don't unify
           _ => Err(TypeError {
               message: "type mismatch",
               expected: t1,
               actual: t2,
               span: span
           })
       }
   }
   
   fn solve_constraints(checker: TypeChecker) -> Result<TypeChecker, Vec<TypeError>> {
       // Process all constraints
       // Apply unification to each
       // Build substitution map
       // Apply substitutions to all types
   }
   ```

5. **Type Checking Declarations**
   ```rust
   fn check_function(checker: TypeChecker, func: Function) -> TypeChecker {
       // Add parameters to type environment
       // Infer body expression type
       // Check against declared return type
       // Generate constraints
       
       let (checker, body_type) = infer_expr(checker, func.body);
       checker = generate_constraint(checker, body_type, func.return_type, "return type");
       checker
   }
   
   fn check_struct(checker: TypeChecker, struct_item: Struct) -> TypeChecker {
       // Check field types are valid
       // Check for circular definitions
       // Add struct to type environment
   }
   ```

#### Implementation Timeline
```
Week 1:
  Day 1-2: Type representation, primitive types
  Day 3-4: Type inference for literals, variables, binary ops
  Day 5: Type environment and lookup

Week 2:
  Day 1-2: Constraint generation
  Day 3-4: Unification algorithm
  Day 5: Substitution and solving

Week 3:
  Day 1-2: Function type checking
  Day 3: Struct and enum type checking
  Day 4-5: Error reporting and testing
```

---

### Priority 4: Implement IR Generation (2 weeks)

**File**: `aster/compiler/irgen.ast`  
**Current**: 81 LOC stub  
**Target**: ~400 LOC

#### IR Design

1. **HIR (High-level IR) - Close to AST**
   ```rust
   struct HirModule {
       functions: Vec<HirFunction>,
       structs: Vec<HirStruct>,
       enums: Vec<HirEnum>
   }
   
   struct HirFunction {
       name: String,
       params: Vec<HirParam>,
       return_type: Type,
       body: HirBlock,
       local_vars: Vec<HirLocal>
   }
   
   struct HirBlock {
       stmts: Vec<HirStatement>,
       expr: Option<HirExpression>
   }
   
   enum HirStatement {
       Let(HirLocal, Option<HirExpression>),
       Assign(HirPlace, HirExpression),
       ExprStmt(HirExpression)
   }
   
   enum HirExpression {
       Literal(HirLiteral),
       Variable(String, Type),
       BinaryOp(HirBinaryOp, Box<HirExpression>, Box<HirExpression>, Type),
       Call(String, Vec<HirExpression>, Type),
       If(Box<HirExpression>, HirBlock, Option<HirBlock>, Type),
       Block(HirBlock, Type),
       Return(Option<Box<HirExpression>>)
   }
   ```

2. **AST → HIR Lowering**
   ```rust
   fn lower_module(ast: Module) -> HirModule {
       let mut hir_functions = Vec::new();
       
       for decl in ast.declarations {
           match decl {
               Declaration::Function(func) => {
                   let hir_func = lower_function(func);
                   hir_functions.push(hir_func);
               },
               // Handle other declarations
           }
       }
       
       HirModule { functions: hir_functions, ... }
   }
   
   fn lower_function(func: Function) -> HirFunction {
       // Lower parameters
       let hir_params = func.params.map(|p| lower_param(p));
       
       // Lower body
       let hir_body = lower_expr_to_block(func.body);
       
       HirFunction {
           name: func.name,
           params: hir_params,
           return_type: func.return_type,
           body: hir_body,
           local_vars: collect_locals(hir_body)
       }
   }
   
   fn lower_expr(expr: Expression) -> HirExpression {
       match expr {
           Expression::Literal(lit) => HirExpression::Literal(lower_literal(lit)),
           Expression::Variable(name) => {
               // Lookup type from type checker
               let var_type = lookup_type(name);
               HirExpression::Variable(name, var_type)
           },
           Expression::BinaryOp(op, left, right) => {
               let hir_left = lower_expr(*left);
               let hir_right = lower_expr(*right);
               let result_type = compute_result_type(op, hir_left.type(), hir_right.type());
               HirExpression::BinaryOp(op, Box::new(hir_left), Box::new(hir_right), result_type)
           },
           Expression::If(cond, then_br, else_br) => {
               let hir_cond = lower_expr(*cond);
               let hir_then = lower_expr(*then_br);
               let hir_else = else_br.map(|e| lower_expr(*e));
               HirExpression::If(Box::new(hir_cond), hir_then, hir_else, ...)
           },
           // ... handle all expression types
       }
   }
   ```

3. **Control Flow Graph (Optional for Stage 1, needed for Stage 3)**
   ```rust
   struct BasicBlock {
       id: i32,
       statements: Vec<HirStatement>,
       terminator: Terminator
   }
   
   enum Terminator {
       Return(Option<HirExpression>),
       Goto(i32),  // target block id
       Branch(HirExpression, i32, i32),  // condition, true_block, false_block
       Unreachable
   }
   
   fn build_cfg(func: HirFunction) -> Vec<BasicBlock> {
       // Split function into basic blocks
       // Connect blocks with control flow edges
       // Used for analysis and optimization
   }
   ```

#### Implementation Steps
```
Week 1:
  Day 1-2: HIR data structures
  Day 3-4: Lower simple expressions (literals, vars, binary ops)
  Day 5: Lower function declarations

Week 2:
  Day 1-2: Lower control flow (if, while, loop)
  Day 3: Lower function calls
  Day 4: Collect local variables
  Day 5: Testing and validation
```

---

### Priority 5: Implement Code Generation (2 weeks)

**File**: `aster/compiler/codegen.ast`  
**Current**: 70 LOC stub  
**Target**: ~500 LOC

#### LLVM IR Generation

1. **Module and Function Generation**
   ```rust
   fn generate_module(hir: HirModule) -> String {
       let mut output = String::new();
       
       // Module header
       output += "; ModuleID = 'aster_module'\n";
       output += "target datalayout = \"e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128\"\n";
       output += "target triple = \"x86_64-pc-linux-gnu\"\n\n";
       
       // External declarations (libc functions)
       output += "declare i32 @puts(i8*)\n";
       output += "declare i8* @malloc(i64)\n";
       output += "declare void @free(i8*)\n";
       output += "declare void @exit(i32)\n\n";
       
       // Generate each function
       for func in hir.functions {
           output += generate_function(func);
           output += "\n";
       }
       
       output
   }
   
   fn generate_function(func: HirFunction) -> String {
       let mut output = String::new();
       let mut ctx = CodeGenContext::new();
       
       // Function signature
       let ret_type = llvm_type(func.return_type);
       output += "define " + ret_type + " @" + func.name + "(";
       
       // Parameters
       let params = func.params.map(|p| {
           let param_type = llvm_type(p.param_type);
           let param_name = fresh_var(ctx);
           ctx = bind_var(ctx, p.name, param_name);
           param_type + " " + param_name
       });
       output += params.join(", ");
       output += ") {\n";
       
       // Entry block
       output += "entry:\n";
       
       // Allocate locals
       for local in func.local_vars {
           let local_name = fresh_var(ctx);
           ctx = bind_var(ctx, local.name, local_name);
           let local_type = llvm_type(local.var_type);
           output += "  " + local_name + " = alloca " + local_type + "\n";
       }
       
       // Generate body
       let (ctx, body_val) = generate_block(ctx, func.body);
       
       // Return
       if func.return_type != Type::Void {
           output += "  ret " + ret_type + " " + body_val + "\n";
       } else {
           output += "  ret void\n";
       }
       
       output += "}\n";
       output
   }
   ```

2. **Expression Code Generation**
   ```rust
   struct CodeGenContext {
       var_map: Vec<(String, String)>,  // source_name -> llvm_name
       next_temp: i32,
       next_label: i32
   }
   
   fn generate_expr(ctx: CodeGenContext, expr: HirExpression) -> (CodeGenContext, String, String) {
       // Returns (updated_context, llvm_instructions, result_value)
       
       match expr {
           HirExpression::Literal(lit) => {
               // Literals don't need instructions, just return the value
               (ctx, "", literal_value(lit))
           },
           
           HirExpression::Variable(name, var_type) => {
               // Load from variable
               let var_ptr = lookup_var(ctx, name);
               let temp = fresh_temp(ctx);
               let ty = llvm_type(var_type);
               let instr = "  " + temp + " = load " + ty + ", " + ty + "* " + var_ptr + "\n";
               (ctx, instr, temp)
           },
           
           HirExpression::BinaryOp(op, left, right, result_type) => {
               // Generate left side
               let (ctx, left_instrs, left_val) = generate_expr(ctx, *left);
               
               // Generate right side
               let (ctx, right_instrs, right_val) = generate_expr(ctx, *right);
               
               // Generate operation
               let temp = fresh_temp(ctx);
               let ty = llvm_type(result_type);
               let op_instr = match op {
                   BinaryOp::Add => "add",
                   BinaryOp::Sub => "sub",
                   BinaryOp::Mul => "mul",
                   BinaryOp::Div => "sdiv",
                   BinaryOp::Eq => "icmp eq",
                   // ... all operators
               };
               let instr = left_instrs + right_instrs + 
                          "  " + temp + " = " + op_instr + " " + ty + " " + 
                          left_val + ", " + right_val + "\n";
               
               (ctx, instr, temp)
           },
           
           HirExpression::Call(func_name, args, return_type) => {
               // Generate arguments
               let mut all_instrs = "";
               let mut arg_vals = Vec::new();
               for arg in args {
                   let (new_ctx, instrs, val) = generate_expr(ctx, arg);
                   ctx = new_ctx;
                   all_instrs += instrs;
                   arg_vals.push(val);
               }
               
               // Generate call
               let temp = fresh_temp(ctx);
               let ret_ty = llvm_type(return_type);
               let args_str = args.zip(arg_vals).map(|(arg, val)| {
                   llvm_type(arg.type()) + " " + val
               }).join(", ");
               
               let call_instr = "  " + temp + " = call " + ret_ty + " @" + 
                               func_name + "(" + args_str + ")\n";
               
               (ctx, all_instrs + call_instr, temp)
           },
           
           HirExpression::If(cond, then_block, else_block, result_type) => {
               // Generate condition
               let (ctx, cond_instrs, cond_val) = generate_expr(ctx, *cond);
               
               // Create labels
               let then_label = fresh_label(ctx);
               let else_label = fresh_label(ctx);
               let merge_label = fresh_label(ctx);
               
               // Branch instruction
               let branch_instr = cond_instrs + 
                   "  br i1 " + cond_val + ", label %" + then_label + 
                   ", label %" + else_label + "\n";
               
               // Then block
               let then_instrs = then_label + ":\n";
               let (ctx, then_body, then_val) = generate_block(ctx, then_block);
               let then_instrs = then_instrs + then_body + 
                   "  br label %" + merge_label + "\n";
               
               // Else block
               let else_instrs = else_label + ":\n";
               let (ctx, else_body, else_val) = if else_block.is_some() {
                   generate_block(ctx, else_block.unwrap())
               } else {
                   (ctx, "", "0")  // default value
               };
               let else_instrs = else_instrs + else_body + 
                   "  br label %" + merge_label + "\n";
               
               // Merge block with phi
               let merge_instrs = merge_label + ":\n";
               let result_temp = fresh_temp(ctx);
               let ty = llvm_type(result_type);
               let phi_instr = "  " + result_temp + " = phi " + ty + 
                   " [ " + then_val + ", %" + then_label + " ], " +
                   " [ " + else_val + ", %" + else_label + " ]\n";
               
               let all_instrs = branch_instr + then_instrs + else_instrs + 
                               merge_instrs + phi_instr;
               
               (ctx, all_instrs, result_temp)
           },
           
           // ... handle other expression types
       }
   }
   ```

3. **Type Mapping**
   ```rust
   fn llvm_type(ty: Type) -> String {
       match ty {
           Type::Primitive(PrimitiveType::I32) => "i32",
           Type::Primitive(PrimitiveType::I64) => "i64",
           Type::Primitive(PrimitiveType::F32) => "float",
           Type::Primitive(PrimitiveType::F64) => "double",
           Type::Primitive(PrimitiveType::Bool) => "i1",
           Type::Primitive(PrimitiveType::Char) => "i8",
           Type::Primitive(PrimitiveType::String) => "i8*",  // pointer to char array
           Type::Primitive(PrimitiveType::Void) => "void",
           Type::Pointer(inner) => llvm_type(*inner) + "*",
           Type::Reference(inner, _) => llvm_type(*inner) + "*",
           Type::Struct(struct_ty) => "%struct." + struct_ty.name,
           // ... handle all types
       }
   }
   ```

#### Implementation Timeline
```
Week 1:
  Day 1-2: Module structure, function signatures
  Day 3-4: Simple expressions (literals, vars, binary ops)
  Day 5: Load/store variables

Week 2:
  Day 1-2: Function calls
  Day 3-4: Control flow (if/else, loops)
  Day 5: Testing and LLVM IR validation
```

---

### Priority 6: CLI and I/O Integration (1 week)

**File**: `aster/compiler/io.ast`, `aster/compiler/main.ast`  
**Target**: ~100 LOC

#### File I/O
```rust
// External C functions (provided by runtime)
extern fn aster_read_file(path: *i8) -> *i8;
extern fn aster_write_file(path: *i8, content: *i8) -> i32;
extern fn aster_exit(code: i32) -> void;

fn read_source_file(path: String) -> String {
    // Call external function
    // Convert C string to Aster String
    // Handle errors (file not found)
}

fn write_output_file(path: String, content: String) -> bool {
    // Convert Aster String to C string
    // Call external function
    // Return success/failure
}
```

#### Command-Line Parsing
```rust
fn parse_args(args: Vec<String>) -> CompilerOptions {
    let mut options = default_options();
    
    let mut i = 1;  // Skip program name
    while i < args.len() {
        let arg = args[i];
        
        if arg == "build" {
            options.mode = Mode::Build;
        } else if arg == "check" {
            options.mode = Mode::Check;
        } else if arg == "emit-tokens" {
            options.mode = Mode::EmitTokens;
        } else if arg == "emit-ast-json" {
            options.mode = Mode::EmitAst;
        } else if arg == "-o" || arg == "--output" {
            i = i + 1;
            options.output_file = args[i];
        } else if arg.starts_with("-") {
            // Unknown flag
            println("Unknown option: " + arg);
            print_usage();
            exit(1);
        } else {
            // Input file
            options.input_file = arg;
        }
        
        i = i + 1;
    }
    
    options
}

fn print_usage() {
    println("Aster Compiler - Stage 1");
    println("");
    println("Usage: aster1 <command> <input-file> [options]");
    println("");
    println("Commands:");
    println("  build              Compile to LLVM IR");
    println("  check              Type-check only");
    println("  emit-tokens        Output token stream");
    println("  emit-ast-json      Output AST as JSON");
    println("");
    println("Options:");
    println("  -o, --output FILE  Specify output file");
    println("  -h, --help         Show this help");
}
```

#### Main Entry Point
```rust
fn main(args: Vec<String>) -> i32 {
    // Parse command-line arguments
    let options = parse_args(args);
    
    // Read source file
    let source = read_source_file(options.input_file);
    if source.is_empty() {
        println("Error: Could not read file: " + options.input_file);
        return 1;
    }
    
    // Lex
    let tokens = lex(source, options.input_file);
    
    // Parse
    let ast = parse(tokens);
    if ast.has_errors {
        print_errors(ast.errors);
        return 1;
    }
    
    // Handle emit modes
    if options.mode == Mode::EmitTokens {
        let json = serialize_tokens(tokens);
        write_output_file(options.output_file, json);
        return 0;
    }
    
    if options.mode == Mode::EmitAst {
        let json = serialize_ast(ast);
        write_output_file(options.output_file, json);
        return 0;
    }
    
    // Resolve names
    let resolved = resolve_names(ast);
    if resolved.has_errors {
        print_errors(resolved.errors);
        return 1;
    }
    
    // Type check
    let typed = type_check(resolved.ast);
    if typed.has_errors {
        print_errors(typed.errors);
        return 1;
    }
    
    if options.mode == Mode::Check {
        println("Type checking successful");
        return 0;
    }
    
    // Generate IR
    let hir = generate_hir(typed.ast);
    
    // Generate code
    let llvm_ir = generate_llvm(hir);
    
    // Write output
    write_output_file(options.output_file, llvm_ir);
    
    println("Compilation successful");
    return 0;
}
```

---

## Part 2: Testing and Validation (Parallel to Development)

### Differential Testing

1. **Token Comparison**
   ```bash
   # For each test file:
   # 1. Generate tokens with aster0 (C#)
   dotnet run --project src/Aster.CLI emit-tokens test.ast > golden.json
   
   # 2. Generate tokens with aster1 (Aster)
   ./aster1 emit-tokens test.ast > output.json
   
   # 3. Compare
   diff golden.json output.json
   ```

2. **AST Comparison**
   - Same process as tokens
   - Normalize JSON before comparing (sort keys, handle formatting)

3. **Type Checker Comparison**
   - Run both compilers in check mode
   - Compare error messages
   - Validate type inference matches

4. **Code Generation Comparison**
   - Generate LLVM IR with both compilers
   - Normalize IR (variable names may differ)
   - Validate with `llvm-as` (IR parser)
   - Compare runtime behavior (execute programs)

### Unit Tests

Each module should have unit tests:
```rust
// In typecheck.ast
#[test]
fn test_unify_primitives() {
    let checker = new_type_checker();
    let result = unify(checker, Type::I32, Type::I32);
    assert(result.is_ok());
}

#[test]
fn test_unify_mismatch() {
    let checker = new_type_checker();
    let result = unify(checker, Type::I32, Type::Bool);
    assert(result.is_err());
}
```

---

## Part 3: Stage 2 and 3 (After Stage 1 Complete)

### Stage 2: Add Generics and Traits (~5000 LOC, 4-6 months)

**Prerequisites**: Stage 1 working and validated

1. **Generic Type Parameters** (~600 LOC)
   ```rust
   // In typecheck.ast
   fn instantiate_generic_function(func: Function, type_args: Vec<Type>) -> Function {
       // Replace type parameters with concrete types
       // Monomorphize function
   }
   ```

2. **Trait System** (~1000 LOC)
   ```rust
   struct Trait {
       name: String,
       methods: Vec<TraitMethod>,
       type_params: Vec<TypeParameter>
   }
   
   struct ImplBlock {
       trait_name: Option<String>,
       type_name: String,
       methods: Vec<Function>
   }
   
   fn resolve_trait_method(trait_name: String, method_name: String) -> Function {
       // Lookup method in trait
       // Find implementation for type
       // Return concrete function
   }
   ```

3. **Effect System** (~500 LOC)
   - Track effects: `@io`, `@alloc`, `@unsafe`, `@async`
   - Effect inference
   - Effect checking

4. **Advanced Type Checking** (~800 LOC)
   - Generic constraints
   - Trait bounds
   - Where clauses

5. **MIR Lowering** (~1200 LOC)
   - HIR → MIR transformation
   - SSA construction
   - More detailed than HIR

6. **Basic Optimizations** (~500 LOC)
   - Dead code elimination
   - Constant folding

### Stage 3: Full Self-Hosted Compiler (~3000 LOC, 4-6 months)

**Prerequisites**: Stage 2 working and validated

1. **Borrow Checker** (~1000 LOC)
   - Non-lexical lifetimes (NLL)
   - Two-phase borrowing
   - Move checking
   - Dataflow analysis

2. **Complete MIR** (~500 LOC)
   - Full MIR construction
   - Control flow graph
   - Dominance frontiers

3. **Optimizer** (~800 LOC)
   - Dead code elimination (DCE)
   - Common subexpression elimination (CSE)
   - Inlining
   - Scalar replacement of aggregates (SROA)
   - Loop optimizations

4. **LLVM Backend** (~700 LOC)
   - Complete LLVM IR emission
   - Debug info generation
   - Optimization levels
   - Platform-specific code

---

## Part 4: Timeline and Resource Estimates

### Stage 1 Completion Timeline

**Best Case** (1 dedicated experienced compiler engineer):
- Lexer completion: 1 week
- Name resolution: 2 weeks
- Type checker: 3 weeks
- IR generation: 2 weeks
- Code generation: 2 weeks
- CLI/I/O: 1 week
- Testing/debugging: 2 weeks
- **Total**: 13 weeks (~3 months)

**Realistic Case** (2 engineers, part-time):
- Same tasks but 1.5x duration due to coordination
- Additional time for reviews, documentation
- **Total**: 20 weeks (~5 months)

### Full Self-Hosting Timeline

| Phase | Duration (Best) | Duration (Realistic) |
|-------|----------------|---------------------|
| Stage 1 | 3 months | 5 months |
| Stage 2 | 4 months | 6 months |
| Stage 3 | 5 months | 7 months |
| Validation & Polish | 1 month | 2 months |
| **Total** | 13 months | 20 months |

### Resource Requirements

**Minimum**:
- 1 experienced compiler engineer (full-time)
- 1 intermediate engineer (part-time, testing)

**Recommended**:
- 1 senior compiler engineer (architecture, critical algorithms)
- 2 intermediate engineers (implementation)
- 1 QA engineer (testing, validation)

**Skills Needed**:
- Compiler construction (lexing, parsing, type systems)
- Type theory (Hindley-Milner, unification)
- LLVM IR generation
- Rust/Aster-like ownership systems
- Testing and validation

---

## Part 5: Alternative Approaches

### Option 1: Incremental Self-Hosting (Recommended)

Build Stage 1-3 sequentially as described above.

**Pros**:
- Validates each stage independently
- Catches issues early
- Provides bootstrap chain
- Educational value

**Cons**:
- Slower (12-20 months)
- More coordination needed
- May need to revisit earlier stages

### Option 2: Direct Stage 3 Implementation

Skip Stage 1 and 2, implement Stage 3 directly using Stage 0.

**Pros**:
- Faster (8-10 months)
- Single implementation effort
- Modern from start

**Cons**:
- No validation of bootstrap chain
- Harder to debug
- All-or-nothing approach
- Higher risk

### Option 3: Hybrid Approach

Implement core Stage 1, then jump to Stage 3 using Stage 0.

**Pros**:
- Faster than full sequential (10-14 months)
- Some bootstrap validation
- Lower risk than Option 2

**Cons**:
- Missing Stage 2 validation
- May need to backfill Stage 2 later

### Option 4: Pragmatic "Delegated Self-Hosting" (Quick Demo)

Stage 3 accepts CLI arguments but delegates to Stage 0 via FFI.

**Pros**:
- Very fast (2-4 weeks)
- Demonstrates self-hosting appearance
- Validates infrastructure

**Cons**:
- **Not true self-hosting**
- Misleading to users
- Still requires Stage 0
- Not recommended for production claims

---

## Part 6: Quick Wins vs Long-Term Goals

### Quick Wins (1-2 weeks each)

1. **Complete Lexer** (Priority: High)
   - Immediate value: better error messages
   - Small effort (150 LOC)
   - Unblocks parser improvements

2. **Enhance Error Reporting** (Priority: Medium)
   - Better diagnostics
   - Span-based error messages
   - Color output

3. **Add More Examples** (Priority: Low)
   - Demonstrates Stage 0 capabilities
   - Good for documentation
   - Testing edge cases

4. **Improve Documentation** (Priority: Medium)
   - Update STATUS.md with progress
   - Add tutorials
   - API documentation

5. **CI/CD Improvements** (Priority: Medium)
   - Add differential testing to CI
   - Automated performance tracking
   - Nightly fuzzing

### Long-Term Goals (3+ months each)

1. **Complete Stage 1** (Priority: Critical)
   - Foundation for self-hosting
   - ~2600 LOC
   - 3-5 months

2. **Implement Trait System** (Priority: High)
   - Needed for idiomatic Aster
   - ~1000 LOC
   - 1-2 months

3. **Borrow Checker** (Priority: High)
   - Safety guarantees
   - ~1000 LOC
   - 2-3 months

4. **Full Optimizer** (Priority: Medium)
   - Performance
   - ~800 LOC
   - 1-2 months

5. **Language Server Protocol** (Priority: Medium)
   - IDE support
   - Large effort
   - 2-3 months

---

## Part 7: Recommended Next Actions

### This Week

1. **Choose an approach**:
   - Incremental (Stage 1 → 2 → 3) [RECOMMENDED]
   - Direct Stage 3
   - Hybrid

2. **Set up development environment**:
   ```bash
   # Build Stage 0
   cd /home/runner/work/Aster-1/Aster-1
   dotnet build src/Aster.sln
   
   # Verify it works
   dotnet run --project src/Aster.CLI emit-ast-json examples/hello.ast
   ```

3. **Start with lexer completion** (easiest first task):
   - Pick one missing feature (e.g., character literals)
   - Implement in `aster/compiler/frontend/lexer.ast`
   - Test with Stage 0 compiler
   - Validate output

### This Month

1. **Complete lexer** (week 1)
2. **Begin name resolution** (weeks 2-3)
3. **Start type checker** (week 4)
4. **Set up differential testing** (ongoing)

### This Quarter

1. **Complete Stage 1 core components**:
   - Lexer ✓
   - Parser (already done)
   - Name resolution
   - Type checker
   - IR generation
   - Code generation

2. **Bootstrap compilation**:
   - Compile Stage 1 source with Stage 0
   - Produce working `aster1` binary
   - Validate with differential tests

3. **Documentation**:
   - Update STATUS.md
   - Create progress reports
   - Document decisions

---

## Part 8: Success Criteria

### Stage 1 Complete

- [ ] Lexer handles all tokens (100%)
- [ ] Parser works (already at 90%)
- [ ] Name resolution implemented
- [ ] Type checker with HM inference
- [ ] IR generation (AST → HIR)
- [ ] Code generation (HIR → LLVM IR)
- [ ] CLI accepts arguments
- [ ] File I/O works
- [ ] Can compile simple Core-0 programs
- [ ] Differential tests pass
- [ ] Bootstrap successful (aster0 compiles aster1)

### Stage 2 Complete

- [ ] Stage 1 criteria met
- [ ] Generic functions work
- [ ] Generic structs work
- [ ] Basic traits implemented
- [ ] Impl blocks work
- [ ] Effect system tracks effects
- [ ] MIR lowering complete
- [ ] Basic optimizations (DCE, constant folding)
- [ ] Can compile Core-1 programs
- [ ] Stage 1 compiles Stage 2 source

### Stage 3 Complete (True Self-Hosting)

- [ ] Stage 2 criteria met
- [ ] Borrow checker with NLL
- [ ] Full MIR with CFG
- [ ] Complete optimizer (DCE, CSE, inlining, SROA)
- [ ] Full LLVM backend
- [ ] Can compile full Aster language
- [ ] Stage 3 compiles itself: `aster3 + stage3/*.ast → aster3'`
- [ ] aster3' compiles itself: `aster3' + stage3/*.ast → aster3''`
- [ ] Deterministic: `aster3 ≡ aster3' ≡ aster3''`
- [ ] Bootstrap chain validated: `aster0 → aster1 → aster2 → aster3 → aster3' → aster3''`

---

## Conclusion

True self-hosting for Aster is achievable but requires **significant engineering effort**:

- **Minimum viable Stage 1**: ~2,630 lines of Aster code, 3-5 months
- **Complete self-hosting (Stage 3)**: ~6,000-11,000 lines, 12-20 months
- **Resources needed**: 2-5 engineers with compiler expertise

**Current status**: Stage 0 (C#) is production-ready with 119 passing tests. Use it for all production needs.

**Bootstrap status**: Infrastructure complete, implementation needed.

**Recommendation**:
1. ✅ Use Stage 0 (C#) for production
2. 🚧 Implement self-hosting incrementally (Stage 1 → 2 → 3)
3. 🎯 Set realistic 18-24 month timeline
4. 🎬 Start with Stage 1 lexer completion (this week!)

**Not recommended**:
- ❌ Claiming self-hosting without implementation
- ❌ Delegated/fake self-hosting
- ❌ Rushing incomplete implementations

---

**Ready to contribute?** Start with completing the lexer! See section "Priority 1: Complete the Lexer" above.

**Questions?** Refer to:
- [SELF_HOSTING_ROADMAP.md](SELF_HOSTING_ROADMAP.md) - Strategic overview
- [docs/NEXT_STEPS_GUIDE.md](docs/NEXT_STEPS_GUIDE.md) - Detailed guide
- [STATUS.md](STATUS.md) - Current status
- [TOOLCHAIN.md](TOOLCHAIN.md) - Build instructions
