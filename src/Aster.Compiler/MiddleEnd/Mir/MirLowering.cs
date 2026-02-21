using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.Hir;
using Aster.Compiler.Frontend.TypeSystem;

namespace Aster.Compiler.MiddleEnd.Mir;

/// <summary>
/// Lowers HIR to MIR (Mid-level IR).
/// Converts high-level constructs into SSA-based instructions with explicit control flow.
/// </summary>
public sealed class MirLowering
{
    private MirFunction? _currentFunction;
    private MirBasicBlock? _currentBlock;
    private int _tempCounter;
    private readonly Dictionary<string, MirOperand> _localVariables = new();
    private readonly Dictionary<string, MirType> _functionReturnTypes = new();
    // Loop context stack for break/continue target resolution
    private readonly Stack<(int BreakTarget, int ContinueTarget)> _loopStack = new();
    // Closure functions collected during lowering (added to module at the end)
    private readonly List<MirFunction> _closureFunctions = new();
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>Lower an HIR program to MIR.</summary>
    public MirModule Lower(HirProgram program)
    {
        var module = new MirModule("main");

        // First pass: collect function signatures for return type lookup
        foreach (var decl in program.Declarations)
        {
            if (decl is HirFunctionDecl fn)
            {
                var returnType = fn.ReturnType != null ? ResolveType(fn.ReturnType) : MirType.Void;
                _functionReturnTypes[fn.Symbol.Name] = returnType;
            }
        }

        // Second pass: lower function bodies
        foreach (var decl in program.Declarations)
        {
            if (decl is HirFunctionDecl fn)
            {
                var mirFn = LowerFunction(fn);
                module.Functions.Add(mirFn);
            }
        }

        // Third pass: append any closure functions emitted during lowering
        foreach (var closureFn in _closureFunctions)
            module.Functions.Add(closureFn);

        return module;
    }

    private MirFunction LowerFunction(HirFunctionDecl fn)
    {
        _currentFunction = new MirFunction(fn.Symbol.Name);
        _tempCounter = 0;
        _localVariables.Clear(); // Clear local variables for new function

        // Add parameters
        for (int i = 0; i < fn.Parameters.Count; i++)
        {
            var param = fn.Parameters[i];
            var mirType = ResolveType(param.TypeRef);
            _currentFunction.Parameters.Add(new MirParameter(param.Symbol.Name, mirType, i));
        }

        _currentFunction.ReturnType = fn.ReturnType != null ? ResolveType(fn.ReturnType) : MirType.Void;

        // Create entry block
        _currentBlock = _currentFunction.CreateBlock("entry");

        // Lower body
        var result = LowerBlock(fn.Body);

        // Add return if not already terminated
        if (_currentBlock?.Terminator == null)
        {
            // If function returns void, don't return the tail expression value
            if (_currentFunction.ReturnType.Name == "void")
            {
                _currentBlock!.Terminator = new MirReturn(null);
            }
            else
            {
                _currentBlock!.Terminator = new MirReturn(result);
            }
        }

        return _currentFunction;
    }

    private MirOperand? LowerBlock(HirBlock block)
    {
        MirOperand? lastResult = null;

        foreach (var stmt in block.Statements)
        {
            LowerNode(stmt);
        }

        if (block.TailExpression != null)
        {
            lastResult = LowerExpr(block.TailExpression);
        }

        return lastResult;
    }

    private void LowerNode(HirNode node)
    {
        switch (node)
        {
            case HirLetStmt let:
                LowerLetStmt(let);
                break;
            case HirReturnStmt ret:
                LowerReturn(ret);
                break;
            case HirExprStmt es:
                LowerExpr(es.Expression);
                break;
            case HirWhileStmt ws:
                LowerWhile(ws);
                break;
            case HirForStmt forStmt:
                LowerForStmt(forStmt);
                break;
            case HirBreakStmt:
                LowerBreak();
                break;
            case HirContinueStmt:
                LowerContinue();
                break;
            default:
                LowerExpr(node);
                break;
        }
    }

    private void LowerLetStmt(HirLetStmt let)
    {
        if (let.Initializer != null)
        {
            var value = LowerExpr(let.Initializer);
            if (value != null)
            {
                // Instead of creating a new variable and assigning,
                // just map the variable name to the value operand directly
                // This works because in SSA form, variables are just names for values
                _localVariables[let.Symbol.Name] = value;
                
                // Don't emit an Assign instruction - in SSA, we just use the value directly
            }
        }
        else
        {
            // No initializer - create a placeholder (though this is unusual in SSA)
            var dest = MirOperand.Variable(let.Symbol.Name, MirType.I64);
            _localVariables[let.Symbol.Name] = dest;
        }
    }

    private void LowerReturn(HirReturnStmt ret)
    {
        MirOperand? value = null;
        if (ret.Value != null)
        {
            value = LowerExpr(ret.Value);
            // Coerce the return value type to match the function's return type
            if (value != null && _currentFunction != null)
            {
                value = CoerceType(value, _currentFunction.ReturnType);
            }
        }
        _currentBlock!.Terminator = new MirReturn(value);
    }

    private void LowerWhile(HirWhileStmt ws)
    {
        var condBlock = _currentFunction!.CreateBlock("while.cond");
        var bodyBlock = _currentFunction.CreateBlock("while.body");
        var exitBlock = _currentFunction.CreateBlock("while.exit");

        _currentBlock!.Terminator = new MirBranch(condBlock.Index);

        _currentBlock = condBlock;
        var cond = LowerExpr(ws.Condition);
        _currentBlock.Terminator = new MirConditionalBranch(cond!, bodyBlock.Index, exitBlock.Index);

        _currentBlock = bodyBlock;
        LowerBlock(ws.Body);
        if (_currentBlock.Terminator == null)
            _currentBlock.Terminator = new MirBranch(condBlock.Index);

        _currentBlock = exitBlock;
    }

    private MirOperand? LowerExpr(HirNode node)
    {
        switch (node)
        {
            case HirLiteralExpr lit:
                return LowerLiteral(lit);

            case HirIdentifierExpr id:
                // First check if it's a local variable
                if (_localVariables.TryGetValue(id.Name, out var localVar))
                {
                    return localVar;
                }
                
                // Then try to find the type from function parameters
                var paramType = MirType.I64; // default
                if (_currentFunction != null)
                {
                    var param = _currentFunction.Parameters.FirstOrDefault(p => p.Name == id.Name);
                    if (param != null)
                    {
                        paramType = param.Type;
                    }
                }
                return MirOperand.Variable(id.Name, paramType);

            case HirPathExpr path:
                // Handle path expressions like Vec::new, String::from, etc.
                // For now, treat them as function references
                var pathName = string.Join("::", path.Segments);
                return MirOperand.FunctionRef(pathName);

            case HirCallExpr call:
                return LowerCall(call);

            case HirBinaryExpr bin:
                return LowerBinary(bin);

            case HirUnaryExpr un:
                return LowerUnary(un);

            case HirIfExpr ifExpr:
                return LowerIf(ifExpr);

            case HirBlock block:
                return LowerBlock(block);

            case HirAssignExpr assign:
                return LowerAssign(assign);

            case HirMemberAccessExpr ma:
                var obj = LowerExpr(ma.Object);
                // For Core-0 bootstrap, infer field type from field name
                // Common patterns:
                // - *_count, *_index, line, column, position, start, length: i32
                // - success, is_*, has_*: bool
                // - String fields, Vec fields, Box fields, other struct types: ptr
                var fieldType = MirType.Ptr; // Default to pointer
                
                // Heuristic for integer fields
                if (ma.Member.Contains("count") || ma.Member.Contains("index") || 
                    ma.Member == "line" || ma.Member == "column" || ma.Member.Contains("length") ||
                    ma.Member.Contains("position") || ma.Member == "start")
                {
                    fieldType = MirType.I32;
                }
                // Heuristic for bool fields
                else if (ma.Member == "success" || ma.Member.StartsWith("is_") || 
                         ma.Member.StartsWith("has_") || ma.Member == "is_mutable")
                {
                    fieldType = MirType.Bool;
                }
                
                var temp = NewTemp(fieldType);
                Emit(new MirInstruction(MirOpcode.Load, temp, new[] { obj! }, ma.Member));
                return temp;

            case HirIndexExpr idx:
                return LowerIndexExpr(idx);

            case HirMatchExpr matchExpr:
                return LowerMatchExpr(matchExpr);

            case HirClosureExpr closure:
                return LowerClosure(closure);

            // Phase 4
            case HirMethodCallExpr mc:
                return LowerMethodCall(mc);

            case HirMacroInvocationExpr macroInv:
                return LowerMacroInvocation(macroInv);

            // Phase 6: casts, array literals
            case HirCastExpr cast:
                return LowerCastExpr(cast);
            case HirArrayLiteralExpr arr:
                return LowerArrayLiteral(arr);

            default:
                return null;
        }
    }

    private MirOperand LowerLiteral(HirLiteralExpr lit)
    {
        return lit.LiteralKind switch
        {
            LiteralKind.Integer => MirOperand.Constant(lit.Value, MirType.I64),
            LiteralKind.Float => MirOperand.Constant(lit.Value, MirType.F64),
            LiteralKind.String => MirOperand.Constant(lit.Value, MirType.StringPtr),
            LiteralKind.Char => MirOperand.Constant(lit.Value, MirType.Char),
            LiteralKind.Bool => MirOperand.Constant(lit.Value, MirType.Bool),
            _ => MirOperand.Constant(0, MirType.I64),
        };
    }

    private MirOperand? LowerCall(HirCallExpr call)
    {
        var callee = LowerExpr(call.Callee);
        var args = new List<MirOperand>();
        foreach (var arg in call.Arguments)
        {
            var a = LowerExpr(arg);
            if (a != null) args.Add(a);
        }

        var operands = new List<MirOperand>();
        if (callee != null) operands.Add(callee);
        operands.AddRange(args);

        // Determine the return type of the called function
        MirType returnType = MirType.I64; // default
        
        // Try to resolve the function name from the callee
        string? functionName = null;
        if (call.Callee is HirIdentifierExpr idExpr)
        {
            functionName = idExpr.Name;
        }
        else if (call.Callee is HirPathExpr pathExpr)
        {
            functionName = string.Join("::", pathExpr.Segments);
            
            // Handle built-in constructors for Core-0 types
            // These should return ptr (pointer to heap-allocated data)
            if (functionName == "Vec::new" || functionName.StartsWith("Vec::"))
            {
                returnType = MirType.Ptr;
            }
            else if (functionName == "Box::new" || functionName.StartsWith("Box::"))
            {
                returnType = MirType.Ptr;
            }
            else if (functionName == "String::new" || functionName.StartsWith("String::"))
            {
                returnType = MirType.Ptr;
            }
        }
        
        // Look up the function's return type if not already determined
        if (functionName != null && returnType.Name == "i64" && _functionReturnTypes.TryGetValue(functionName, out var declaredReturnType))
        {
            returnType = declaredReturnType;
        }
        
        // If the return type is void, don't create a result temporary
        if (returnType.Name == "void")
        {
            Emit(new MirInstruction(MirOpcode.Call, null, operands));
            return null;
        }
        
        var result = NewTemp(returnType);
        Emit(new MirInstruction(MirOpcode.Call, result, operands));
        return result;
    }

    private MirOperand LowerBinary(HirBinaryExpr bin)
    {
        var left = LowerExpr(bin.Left)!;
        var right = LowerExpr(bin.Right)!;
        
        // Infer result type from operands (use left operand's type)
        // For comparison operators, result is always bool
        MirType resultType;
        switch (bin.Operator)
        {
            case BinaryOperator.Eq:
            case BinaryOperator.Ne:
            case BinaryOperator.Lt:
            case BinaryOperator.Le:
            case BinaryOperator.Gt:
            case BinaryOperator.Ge:
                resultType = MirType.Bool;
                break;
            default:
                resultType = left.Type;
                break;
        }
        
        var result = NewTemp(resultType);
        Emit(new MirInstruction(MirOpcode.BinaryOp, result, new[] { left, right }, bin.Operator));
        return result;
    }

    private MirOperand LowerUnary(HirUnaryExpr un)
    {
        var operand = LowerExpr(un.Operand)!;
        // ? operator: early-return on Err (Result) or None (Option), unwrap the Ok/Some value
        if (un.Operator == UnaryOperator.Try)
        {
            // Emit:  if operand.is_err { return operand }  else { operand.unwrap() }
            var isErrTemp = NewTemp(MirType.Bool);
            Emit(new MirInstruction(MirOpcode.Call, isErrTemp,
                new[] { operand }, "__is_err_or_none"));

            var errBlock = _currentFunction!.CreateBlock("try.err");
            var okBlock = _currentFunction.CreateBlock("try.ok");
            _currentBlock!.Terminator = new MirConditionalBranch(isErrTemp, errBlock.Index, okBlock.Index);

            // Err path: return the error
            _currentBlock = errBlock;
            errBlock.Terminator = new MirReturn(operand);

            // Ok path: unwrap and continue
            _currentBlock = okBlock;
            var unwrapped = NewTemp(operand.Type);
            Emit(new MirInstruction(MirOpcode.Call, unwrapped,
                new[] { operand }, "__unwrap_ok_or_some"));
            return unwrapped;
        }

        // Infer result type from operand
        var result = NewTemp(operand.Type);
        Emit(new MirInstruction(MirOpcode.UnaryOp, result, new[] { operand }, un.Operator));
        return result;
    }

    /// <summary>
    /// Lower a closure expression: emit an anonymous MIR function and return a FunctionRef to it.
    /// Captures are approximated — in bootstrap mode all free variables are passed as parameters.
    /// </summary>
    private MirOperand LowerClosure(HirClosureExpr closure)
    {
        // Save current function context
        var savedFn = _currentFunction;
        var savedBlock = _currentBlock;
        var savedLocals = new Dictionary<string, MirOperand>(_localVariables);
        var savedCounter = _tempCounter;

        _currentFunction = new MirFunction(closure.MangledName);
        _tempCounter = 0;
        _localVariables.Clear();

        // Register parameters
        for (int i = 0; i < closure.Parameters.Count; i++)
        {
            var p = closure.Parameters[i];
            var pt = ResolveType(p.TypeRef);
            var mp = new MirParameter(p.Symbol.Name, pt, i);
            _currentFunction.Parameters.Add(mp);
            _localVariables[p.Symbol.Name] = MirOperand.Variable(p.Symbol.Name, pt);
        }

        var entryBlock = _currentFunction.CreateBlock("entry");
        _currentBlock = entryBlock;

        var bodyVal = LowerExpr(closure.Body);
        if (_currentBlock.Terminator == null)
            _currentBlock.Terminator = new MirReturn(bodyVal);

        // Register the closure function in the current module-level scope
        // (the module is built by the Lower() caller; we append to it via a side list)
        _closureFunctions.Add(_currentFunction);

        // Restore context
        _currentFunction = savedFn;
        _currentBlock = savedBlock;
        _localVariables.Clear();
        foreach (var kv in savedLocals) _localVariables[kv.Key] = kv.Value;
        _tempCounter = savedCounter;

        return MirOperand.FunctionRef(closure.MangledName);
    }

    private MirOperand? LowerIf(HirIfExpr ifExpr)
    {
        var cond = LowerExpr(ifExpr.Condition)!;

        var thenBlock = _currentFunction!.CreateBlock("if.then");
        var elseBlock = _currentFunction.CreateBlock("if.else");
        var mergeBlock = _currentFunction.CreateBlock("if.merge");

        _currentBlock!.Terminator = new MirConditionalBranch(cond, thenBlock.Index, elseBlock.Index);

        _currentBlock = thenBlock;
        var thenResult = LowerBlock(ifExpr.ThenBranch);
        if (_currentBlock.Terminator == null)
            _currentBlock.Terminator = new MirBranch(mergeBlock.Index);

        _currentBlock = elseBlock;
        if (ifExpr.ElseBranch is HirBlock elBlock)
        {
            LowerBlock(elBlock);
        }
        else if (ifExpr.ElseBranch != null)
        {
            LowerExpr(ifExpr.ElseBranch);
        }
        if (_currentBlock.Terminator == null)
            _currentBlock.Terminator = new MirBranch(mergeBlock.Index);

        _currentBlock = mergeBlock;
        return thenResult;
    }

    private MirOperand? LowerAssign(HirAssignExpr assign)
    {
        var target = LowerExpr(assign.Target);
        var value = LowerExpr(assign.Value);
        if (target != null && value != null)
        {
            Emit(new MirInstruction(MirOpcode.Store, target, new[] { value }));
        }
        return null;
    }

    private MirType ResolveType(HirTypeRef? typeRef)
    {
        if (typeRef == null) return MirType.Void;
        return typeRef.Name switch
        {
            "i32" => MirType.I32,
            "i64" => MirType.I64,
            "f32" => MirType.F32,
            "f64" => MirType.F64,
            "bool" => MirType.Bool,
            "char" => MirType.Char,
            "String" => MirType.StringPtr,
            "void" => MirType.Void,
            _ => MirType.Ptr,
        };
    }

    private MirOperand NewTemp(MirType type) => MirOperand.Temp($"_t{_tempCounter++}", type);

    private void Emit(MirInstruction instruction) => _currentBlock!.AddInstruction(instruction);

    /// <summary>
    /// Coerce an operand to a target type if it's a constant with a compatible but different type.
    /// This is primarily for handling integer literals that default to i64 but need to be i32, etc.
    /// </summary>
    private MirOperand CoerceType(MirOperand operand, MirType targetType)
    {
        // If the operand is already the target type, no coercion needed
        if (operand.Type.Name == targetType.Name)
            return operand;

        // Only coerce constants
        if (operand.Kind != MirOperandKind.Constant)
            return operand;

        // Coerce integer types
        if (targetType.Name == "i32" && operand.Type.Name == "i64")
        {
            return MirOperand.Constant(operand.Value, MirType.I32);
        }
        if (targetType.Name == "i64" && operand.Type.Name == "i32")
        {
            return MirOperand.Constant(operand.Value, MirType.I64);
        }

        // Coerce float types
        if (targetType.Name == "f32" && operand.Type.Name == "f64")
        {
            return MirOperand.Constant(operand.Value, MirType.F32);
        }
        if (targetType.Name == "f64" && operand.Type.Name == "f32")
        {
            return MirOperand.Constant(operand.Value, MirType.F64);
        }

        // Default: return original operand
        return operand;
    }

    // ===== Phase 2 Closeout: for / match / break / continue / index =====

    private void LowerForStmt(HirForStmt forStmt)
    {
        // Lower:  for x in iterable { body }
        // Into:   _idx = 0; loop { if _idx >= len(iterable) break; x = get(iterable, _idx); body; _idx++; }

        var iterable = LowerExpr(forStmt.Iterable) ?? MirOperand.Variable("_iterable", MirType.Ptr);

        // Compute length once before loop
        var lenTemp = NewTemp(MirType.I64);
        Emit(new MirInstruction(MirOpcode.Call, lenTemp, new[] { MirOperand.FunctionRef("vec_len"), iterable }, "vec_len"));

        // Counter variable
        var idxName = $"_for_idx_{_tempCounter++}";
        var idxVar = MirOperand.Variable(idxName, MirType.I64);
        Emit(new MirInstruction(MirOpcode.Assign, idxVar, new[] { MirOperand.Constant(0L, MirType.I64) }));

        // Blocks: cond → body → incr → exit
        var condBlock = NewBlock();
        var bodyBlock = NewBlock();
        var incrBlock = NewBlock();
        var exitBlock = NewBlock();

        _currentBlock!.Terminator = new MirBranch(condBlock.Index);

        // Condition block: if idx >= len → exit, else body
        _currentBlock = condBlock;
        var cond = NewTemp(MirType.Bool);
        Emit(new MirInstruction(MirOpcode.BinaryOp, cond, new[] { idxVar, lenTemp }, "lt"));
        _currentBlock.Terminator = new MirConditionalBranch(cond, bodyBlock.Index, exitBlock.Index);

        // Push loop context for break/continue
        _loopStack.Push((exitBlock.Index, incrBlock.Index));

        // Body block: bind loop variable, lower body
        _currentBlock = bodyBlock;
        var elemTemp = NewTemp(MirType.Ptr);
        Emit(new MirInstruction(MirOpcode.Call, elemTemp, new[] { MirOperand.FunctionRef("vec_get"), iterable, idxVar }, "vec_get"));
        _localVariables[forStmt.Variable.Name] = elemTemp;

        foreach (var stmt in forStmt.Body.Statements)
            LowerNode(stmt);
        if (forStmt.Body.TailExpression != null)
            LowerExpr(forStmt.Body.TailExpression);

        // Fall through to increment (unless already terminated by break)
        if (_currentBlock.Terminator == null)
            _currentBlock.Terminator = new MirBranch(incrBlock.Index);

        // Increment block: idx++ → cond
        _currentBlock = incrBlock;
        var newIdx = NewTemp(MirType.I64);
        Emit(new MirInstruction(MirOpcode.BinaryOp, newIdx, new[] { idxVar, MirOperand.Constant(1L, MirType.I64) }, "add"));
        Emit(new MirInstruction(MirOpcode.Assign, idxVar, new[] { newIdx }));
        _currentBlock.Terminator = new MirBranch(condBlock.Index);

        _loopStack.Pop();
        _currentBlock = exitBlock;
    }

    private void LowerBreak()
    {
        if (_loopStack.Count > 0)
        {
            var (breakTarget, _) = _loopStack.Peek();
            _currentBlock!.Terminator = new MirBranch(breakTarget);
            // Start a new dead block to absorb any subsequent instructions
            _currentBlock = NewBlock();
        }
    }

    private void LowerContinue()
    {
        if (_loopStack.Count > 0)
        {
            var (_, continueTarget) = _loopStack.Peek();
            _currentBlock!.Terminator = new MirBranch(continueTarget);
            _currentBlock = NewBlock();
        }
    }

    private MirOperand LowerIndexExpr(HirIndexExpr idx)
    {
        var target = LowerExpr(idx.Target) ?? MirOperand.Variable("_target", MirType.Ptr);
        var index = LowerExpr(idx.Index) ?? MirOperand.Constant(0L, MirType.I64);
        var result = NewTemp(MirType.Ptr);
        Emit(new MirInstruction(MirOpcode.Call, result, new[] { MirOperand.FunctionRef("vec_get"), target, index }, "vec_get"));
        return result;
    }

    private MirOperand? LowerMatchExpr(HirMatchExpr matchExpr)
    {
        var scrutinee = LowerExpr(matchExpr.Scrutinee);
        if (scrutinee == null || matchExpr.Arms.Count == 0) return null;

        // Create exit block to collect results
        var exitBlock = NewBlock();
        var resultTemp = NewTemp(MirType.I64);

        // Lower each arm as a conditional branch
        for (int i = 0; i < matchExpr.Arms.Count; i++)
        {
            var arm = matchExpr.Arms[i];
            var armBlock = NewBlock();
            var nextBlock = i < matchExpr.Arms.Count - 1 ? NewBlock() : exitBlock;

            // Generate condition for the pattern
            var cond = LowerPatternCondition(scrutinee, arm.Pattern);
            if (cond != null)
                _currentBlock!.Terminator = new MirConditionalBranch(cond, armBlock.Index, nextBlock.Index);
            else
                _currentBlock!.Terminator = new MirBranch(armBlock.Index); // wildcard always matches

            // Arm body
            _currentBlock = armBlock;
            var armValue = LowerExpr(arm.Body);
            if (armValue != null)
                Emit(new MirInstruction(MirOpcode.Assign, resultTemp, new[] { armValue }));
            _currentBlock.Terminator = new MirBranch(exitBlock.Index);

            _currentBlock = nextBlock;
        }

        _currentBlock = exitBlock;
        return resultTemp;
    }

    private MirOperand? LowerPatternCondition(MirOperand scrutinee, HirPattern pattern)
    {
        return pattern.Kind switch
        {
            PatternKind.Wildcard or PatternKind.Variable => null, // always matches
            PatternKind.Literal when pattern.LiteralValue != null =>
                EmitEqCheck(scrutinee, MirOperand.Constant(pattern.LiteralValue, scrutinee.Type)),
            _ => null,
        };
    }

    private MirOperand EmitEqCheck(MirOperand left, MirOperand right)
    {
        var result = NewTemp(MirType.Bool);
        Emit(new MirInstruction(MirOpcode.BinaryOp, result, new[] { left, right }, "eq"));
        return result;
    }

    private MirBasicBlock NewBlock()
    {
        return _currentFunction!.CreateBlock($"bb{_currentFunction.BasicBlocks.Count}");
    }

    // ========== Phase 4: Method Calls and Macros ==========

    /// <summary>
    /// Lower a method call: receiver.method(args) →
    /// mangled name call: __TypeName_method(receiver, args...).
    /// The mangling is intentionally simple: two underscores + TypeName + underscore + method.
    /// </summary>
    private MirOperand? LowerMethodCall(HirMethodCallExpr mc)
    {
        var receiver = LowerExpr(mc.Receiver);
        var args = mc.Arguments.Select(a => LowerExpr(a)).Where(a => a != null).Cast<MirOperand>().ToList();

        // Build mangled name.  If we know the receiver type from _localVariables / function params, use it;
        // otherwise fall back to "__method_name" (type-erased path).
        var mangledName = $"__{mc.MethodName}";
        if (receiver != null)
        {
            // Extract type hint from the operand if available
            var typeName = receiver.Type.Name ?? "obj";
            mangledName = $"__{typeName}_{mc.MethodName}";
        }

        var allArgs = new List<MirOperand>();
        if (receiver != null) allArgs.Add(receiver);
        allArgs.AddRange(args);

        var result = NewTemp(MirType.I64);
        Emit(new MirInstruction(MirOpcode.Call, result, allArgs, mangledName));
        return result;
    }

    /// <summary>Lower a macro invocation by delegating to its expanded HIR form.</summary>
    private MirOperand? LowerMacroInvocation(HirMacroInvocationExpr macroInv)
    {
        if (macroInv.Expanded != null)
            return LowerExpr(macroInv.Expanded);
        // Unknown macro with no expansion: no-op
        return null;
    }

    // ========== Phase 6: Casts and Array Literals ==========

    /// <summary>
    /// Lower a cast expression (expr as Type).
    /// Emits a Cast MIR instruction that the backend can lower to an appropriate
    /// numeric conversion or bitcast.
    /// </summary>
    private MirOperand LowerCastExpr(HirCastExpr cast)
    {
        var src = LowerExpr(cast.Expression);
        var targetMirType = cast.TargetType switch
        {
            PrimitiveType pt when pt == PrimitiveType.I32 || pt == PrimitiveType.I64 => MirType.I64,
            PrimitiveType pt when pt == PrimitiveType.F32 || pt == PrimitiveType.F64 => MirType.F64,
            _ => MirType.I64,
        };

        var result = NewTemp(targetMirType);
        Emit(new MirInstruction(MirOpcode.Cast, result,
            src != null ? new List<MirOperand> { src } : new List<MirOperand>(),
            cast.TargetType.DisplayName));
        return result;
    }

    /// <summary>
    /// Lower an array literal [a, b, c] to a sequence of stores into a stack-allocated array.
    /// Returns a pointer to the first element (i64 index 0).
    /// </summary>
    private MirOperand LowerArrayLiteral(HirArrayLiteralExpr arr)
    {
        var arrayPtr = NewTemp(MirType.I64);
        // Emit array allocation with length as metadata
        Emit(new MirInstruction(MirOpcode.Alloca, arrayPtr, new List<MirOperand>(), arr.Elements.Count.ToString()));

        for (int i = 0; i < arr.Elements.Count; i++)
        {
            var elemVal = LowerExpr(arr.Elements[i]);
            if (elemVal == null) continue;
            var idxOp = MirOperand.Constant((long)i, MirType.I64);
            Emit(new MirInstruction(MirOpcode.Store, null,
                new List<MirOperand> { arrayPtr, idxOp, elemVal }, $"array_store_{i}"));
        }

        return arrayPtr;
    }
}
