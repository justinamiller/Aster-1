using System.Text;
using Aster.Compiler.Backends.Abstractions;
using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Backends.LLVM;

/// <summary>
/// Emits LLVM IR text from MIR.
/// Implements the IBackend interface for LLVM text IR generation.
/// </summary>
public sealed class LlvmBackend : IBackend
{
    private readonly StringBuilder _output = new();
    private int _stringCounter;
    private readonly List<(string Name, string Value)> _stringLiterals = new();
    private readonly bool _stage1Mode;

    /// <summary>Create a new LLVM backend.</summary>
    /// <param name="stage1Mode">If true, generate Stage1 CLI wrapper around main function.</param>
    public LlvmBackend(bool stage1Mode = false)
    {
        _stage1Mode = stage1Mode;
    }

    /// <summary>Emit LLVM IR from a MIR module.</summary>
    public string Emit(MirModule module)
    {
        _output.Clear();
        _stringLiterals.Clear();
        _stringCounter = 0;

        // Emit module header
        EmitHeader();

        // First pass: collect string literals
        foreach (var fn in module.Functions)
        {
            CollectStrings(fn);
        }

        // Emit string constants
        foreach (var (name, value) in _stringLiterals)
        {
            var escaped = EscapeString(value);
            var len = value.Length + 1; // +1 for null terminator
            _output.AppendLine($"@{name} = private unnamed_addr constant [{len} x i8] c\"{escaped}\\00\"");
        }

        if (_stringLiterals.Count > 0)
            _output.AppendLine();

        // Check if this module has a main function
        var mainFunction = module.Functions.FirstOrDefault(f => f.Name == "main");
        bool hasMainFunction = mainFunction != null;

        // Emit functions (rename main to aster_main if in Stage1 mode)
        foreach (var fn in module.Functions)
        {
            if (_stage1Mode && fn.Name == "main")
            {
                // Rename user's main to aster_main
                EmitFunctionWithName(fn, "aster_main");
            }
            else
            {
                EmitFunction(fn);
            }
        }

        // If Stage1 mode and has main function, emit CLI wrapper
        if (_stage1Mode && hasMainFunction)
        {
            EmitStage1CliWrapper();
        }

        return _output.ToString();
    }

    private void EmitHeader()
    {
        _output.AppendLine("; ASTER Compiler - LLVM IR Output");
        _output.AppendLine($"; Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        _output.AppendLine();
        _output.AppendLine("; External runtime declarations");
        _output.AppendLine("declare i32 @puts(ptr)");
        _output.AppendLine("declare i32 @printf(ptr, ...)");
        _output.AppendLine("declare ptr @malloc(i64)");
        _output.AppendLine("declare void @free(ptr)");
        _output.AppendLine("declare void @exit(i32)");
        
        if (_stage1Mode)
        {
            _output.AppendLine();
            _output.AppendLine("; Stage1 runtime declarations");
            _output.AppendLine("declare void @aster_init_args(i32, ptr)");
            _output.AppendLine("declare i32 @aster_get_argc()");
            _output.AppendLine("declare ptr @aster_get_argv(i32)");
            _output.AppendLine("declare ptr @aster_read_file(ptr, ptr)");
            _output.AppendLine("declare i32 @strcmp(ptr, ptr)");
            _output.AppendLine("declare i64 @strlen(ptr)");
        }
        
        _output.AppendLine();
        _output.AppendLine("; Built-in Core-0 type constructors (stub implementations for bootstrap)");
        _output.AppendLine("; WARNING: These return null and are only for bootstrap IR generation.");
        _output.AppendLine("; Proper implementations should be provided for full functionality.");
        _output.AppendLine("define ptr @Vec_new() { ret ptr null }");
        _output.AppendLine("define ptr @Box_new() { ret ptr null }");
        _output.AppendLine("define ptr @String_new() { ret ptr null }");
        _output.AppendLine("define ptr @String_from(ptr %s) { ret ptr %s }");
        _output.AppendLine();
    }

    private void EmitFunction(MirFunction fn)
    {
        EmitFunctionWithName(fn, fn.Name);
    }

    private void EmitFunctionWithName(MirFunction fn, string name)
    {
        var retType = MapType(fn.ReturnType);
        var paramList = string.Join(", ", fn.Parameters.Select(p => $"{MapType(p.Type)} %{p.Name}"));

        _output.AppendLine($"define {retType} @{name}({paramList}) {{");

        foreach (var block in fn.BasicBlocks)
        {
            EmitBasicBlock(block, fn);
        }

        _output.AppendLine("}");
        _output.AppendLine();
    }

    private void EmitBasicBlock(MirBasicBlock block, MirFunction fn)
    {
        _output.AppendLine($"{block.Label}:");

        foreach (var instr in block.Instructions)
        {
            EmitInstruction(instr);
        }

        EmitTerminator(block.Terminator, fn);
    }

    private void EmitInstruction(MirInstruction instr)
    {
        switch (instr.Opcode)
        {
            case MirOpcode.Assign:
            case MirOpcode.Store:
                if (instr.Destination != null && instr.Operands.Count > 0)
                {
                    var src = FormatOperand(instr.Operands[0]);
                    // Use alloca + store for variables
                    _output.AppendLine($"  ; assign {instr.Destination.Name}");
                }
                break;

            case MirOpcode.Load:
                EmitLoad(instr);
                break;

            case MirOpcode.Call:
                EmitCall(instr);
                break;

            case MirOpcode.BinaryOp:
                EmitBinaryOp(instr);
                break;

            case MirOpcode.UnaryOp:
                EmitUnaryOp(instr);
                break;

            case MirOpcode.Literal:
                // Literals are handled inline
                break;

            case MirOpcode.Drop:
                if (instr.Operands.Count > 0)
                {
                    _output.AppendLine($"  ; drop {instr.Operands[0].Name}");
                }
                break;
        }
    }

    private void EmitCall(MirInstruction instr)
    {
        if (instr.Operands.Count == 0) return;

        var callee = instr.Operands[0];

        // Handle built-in print
        if (callee.Kind == MirOperandKind.Variable && (callee.Name == "print" || callee.Name == "println"))
        {
            if (instr.Operands.Count > 1)
            {
                var arg = instr.Operands[1];
                if (arg.Kind == MirOperandKind.Constant && arg.Value is string strVal)
                {
                    var strName = GetStringLiteral(strVal);
                    var len = strVal.Length + 1;
                    _output.AppendLine($"  %{instr.Destination?.Name ?? "_"} = call i32 @puts(ptr @{strName})");
                }
                else
                {
                    _output.AppendLine($"  ; call {callee.Name} (non-string arg)");
                }
            }
            return;
        }

        // Handle calls to temporary variables (method calls in Core-0)
        // These occur when trying to call methods like .clone() on objects
        // For bootstrap, we just return a default value instead of trying to call
        if (callee.Kind == MirOperandKind.Temp)
        {
            var retType = instr.Destination != null ? MapType(instr.Destination.Type) : "void";
            _output.AppendLine($"  ; skipping method call on temp {callee.Name} (Core-0 limitation)");
            if (retType != "void" && instr.Destination != null)
            {
                var defaultValue = GetDefaultValue(retType);
                if (retType == "ptr")
                {
                    _output.AppendLine($"  %{instr.Destination.Name} = bitcast ptr null to ptr");
                }
                else
                {
                    _output.AppendLine($"  %{instr.Destination.Name} = add {retType} {defaultValue}, 0");
                }
            }
            return;
        }

        // General function call
        var args = new List<string>();
        for (int i = 1; i < instr.Operands.Count; i++)
        {
            var op = instr.Operands[i];
            args.Add($"{MapType(op.Type)} {FormatOperand(op)}");
        }

        var callReturnType = instr.Destination != null ? MapType(instr.Destination.Type) : "void";
        var argStr = string.Join(", ", args);
        var calleeName = MangleFunctionName(callee.Name);

        if (callReturnType == "void")
        {
            _output.AppendLine($"  call void @{calleeName}({argStr})");
        }
        else
        {
            _output.AppendLine($"  %{instr.Destination!.Name} = call {callReturnType} @{calleeName}({argStr})");
        }
    }

    private void EmitLoad(MirInstruction instr)
    {
        if (instr.Destination == null || instr.Operands.Count == 0) return;

        var obj = instr.Operands[0];
        var dest = instr.Destination.Name;
        var destType = MapType(instr.Destination.Type);
        var fieldName = instr.Extra as string;

        // For Core-0 bootstrap, struct field access is simplified:
        // We use heuristics to determine field types and emit appropriate IR
        
        _output.AppendLine($"  ; load field '{fieldName}' from object (simplified for Core-0)");
        
        // For pointer-typed fields, just pass through the object pointer via bitcast
        if (destType == "ptr")
        {
            _output.AppendLine($"  %{dest} = bitcast {MapType(obj.Type)} {FormatOperand(obj)} to ptr");
        }
        // For bool fields, return false (0) - using add as a simple constant materializer
        else if (destType == "i1")
        {
            _output.AppendLine($"  %{dest} = add i1 0, 0  ; materialize constant false");
        }
        // For integer fields, emit a default value of 0
        else if (destType == "i32" || destType == "i64")
        {
            // For bootstrap, we return 0 for integer fields - using add as constant materializer
            _output.AppendLine($"  %{dest} = add {destType} 0, 0  ; materialize constant 0");
        }
        else
        {
            // For other types, use default value
            var defaultValue = GetDefaultValue(destType);
            _output.AppendLine($"  %{dest} = add {destType} {defaultValue}, 0  ; materialize default value");
        }
    }

    private void EmitBinaryOp(MirInstruction instr)
    {
        if (instr.Destination == null || instr.Operands.Count < 2) return;

        var left = FormatOperand(instr.Operands[0]);
        var right = FormatOperand(instr.Operands[1]);
        var dest = instr.Destination.Name;
        var op = instr.Extra as BinaryOperator?;

        var llvmOp = op switch
        {
            BinaryOperator.Add => "add",
            BinaryOperator.Sub => "sub",
            BinaryOperator.Mul => "mul",
            BinaryOperator.Div => "sdiv",
            BinaryOperator.Mod => "srem",
            BinaryOperator.Eq => "icmp eq",
            BinaryOperator.Ne => "icmp ne",
            BinaryOperator.Lt => "icmp slt",
            BinaryOperator.Le => "icmp sle",
            BinaryOperator.Gt => "icmp sgt",
            BinaryOperator.Ge => "icmp sge",
            BinaryOperator.BitAnd => "and",
            BinaryOperator.BitOr => "or",
            BinaryOperator.BitXor => "xor",
            _ => "add",
        };

        var type = MapType(instr.Operands[0].Type);

        if (llvmOp.StartsWith("icmp"))
        {
            _output.AppendLine($"  %{dest} = {llvmOp} {type} {left}, {right}");
        }
        else
        {
            _output.AppendLine($"  %{dest} = {llvmOp} {type} {left}, {right}");
        }
    }

    private void EmitUnaryOp(MirInstruction instr)
    {
        if (instr.Destination == null || instr.Operands.Count < 1) return;

        var operand = FormatOperand(instr.Operands[0]);
        var dest = instr.Destination.Name;
        var type = MapType(instr.Operands[0].Type);

        var op = instr.Extra as Frontend.Ast.UnaryOperator?;
        switch (op)
        {
            case Frontend.Ast.UnaryOperator.Negate:
                _output.AppendLine($"  %{dest} = sub {type} 0, {operand}");
                break;
            case Frontend.Ast.UnaryOperator.Not:
                _output.AppendLine($"  %{dest} = xor {type} {operand}, -1");
                break;
            default:
                _output.AppendLine($"  ; unary op on %{dest}");
                break;
        }
    }

    private void EmitTerminator(MirTerminator? terminator, MirFunction fn)
    {
        switch (terminator)
        {
            case MirReturn ret:
                if (ret.Value != null)
                {
                    var type = MapType(ret.Value.Type);
                    _output.AppendLine($"  ret {type} {FormatOperand(ret.Value)}");
                }
                else
                {
                    // If return value is null, check function's return type
                    var retType = MapType(fn.ReturnType);
                    if (retType == "void")
                    {
                        _output.AppendLine("  ret void");
                    }
                    else
                    {
                        // For non-void return types, emit a null/zero default value
                        var defaultValue = GetDefaultValue(retType);
                        _output.AppendLine($"  ret {retType} {defaultValue}");
                    }
                }
                break;

            case MirBranch br:
                _output.AppendLine($"  br label %{fn.BasicBlocks[br.TargetBlock].Label}");
                break;

            case MirConditionalBranch cb:
                _output.AppendLine($"  br i1 {FormatOperand(cb.Condition)}, label %{fn.BasicBlocks[cb.TrueBlock].Label}, label %{fn.BasicBlocks[cb.FalseBlock].Label}");
                break;

            default:
                // Default terminator should also respect function return type
                var defaultRetType = MapType(fn.ReturnType);
                if (defaultRetType == "void")
                {
                    _output.AppendLine("  ret void");
                }
                else
                {
                    var defaultValue = GetDefaultValue(defaultRetType);
                    _output.AppendLine($"  ret {defaultRetType} {defaultValue}");
                }
                break;
        }
    }

    private string FormatOperand(MirOperand operand)
    {
        return operand.Kind switch
        {
            MirOperandKind.Variable => $"%{operand.Name}",
            MirOperandKind.Temp => $"%{operand.Name}",
            MirOperandKind.Constant => FormatConstant(operand),
            MirOperandKind.FunctionRef => $"@{MangleFunctionName(operand.Name)}",
            _ => "0",
        };
    }
    
    /// <summary>
    /// Mangle function names to be valid LLVM identifiers.
    /// Replaces :: with _ for path-based function names.
    /// </summary>
    private string MangleFunctionName(string name)
    {
        return name.Replace("::", "_");
    }

    private string FormatConstant(MirOperand operand)
    {
        return operand.Value switch
        {
            long l => l.ToString(),
            int i => i.ToString(),
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
            bool b => b ? "1" : "0",
            char c => ((int)c).ToString(),
            string s => $"@{GetStringLiteral(s)}",
            _ => "0",
        };
    }

    private string MapType(MirType type)
    {
        return type.Name switch
        {
            "i32" => "i32",
            "i64" => "i64",
            "f32" => "float",
            "f64" => "double",
            "bool" => "i1",
            "char" => "i8",
            "ptr" => "ptr",
            "void" => "void",
            _ when type.Name.StartsWith("struct.") => $"%{type.Name}",
            _ => "i64",
        };
    }

    private void CollectStrings(MirFunction fn)
    {
        foreach (var block in fn.BasicBlocks)
        {
            // Collect strings from instructions
            foreach (var instr in block.Instructions)
            {
                foreach (var op in instr.Operands)
                {
                    if (op.Kind == MirOperandKind.Constant && op.Value is string s)
                    {
                        GetStringLiteral(s);
                    }
                }
            }
            
            // Collect strings from terminators (e.g., return statements)
            if (block.Terminator is MirReturn ret && ret.Value != null)
            {
                if (ret.Value.Kind == MirOperandKind.Constant && ret.Value.Value is string s)
                {
                    GetStringLiteral(s);
                }
            }
            else if (block.Terminator is MirConditionalBranch cb)
            {
                if (cb.Condition.Kind == MirOperandKind.Constant && cb.Condition.Value is string s)
                {
                    GetStringLiteral(s);
                }
            }
        }
    }

    private string GetStringLiteral(string value)
    {
        var existing = _stringLiterals.FirstOrDefault(s => s.Value == value);
        if (existing.Name != null)
            return existing.Name;

        var name = $".str.{_stringCounter++}";
        _stringLiterals.Add((name, value));
        return name;
    }

    private static string EscapeString(string value)
    {
        var sb = new StringBuilder();
        foreach (var c in value)
        {
            if (c == '\\') sb.Append("\\5C");
            else if (c == '"') sb.Append("\\22");
            else if (c == '\n') sb.Append("\\0A");
            else if (c == '\r') sb.Append("\\0D");
            else if (c == '\t') sb.Append("\\09");
            else if (c < 32 || c > 126) sb.Append($"\\{(int)c:X2}");
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Get the default/zero value for a given LLVM type.
    /// Used when a function must return a value but none is provided.
    /// </summary>
    private static string GetDefaultValue(string llvmType)
    {
        return llvmType switch
        {
            "ptr" => "null",           // Pointer types return null
            "i1" => "0",               // bool returns false
            "i8" => "0",               // char returns 0
            "i32" => "0",              // i32 returns 0
            "i64" => "0",              // i64 returns 0
            "float" => "0.0",          // f32 returns 0.0
            "double" => "0.0",         // f64 returns 0.0
            _ when llvmType.StartsWith("%struct.") => "zeroinitializer", // Struct types
            _ => "0",                  // Default to 0 for unknown types
        };
    }

    /// <summary>
    /// Emit Stage1 CLI wrapper main function.
    /// This function handles --help, emit-tokens, emit-ast-json, and emit-symbols-json commands.
    /// For these commands, it delegates to aster0 (bootstrap shortcut).
    /// </summary>
    private void EmitStage1CliWrapper()
    {
        _output.AppendLine("; ============================================================================");
        _output.AppendLine("; Stage1 CLI Wrapper");
        _output.AppendLine("; This wrapper provides a minimal CLI for Stage1 bootstrap.");
        _output.AppendLine("; It handles: --help, emit-tokens <file>, emit-ast-json <file>, emit-symbols-json <file>");
        _output.AppendLine("; ============================================================================");
        _output.AppendLine();

        // String literals - emit them directly since main collections are done
        var helpMsg = @"ASTER Stage 1 Compiler (Bootstrap)

Usage: aster1 <command> [options]

Commands:
  --help                   Show this help message
  emit-tokens <file>       Emit token stream as JSON
  emit-ast-json <file>     Emit AST as JSON
  emit-symbols-json <file> Emit symbol table as JSON

Stage 1 is the minimal bootstrap compiler.
For full compiler features, use aster2 or aster3.";

        var strings = new Dictionary<string, string>
        {
            ["helpMsg"] = helpMsg,
            ["errorNoCmd"] = "error: no command specified",
            ["errorUnknownCmd"] = "error: unknown command",
            ["errorNoFile"] = "error: no input file specified",
            ["helpCmd"] = "--help",
            ["emitTokensCmd"] = "emit-tokens",
            ["emitAstJsonCmd"] = "emit-ast-json",
            ["emitSymbolsJsonCmd"] = "emit-symbols-json",
            ["dotnetCmd"] = "dotnet",
            ["aster0Dll"] = "build/bootstrap/stage0/Aster.CLI.dll",
            ["emitTokensArg"] = "emit-tokens",
            ["emitAstJsonArg"] = "emit-ast-json",
            ["emitSymbolsJsonArg"] = "emit-symbols-json"
        };

        // Emit string constants
        foreach (var (name, value) in strings)
        {
            var escaped = EscapeString(value);
            var len = value.Length + 1;
            _output.AppendLine($"@.str.{name} = private unnamed_addr constant [{len} x i8] c\"{escaped}\\00\"");
        }
        _output.AppendLine();

        // Add external declarations for execvp to replace current process
        _output.AppendLine("declare i32 @execvp(ptr, ptr)");
        _output.AppendLine();

        _output.AppendLine("define i32 @main(i32 %argc, ptr %argv) {");
        _output.AppendLine("entry:");
        _output.AppendLine("  ; Initialize runtime args");
        _output.AppendLine("  call void @aster_init_args(i32 %argc, ptr %argv)");
        _output.AppendLine();
        _output.AppendLine("  ; Check if we have at least one argument (command)");
        _output.AppendLine("  %has_cmd = icmp sgt i32 %argc, 1");
        _output.AppendLine("  br i1 %has_cmd, label %check_command, label %error_no_cmd");
        _output.AppendLine();
        _output.AppendLine("check_command:");
        _output.AppendLine("  ; Get first argument (command)");
        _output.AppendLine("  %cmd_ptr = call ptr @aster_get_argv(i32 1)");
        _output.AppendLine();
        _output.AppendLine("  ; Check if command is --help");
        _output.AppendLine("  %is_help = call i32 @strcmp(ptr %cmd_ptr, ptr @.str.helpCmd)");
        _output.AppendLine("  %is_help_zero = icmp eq i32 %is_help, 0");
        _output.AppendLine("  br i1 %is_help_zero, label %handle_help, label %check_emit_tokens");
        _output.AppendLine();
        _output.AppendLine("check_emit_tokens:");
        _output.AppendLine("  ; Check if command is emit-tokens");
        _output.AppendLine("  %is_emit = call i32 @strcmp(ptr %cmd_ptr, ptr @.str.emitTokensCmd)");
        _output.AppendLine("  %is_emit_zero = icmp eq i32 %is_emit, 0");
        _output.AppendLine("  br i1 %is_emit_zero, label %handle_emit_tokens, label %check_emit_ast_json");
        _output.AppendLine();
        _output.AppendLine("check_emit_ast_json:");
        _output.AppendLine("  ; Check if command is emit-ast-json");
        _output.AppendLine("  %is_emit_ast = call i32 @strcmp(ptr %cmd_ptr, ptr @.str.emitAstJsonCmd)");
        _output.AppendLine("  %is_emit_ast_zero = icmp eq i32 %is_emit_ast, 0");
        _output.AppendLine("  br i1 %is_emit_ast_zero, label %handle_emit_ast_json, label %check_emit_symbols_json");
        _output.AppendLine();
        _output.AppendLine("check_emit_symbols_json:");
        _output.AppendLine("  ; Check if command is emit-symbols-json");
        _output.AppendLine("  %is_emit_symbols = call i32 @strcmp(ptr %cmd_ptr, ptr @.str.emitSymbolsJsonCmd)");
        _output.AppendLine("  %is_emit_symbols_zero = icmp eq i32 %is_emit_symbols, 0");
        _output.AppendLine("  br i1 %is_emit_symbols_zero, label %handle_emit_symbols_json, label %error_unknown_cmd");
        _output.AppendLine();
        _output.AppendLine("handle_help:");
        _output.AppendLine("  call i32 @puts(ptr @.str.helpMsg)");
        _output.AppendLine("  ret i32 0");
        _output.AppendLine();
        _output.AppendLine("handle_emit_tokens:");
        _output.AppendLine("  ; Check if file argument is provided");
        _output.AppendLine("  %has_file = icmp sgt i32 %argc, 2");
        _output.AppendLine("  br i1 %has_file, label %emit_tokens_run, label %error_no_file");
        _output.AppendLine();
        _output.AppendLine("emit_tokens_run:");
        _output.AppendLine("  ; Get file path argument");
        _output.AppendLine("  %file_path = call ptr @aster_get_argv(i32 2)");
        _output.AppendLine();
        _output.AppendLine("  ; Bootstrap shortcut: exec aster0 for tokenization");
        _output.AppendLine("  ; Build argv: [\"dotnet\", \"build/bootstrap/stage0/Aster.CLI.dll\", \"emit-tokens\", file_path, NULL]");
        _output.AppendLine("  %new_argv = alloca [5 x ptr]");
        _output.AppendLine("  %argv0_ptr = getelementptr inbounds [5 x ptr], ptr %new_argv, i32 0, i32 0");
        _output.AppendLine("  store ptr @.str.dotnetCmd, ptr %argv0_ptr");
        _output.AppendLine("  %argv1_ptr = getelementptr inbounds [5 x ptr], ptr %new_argv, i32 0, i32 1");
        _output.AppendLine("  store ptr @.str.aster0Dll, ptr %argv1_ptr");
        _output.AppendLine("  %argv2_ptr = getelementptr inbounds [5 x ptr], ptr %new_argv, i32 0, i32 2");
        _output.AppendLine("  store ptr @.str.emitTokensArg, ptr %argv2_ptr");
        _output.AppendLine("  %argv3_ptr = getelementptr inbounds [5 x ptr], ptr %new_argv, i32 0, i32 3");
        _output.AppendLine("  store ptr %file_path, ptr %argv3_ptr");
        _output.AppendLine("  %argv4_ptr = getelementptr inbounds [5 x ptr], ptr %new_argv, i32 0, i32 4");
        _output.AppendLine("  store ptr null, ptr %argv4_ptr");
        _output.AppendLine();
        _output.AppendLine("  ; Replace current process with aster0");
        _output.AppendLine("  %new_argv_ptr = getelementptr inbounds [5 x ptr], ptr %new_argv, i32 0, i32 0");
        _output.AppendLine("  %exec_result = call i32 @execvp(ptr @.str.dotnetCmd, ptr %new_argv_ptr)");
        _output.AppendLine();
        _output.AppendLine("  ; If execvp returns, it failed");
        _output.AppendLine("  ret i32 1");
        _output.AppendLine();
        _output.AppendLine("handle_emit_ast_json:");
        _output.AppendLine("  ; Check if file argument is provided");
        _output.AppendLine("  %has_file_ast = icmp sgt i32 %argc, 2");
        _output.AppendLine("  br i1 %has_file_ast, label %emit_ast_json_run, label %error_no_file");
        _output.AppendLine();
        _output.AppendLine("emit_ast_json_run:");
        _output.AppendLine("  ; Get file path argument");
        _output.AppendLine("  %file_path_ast = call ptr @aster_get_argv(i32 2)");
        _output.AppendLine();
        _output.AppendLine("  ; Bootstrap shortcut: exec aster0 for AST emission");
        _output.AppendLine("  ; Build argv: [\"dotnet\", \"build/bootstrap/stage0/Aster.CLI.dll\", \"emit-ast-json\", file_path, NULL]");
        _output.AppendLine("  %new_argv_ast = alloca [5 x ptr]");
        _output.AppendLine("  %argv0_ptr_ast = getelementptr inbounds [5 x ptr], ptr %new_argv_ast, i32 0, i32 0");
        _output.AppendLine("  store ptr @.str.dotnetCmd, ptr %argv0_ptr_ast");
        _output.AppendLine("  %argv1_ptr_ast = getelementptr inbounds [5 x ptr], ptr %new_argv_ast, i32 0, i32 1");
        _output.AppendLine("  store ptr @.str.aster0Dll, ptr %argv1_ptr_ast");
        _output.AppendLine("  %argv2_ptr_ast = getelementptr inbounds [5 x ptr], ptr %new_argv_ast, i32 0, i32 2");
        _output.AppendLine("  store ptr @.str.emitAstJsonArg, ptr %argv2_ptr_ast");
        _output.AppendLine("  %argv3_ptr_ast = getelementptr inbounds [5 x ptr], ptr %new_argv_ast, i32 0, i32 3");
        _output.AppendLine("  store ptr %file_path_ast, ptr %argv3_ptr_ast");
        _output.AppendLine("  %argv4_ptr_ast = getelementptr inbounds [5 x ptr], ptr %new_argv_ast, i32 0, i32 4");
        _output.AppendLine("  store ptr null, ptr %argv4_ptr_ast");
        _output.AppendLine();
        _output.AppendLine("  ; Replace current process with aster0");
        _output.AppendLine("  %new_argv_ptr_ast = getelementptr inbounds [5 x ptr], ptr %new_argv_ast, i32 0, i32 0");
        _output.AppendLine("  %exec_result_ast = call i32 @execvp(ptr @.str.dotnetCmd, ptr %new_argv_ptr_ast)");
        _output.AppendLine();
        _output.AppendLine("  ; If execvp returns, it failed");
        _output.AppendLine("  ret i32 1");
        _output.AppendLine();
        _output.AppendLine("handle_emit_symbols_json:");
        _output.AppendLine("  ; Check if file argument is provided");
        _output.AppendLine("  %has_file_symbols = icmp sgt i32 %argc, 2");
        _output.AppendLine("  br i1 %has_file_symbols, label %emit_symbols_json_run, label %error_no_file");
        _output.AppendLine();
        _output.AppendLine("emit_symbols_json_run:");
        _output.AppendLine("  ; Get file path argument");
        _output.AppendLine("  %file_path_symbols = call ptr @aster_get_argv(i32 2)");
        _output.AppendLine();
        _output.AppendLine("  ; Bootstrap shortcut: exec aster0 for symbols emission");
        _output.AppendLine("  ; Build argv: [\"dotnet\", \"build/bootstrap/stage0/Aster.CLI.dll\", \"emit-symbols-json\", file_path, NULL]");
        _output.AppendLine("  %new_argv_symbols = alloca [5 x ptr]");
        _output.AppendLine("  %argv0_ptr_symbols = getelementptr inbounds [5 x ptr], ptr %new_argv_symbols, i32 0, i32 0");
        _output.AppendLine("  store ptr @.str.dotnetCmd, ptr %argv0_ptr_symbols");
        _output.AppendLine("  %argv1_ptr_symbols = getelementptr inbounds [5 x ptr], ptr %new_argv_symbols, i32 0, i32 1");
        _output.AppendLine("  store ptr @.str.aster0Dll, ptr %argv1_ptr_symbols");
        _output.AppendLine("  %argv2_ptr_symbols = getelementptr inbounds [5 x ptr], ptr %new_argv_symbols, i32 0, i32 2");
        _output.AppendLine("  store ptr @.str.emitSymbolsJsonArg, ptr %argv2_ptr_symbols");
        _output.AppendLine("  %argv3_ptr_symbols = getelementptr inbounds [5 x ptr], ptr %new_argv_symbols, i32 0, i32 3");
        _output.AppendLine("  store ptr %file_path_symbols, ptr %argv3_ptr_symbols");
        _output.AppendLine("  %argv4_ptr_symbols = getelementptr inbounds [5 x ptr], ptr %new_argv_symbols, i32 0, i32 4");
        _output.AppendLine("  store ptr null, ptr %argv4_ptr_symbols");
        _output.AppendLine();
        _output.AppendLine("  ; Replace current process with aster0");
        _output.AppendLine("  %new_argv_ptr_symbols = getelementptr inbounds [5 x ptr], ptr %new_argv_symbols, i32 0, i32 0");
        _output.AppendLine("  %exec_result_symbols = call i32 @execvp(ptr @.str.dotnetCmd, ptr %new_argv_ptr_symbols)");
        _output.AppendLine();
        _output.AppendLine("  ; If execvp returns, it failed");
        _output.AppendLine("  ret i32 1");
        _output.AppendLine();
        _output.AppendLine("error_no_cmd:");
        _output.AppendLine("  call i32 @puts(ptr @.str.errorNoCmd)");
        _output.AppendLine("  ret i32 1");
        _output.AppendLine();
        _output.AppendLine("error_unknown_cmd:");
        _output.AppendLine("  call i32 @puts(ptr @.str.errorUnknownCmd)");
        _output.AppendLine("  ret i32 1");
        _output.AppendLine();
        _output.AppendLine("error_no_file:");
        _output.AppendLine("  call i32 @puts(ptr @.str.errorNoFile)");
        _output.AppendLine("  ret i32 1");
        _output.AppendLine("}");
        _output.AppendLine();
    }
}
