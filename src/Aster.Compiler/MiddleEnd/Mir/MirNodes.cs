using Aster.Compiler.Diagnostics;

namespace Aster.Compiler.MiddleEnd.Mir;

/// <summary>
/// MIR (Mid-level Intermediate Representation) module.
/// SSA-based representation with explicit drops and control flow.
/// </summary>
public sealed class MirModule
{
    public string Name { get; }
    public List<MirFunction> Functions { get; } = new();
    public MirModule(string name) => Name = name;
}

/// <summary>MIR function.</summary>
public sealed class MirFunction
{
    public string Name { get; }
    public List<MirParameter> Parameters { get; } = new();
    public List<MirBasicBlock> BasicBlocks { get; } = new();
    public MirType ReturnType { get; set; } = MirType.Void;

    public MirFunction(string name) => Name = name;

    /// <summary>Create a new basic block.</summary>
    public MirBasicBlock CreateBlock(string label)
    {
        var block = new MirBasicBlock(label, BasicBlocks.Count);
        BasicBlocks.Add(block);
        return block;
    }
}

/// <summary>MIR function parameter.</summary>
public sealed class MirParameter
{
    public string Name { get; }
    public MirType Type { get; }
    public int Index { get; }
    public MirParameter(string name, MirType type, int index) { Name = name; Type = type; Index = index; }
}

/// <summary>MIR basic block (node in CFG).</summary>
public sealed class MirBasicBlock
{
    public string Label { get; }
    public int Index { get; }
    public List<MirInstruction> Instructions { get; } = new();
    public MirTerminator? Terminator { get; set; }

    public MirBasicBlock(string label, int index) { Label = label; Index = index; }

    public void AddInstruction(MirInstruction instruction) => Instructions.Add(instruction);
}

/// <summary>MIR instruction opcodes.</summary>
public enum MirOpcode
{
    Assign,
    Move,
    Borrow,
    Load,
    Store,
    Call,
    Drop,
    BinaryOp,
    UnaryOp,
    Literal,
}

/// <summary>MIR instruction.</summary>
public sealed class MirInstruction
{
    public MirOpcode Opcode { get; }
    public MirOperand? Destination { get; }
    public IReadOnlyList<MirOperand> Operands { get; }
    public object? Extra { get; }

    public MirInstruction(MirOpcode opcode, MirOperand? destination, IReadOnlyList<MirOperand> operands, object? extra = null)
    {
        Opcode = opcode; Destination = destination; Operands = operands; Extra = extra;
    }
}

/// <summary>MIR operand (variable, literal, or temporary).</summary>
public sealed class MirOperand
{
    public MirOperandKind Kind { get; }
    public string Name { get; }
    public MirType Type { get; }
    public object? Value { get; }

    private MirOperand(MirOperandKind kind, string name, MirType type, object? value = null)
    { Kind = kind; Name = name; Type = type; Value = value; }

    public static MirOperand Variable(string name, MirType type) => new(MirOperandKind.Variable, name, type);
    public static MirOperand Temp(string name, MirType type) => new(MirOperandKind.Temp, name, type);
    public static MirOperand Constant(object value, MirType type) => new(MirOperandKind.Constant, "", type, value);
    public static MirOperand FunctionRef(string name) => new(MirOperandKind.FunctionRef, name, MirType.Void);
}

public enum MirOperandKind
{
    Variable,
    Temp,
    Constant,
    FunctionRef,
}

/// <summary>Block terminator (control flow).</summary>
public abstract class MirTerminator { }

/// <summary>Return from function.</summary>
public sealed class MirReturn : MirTerminator
{
    public MirOperand? Value { get; }
    public MirReturn(MirOperand? value = null) => Value = value;
}

/// <summary>Unconditional branch.</summary>
public sealed class MirBranch : MirTerminator
{
    public int TargetBlock { get; }
    public MirBranch(int targetBlock) => TargetBlock = targetBlock;
}

/// <summary>Conditional branch.</summary>
public sealed class MirConditionalBranch : MirTerminator
{
    public MirOperand Condition { get; }
    public int TrueBlock { get; }
    public int FalseBlock { get; }
    public MirConditionalBranch(MirOperand condition, int trueBlock, int falseBlock)
    { Condition = condition; TrueBlock = trueBlock; FalseBlock = falseBlock; }
}

/// <summary>Switch terminator.</summary>
public sealed class MirSwitch : MirTerminator
{
    public MirOperand Scrutinee { get; }
    public IReadOnlyList<(object Value, int Block)> Cases { get; }
    public int DefaultBlock { get; }
    public MirSwitch(MirOperand scrutinee, IReadOnlyList<(object, int)> cases, int defaultBlock)
    { Scrutinee = scrutinee; Cases = cases; DefaultBlock = defaultBlock; }
}

/// <summary>MIR types.</summary>
public sealed class MirType
{
    public string Name { get; }
    private MirType(string name) => Name = name;

    public static readonly MirType I32 = new("i32");
    public static readonly MirType I64 = new("i64");
    public static readonly MirType F32 = new("f32");
    public static readonly MirType F64 = new("f64");
    public static readonly MirType Bool = new("bool");
    public static readonly MirType Char = new("char");
    public static readonly MirType StringPtr = new("ptr");
    public static readonly MirType Void = new("void");
    public static readonly MirType Ptr = new("ptr");

    public static MirType Struct(string name) => new($"struct.{name}");

    public override string ToString() => Name;
}
