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

/// <summary>Generic type parameter.</summary>
public sealed class GenericParameter : AsterType
{
    public string Name { get; }
    public int Id { get; }
    public IReadOnlyList<TraitBound> Bounds { get; }
    public override string DisplayName => Name;

    public GenericParameter(string name, int id, IReadOnlyList<TraitBound>? bounds = null)
    {
        Name = name;
        Id = id;
        Bounds = bounds ?? Array.Empty<TraitBound>();
    }
}

/// <summary>Type application - applying type arguments to a generic type.</summary>
public sealed class TypeApp : AsterType
{
    public AsterType Constructor { get; }
    public IReadOnlyList<AsterType> Arguments { get; }
    public override string DisplayName => $"{Constructor.DisplayName}<{string.Join(", ", Arguments.Select(a => a.DisplayName))}>";

    public TypeApp(AsterType constructor, IReadOnlyList<AsterType> arguments)
    {
        Constructor = constructor;
        Arguments = arguments;
    }
}

/// <summary>Type alias — a named alias for another type.</summary>
public sealed class TypeAlias : AsterType
{
    public string Name { get; }
    public AsterType Underlying { get; }
    public override string DisplayName => Name;
    public TypeAlias(string name, AsterType underlying) { Name = name; Underlying = underlying; }
}

/// <summary>
/// Phase 4: Lifetime annotation type (e.g. 'a, 'static).
/// Currently used only for parsing / source-level tracking; lifetime inference
/// is not yet performed — lifetimes are erased before type checking.
/// </summary>
public sealed class LifetimeType : AsterType
{
    public string Name { get; }
    public override string DisplayName => $"'{Name}";
    public LifetimeType(string name) => Name = name;
}

/// <summary>
/// Phase 4: Trait object type — `dyn TraitName`.
/// Used to represent dynamically-dispatched trait values.
/// Method calls on dyn types are dispatched via the impl method table.
/// </summary>
public sealed class TraitObjectType : AsterType
{
    /// <summary>The trait this object must implement.</summary>
    public string TraitName { get; }
    public override string DisplayName => $"dyn {TraitName}";
    public TraitObjectType(string traitName) => TraitName = traitName;
}

/// <summary>Phase 6: Slice type — dynamically-sized view [T].</summary>
public sealed class SliceType : AsterType
{
    public AsterType ElementType { get; }
    public override string DisplayName => $"[{ElementType.DisplayName}]";
    public SliceType(AsterType elementType) => ElementType = elementType;
}

/// <summary>Phase 6: Fixed-size array type [T; N].</summary>
public sealed class ArrayType : AsterType
{
    public AsterType ElementType { get; }
    public int Length { get; }
    public override string DisplayName => $"[{ElementType.DisplayName}; {Length}]";
    public ArrayType(AsterType elementType, int length) { ElementType = elementType; Length = length; }
}

/// <summary>Phase 6: String slice type &amp;str (distinct from owned String).</summary>
public sealed class StrType : AsterType
{
    public override string DisplayName => "str";
    public static readonly StrType Instance = new();
    private StrType() { }
}

/// <summary>Trait bound on a type parameter.</summary>
public sealed class TraitBound
{
    public string TraitName { get; }
    public IReadOnlyList<AsterType> AssociatedTypes { get; }

    public TraitBound(string traitName, IReadOnlyList<AsterType>? associatedTypes = null)
    {
        TraitName = traitName;
        AssociatedTypes = associatedTypes ?? Array.Empty<AsterType>();
    }

    public override string ToString() => TraitName;
}

/// <summary>Type scheme for let-polymorphism.</summary>
public sealed class TypeScheme
{
    public IReadOnlyList<GenericParameter> TypeParameters { get; }
    public IReadOnlyList<TraitBound> Constraints { get; }
    public AsterType Body { get; }

    public TypeScheme(
        IReadOnlyList<GenericParameter> typeParameters,
        IReadOnlyList<TraitBound> constraints,
        AsterType body)
    {
        TypeParameters = typeParameters;
        Constraints = constraints;
        Body = body;
    }

    /// <summary>Instantiate this scheme with fresh type variables.</summary>
    public AsterType Instantiate(Dictionary<int, AsterType> substitution)
    {
        foreach (var param in TypeParameters)
        {
            substitution[param.Id] = new TypeVariable();
        }
        return Substitute(Body, substitution);
    }

    private AsterType Substitute(AsterType type, Dictionary<int, AsterType> substitution)
    {
        return type switch
        {
            GenericParameter gp when substitution.TryGetValue(gp.Id, out var sub) => sub,
            FunctionType ft => new FunctionType(
                ft.ParameterTypes.Select(p => Substitute(p, substitution)).ToList(),
                Substitute(ft.ReturnType, substitution)),
            ReferenceType rt => new ReferenceType(Substitute(rt.Inner, substitution), rt.IsMutable),
            TypeApp ta => new TypeApp(
                Substitute(ta.Constructor, substitution),
                ta.Arguments.Select(a => Substitute(a, substitution)).ToList()),
            _ => type
        };
    }

    public override string ToString()
    {
        if (TypeParameters.Count == 0)
            return Body.DisplayName;
        return $"∀{string.Join(", ", TypeParameters.Select(p => p.Name))}. {Body.DisplayName}";
    }
}
