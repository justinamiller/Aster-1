namespace Aster.Compiler.Frontend.TypeSystem;

/// <summary>
/// Base class for all types in the Aster type system.
/// </summary>
public abstract class AsterType
{
    public abstract string DisplayName { get; }
    public override string ToString() => DisplayName;
}

/// <summary>Primitive built-in types.</summary>
public sealed class PrimitiveType : AsterType
{
    public PrimitiveKind Kind { get; }
    public override string DisplayName => Kind.ToString().ToLowerInvariant();

    public PrimitiveType(PrimitiveKind kind) => Kind = kind;

    public static readonly PrimitiveType I8 = new(PrimitiveKind.I8);
    public static readonly PrimitiveType I16 = new(PrimitiveKind.I16);
    public static readonly PrimitiveType I32 = new(PrimitiveKind.I32);
    public static readonly PrimitiveType I64 = new(PrimitiveKind.I64);
    public static readonly PrimitiveType U8 = new(PrimitiveKind.U8);
    public static readonly PrimitiveType U16 = new(PrimitiveKind.U16);
    public static readonly PrimitiveType U32 = new(PrimitiveKind.U32);
    public static readonly PrimitiveType U64 = new(PrimitiveKind.U64);
    public static readonly PrimitiveType F32 = new(PrimitiveKind.F32);
    public static readonly PrimitiveType F64 = new(PrimitiveKind.F64);
    public static readonly PrimitiveType Bool = new(PrimitiveKind.Bool);
    public static readonly PrimitiveType Char = new(PrimitiveKind.Char);
    public static readonly PrimitiveType StringType = new(PrimitiveKind.String);
    public static readonly PrimitiveType Void = new(PrimitiveKind.Void);
}

public enum PrimitiveKind
{
    I8, I16, I32, I64,
    U8, U16, U32, U64,
    F32, F64,
    Bool, Char, String, Void,
}

/// <summary>Struct type with named fields.</summary>
public sealed class StructType : AsterType
{
    public string Name { get; }
    public IReadOnlyList<(string Name, AsterType Type)> Fields { get; }
    public override string DisplayName => Name;

    public StructType(string name, IReadOnlyList<(string, AsterType)> fields)
    { Name = name; Fields = fields; }
}

/// <summary>Enum type with variants.</summary>
public sealed class EnumType : AsterType
{
    public string Name { get; }
    public IReadOnlyList<(string Name, IReadOnlyList<AsterType> Fields)> Variants { get; }
    public override string DisplayName => Name;

    public EnumType(string name, IReadOnlyList<(string, IReadOnlyList<AsterType>)> variants)
    { Name = name; Variants = variants; }
}

/// <summary>Trait type.</summary>
public sealed class TraitType : AsterType
{
    public string Name { get; }
    public override string DisplayName => Name;

    public TraitType(string name) => Name = name;
}

/// <summary>Type variable for inference.</summary>
public sealed class TypeVariable : AsterType
{
    public int Id { get; }
    public override string DisplayName => $"?T{Id}";

    private static int _nextId;
    public TypeVariable() => Id = Interlocked.Increment(ref _nextId);
    public TypeVariable(int id) => Id = id;
}

/// <summary>Function type.</summary>
public sealed class FunctionType : AsterType
{
    public IReadOnlyList<AsterType> ParameterTypes { get; }
    public AsterType ReturnType { get; }
    public override string DisplayName => $"fn({string.Join(", ", ParameterTypes.Select(p => p.DisplayName))}) -> {ReturnType.DisplayName}";

    public FunctionType(IReadOnlyList<AsterType> parameterTypes, AsterType returnType)
    { ParameterTypes = parameterTypes; ReturnType = returnType; }
}

/// <summary>Reference type (borrowed).</summary>
public sealed class ReferenceType : AsterType
{
    public AsterType Inner { get; }
    public bool IsMutable { get; }
    public override string DisplayName => IsMutable ? $"&mut {Inner.DisplayName}" : $"&{Inner.DisplayName}";

    public ReferenceType(AsterType inner, bool isMutable) { Inner = inner; IsMutable = isMutable; }
}

/// <summary>An error/unknown type used for error recovery.</summary>
public sealed class ErrorType : AsterType
{
    public static readonly ErrorType Instance = new();
    public override string DisplayName => "<error>";
}
