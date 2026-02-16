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

        // Emit functions
        foreach (var fn in module.Functions)
        {
            EmitFunction(fn);
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
        _output.AppendLine();
    }

    private void EmitFunction(MirFunction fn)
    {
        var retType = MapType(fn.ReturnType);
        var paramList = string.Join(", ", fn.Parameters.Select(p => $"{MapType(p.Type)} %{p.Name}"));

        _output.AppendLine($"define {retType} @{fn.Name}({paramList}) {{");

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

        // General function call
        var args = new List<string>();
        for (int i = 1; i < instr.Operands.Count; i++)
        {
            var op = instr.Operands[i];
            args.Add($"{MapType(op.Type)} {FormatOperand(op)}");
        }

        var retType = instr.Destination != null ? MapType(instr.Destination.Type) : "void";
        var argStr = string.Join(", ", args);
        var calleeName = MangleFunctionName(callee.Name);

        if (retType == "void")
        {
            _output.AppendLine($"  call void @{calleeName}({argStr})");
        }
        else
        {
            _output.AppendLine($"  %{instr.Destination!.Name} = call {retType} @{calleeName}({argStr})");
        }
    }

    private void EmitLoad(MirInstruction instr)
    {
        if (instr.Destination == null || instr.Operands.Count == 0) return;

        var obj = instr.Operands[0];
        var dest = instr.Destination.Name;
        var destType = MapType(instr.Destination.Type);
        var fieldName = instr.Extra as string;

        // For now, we'll emit a simplified load that just reads from the object
        // In a full implementation, this would need to calculate field offsets
        // For struct field access, we emit a getelementptr + load
        // But for Core-0 bootstrap, we'll use a simpler approach
        
        // If the object is a struct pointer and we're accessing a field,
        // we need to use getelementptr to get the field address, then load it
        // For simplicity in Core-0, we'll just emit a comment and use a default value
        // This is a known limitation for the bootstrap phase
        
        _output.AppendLine($"  ; load {fieldName} from {FormatOperand(obj)}");
        _output.AppendLine($"  %{dest} = load {destType}, ptr {FormatOperand(obj)}");
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
}
