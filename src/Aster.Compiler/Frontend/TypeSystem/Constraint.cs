using Aster.Compiler.Diagnostics;

namespace Aster.Compiler.Frontend.TypeSystem;

/// <summary>
/// Represents a type constraint generated during type inference.
/// </summary>
public abstract class Constraint
{
    public Span Span { get; }
    protected Constraint(Span span) => Span = span;
}

/// <summary>Equality constraint: T1 = T2</summary>
public sealed class EqualityConstraint : Constraint
{
    public AsterType Left { get; }
    public AsterType Right { get; }

    public EqualityConstraint(AsterType left, AsterType right, Span span) : base(span)
    {
        Left = left;
        Right = right;
    }

    public override string ToString() => $"{Left.DisplayName} = {Right.DisplayName}";
}

/// <summary>Trait constraint: T: Trait</summary>
public sealed class TraitConstraint : Constraint
{
    public AsterType Type { get; }
    public TraitBound Bound { get; }

    public TraitConstraint(AsterType type, TraitBound bound, Span span) : base(span)
    {
        Type = type;
        Bound = bound;
    }

    public override string ToString() => $"{Type.DisplayName}: {Bound}";
}

/// <summary>
/// Constraint solver that performs unification and trait resolution.
/// </summary>
public sealed class ConstraintSolver
{
    private readonly Dictionary<int, AsterType> _substitutions = new();
    private readonly List<Constraint> _constraints = new();
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>Add a constraint to be solved.</summary>
    public void AddConstraint(Constraint constraint)
    {
        _constraints.Add(constraint);
    }

    /// <summary>Solve all accumulated constraints.</summary>
    public bool Solve()
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 0; i < _constraints.Count; i++)
            {
                var constraint = _constraints[i];
                if (constraint is EqualityConstraint eq)
                {
                    if (SolveEquality(eq))
                    {
                        _constraints.RemoveAt(i);
                        i--;
                        changed = true;
                    }
                }
                else if (constraint is TraitConstraint tc)
                {
                    // Trait constraints are checked later
                }
            }
        }

        return Diagnostics.HasErrors ? false : true;
    }

    private bool SolveEquality(EqualityConstraint constraint)
    {
        var left = Resolve(constraint.Left);
        var right = Resolve(constraint.Right);

        if (!Unify(left, right))
        {
            Diagnostics.ReportError(
                "E0310",
                $"Cannot unify types '{left.DisplayName}' and '{right.DisplayName}'",
                constraint.Span);
            return true; // Remove constraint even on failure
        }

        return true;
    }

    /// <summary>Unify two types, recording substitutions.</summary>
    public bool Unify(AsterType a, AsterType b)
    {
        a = Resolve(a);
        b = Resolve(b);

        if (ReferenceEquals(a, b))
            return true;

        if (a is TypeVariable tv1)
        {
            if (OccursCheck(tv1, b))
            {
                Diagnostics.ReportError("E0311", $"Occurs check failed: infinite type detected", Span.Unknown);
                return false;
            }
            _substitutions[tv1.Id] = b;
            return true;
        }

        if (b is TypeVariable tv2)
        {
            if (OccursCheck(tv2, a))
            {
                Diagnostics.ReportError("E0311", $"Occurs check failed: infinite type detected", Span.Unknown);
                return false;
            }
            _substitutions[tv2.Id] = a;
            return true;
        }

        // Generic type parameters implement unconstrained polymorphism: they can unify
        // with any concrete type. When constraint bounds are added in a future phase,
        // this is where bound checking (e.g. T: Ord) would be enforced.
        if (a is GenericParameter || b is GenericParameter)
            return true;

        if (a is PrimitiveType pa && b is PrimitiveType pb)
        {
            // Exact match
            if (pa.Kind == pb.Kind)
                return true;
            
            // Allow numeric type coercion (widening conversions only)
            return CanCoerce(pa, pb);
        }

        if (a is FunctionType fa && b is FunctionType fb)
        {
            if (fa.ParameterTypes.Count != fb.ParameterTypes.Count)
                return false;

            for (int i = 0; i < fa.ParameterTypes.Count; i++)
            {
                if (!Unify(fa.ParameterTypes[i], fb.ParameterTypes[i]))
                    return false;
            }

            return Unify(fa.ReturnType, fb.ReturnType);
        }

        if (a is ReferenceType ra && b is ReferenceType rb)
        {
            if (ra.IsMutable != rb.IsMutable)
                return false;
            return Unify(ra.Inner, rb.Inner);
        }

        if (a is TypeApp ta && b is TypeApp tb)
        {
            if (!Unify(ta.Constructor, tb.Constructor))
                return false;

            if (ta.Arguments.Count != tb.Arguments.Count)
                return false;

            for (int i = 0; i < ta.Arguments.Count; i++)
            {
                if (!Unify(ta.Arguments[i], tb.Arguments[i]))
                    return false;
            }

            return true;
        }

        if (a is StructType sa && b is StructType sb)
            return sa.Name == sb.Name;

        if (a is EnumType ea && b is EnumType eb)
            return ea.Name == eb.Name;

        // Phase 6: slice and array types
        if (a is SliceType sla && b is SliceType slb)
            return Unify(sla.ElementType, slb.ElementType);

        if (a is ArrayType ata && b is ArrayType atb)
            return ata.Length == atb.Length && Unify(ata.ElementType, atb.ElementType);

        if (a is StrType && b is StrType)
            return true;

        // Allow SliceType ↔ ArrayType coercion (array coerces to slice)
        if (a is ArrayType atc && b is SliceType slc)
            return Unify(atc.ElementType, slc.ElementType);
        if (a is SliceType sld && b is ArrayType atd)
            return Unify(sld.ElementType, atd.ElementType);

        // Allow StrType ↔ String coercion
        if (a is StrType && b is PrimitiveType pb2 && pb2.Kind == PrimitiveKind.String)
            return true;
        if (b is StrType && a is PrimitiveType pa2 && pa2.Kind == PrimitiveKind.String)
            return true;

        return false;
    }

    /// <summary>Resolve a type through substitutions.</summary>
    public AsterType Resolve(AsterType type)
    {
        while (type is TypeVariable tv && _substitutions.TryGetValue(tv.Id, out var resolved))
            type = resolved;
        return type;
    }

    /// <summary>Occurs check to prevent infinite types.</summary>
    private bool OccursCheck(TypeVariable tv, AsterType type)
    {
        type = Resolve(type);

        if (type is TypeVariable other && other.Id == tv.Id)
            return true;

        if (type is FunctionType fn)
        {
            return fn.ParameterTypes.Any(p => OccursCheck(tv, p)) || OccursCheck(tv, fn.ReturnType);
        }

        if (type is ReferenceType rf)
            return OccursCheck(tv, rf.Inner);

        if (type is TypeApp ta)
        {
            return OccursCheck(tv, ta.Constructor) || ta.Arguments.Any(a => OccursCheck(tv, a));
        }

        return false;
    }

    /// <summary>Get the final substitution for a type variable.</summary>
    public AsterType? GetSubstitution(int typeVarId)
    {
        return _substitutions.TryGetValue(typeVarId, out var type) ? Resolve(type) : null;
    }

    /// <summary>Apply all substitutions to a type.</summary>
    public AsterType ApplySubstitutions(AsterType type)
    {
        type = Resolve(type);

        return type switch
        {
            FunctionType ft => new FunctionType(
                ft.ParameterTypes.Select(ApplySubstitutions).ToList(),
                ApplySubstitutions(ft.ReturnType)),
            ReferenceType rt => new ReferenceType(ApplySubstitutions(rt.Inner), rt.IsMutable),
            TypeApp ta => new TypeApp(
                ApplySubstitutions(ta.Constructor),
                ta.Arguments.Select(ApplySubstitutions).ToList()),
            _ => type
        };
    }

    /// <summary>
    /// Check if type 'from' can be implicitly coerced to type 'to'.
    /// Supports safe widening conversions for numeric types.
    /// </summary>
    private bool CanCoerce(PrimitiveType from, PrimitiveType to)
    {
        // Allow widening integer conversions (smaller -> larger, same signedness)
        var coercionRules = new Dictionary<PrimitiveKind, PrimitiveKind[]>
        {
            // Signed integer widening
            [PrimitiveKind.I8] = new[] { PrimitiveKind.I16, PrimitiveKind.I32, PrimitiveKind.I64 },
            [PrimitiveKind.I16] = new[] { PrimitiveKind.I32, PrimitiveKind.I64 },
            [PrimitiveKind.I32] = new[] { PrimitiveKind.I64 },
            
            // Unsigned integer widening
            [PrimitiveKind.U8] = new[] { PrimitiveKind.U16, PrimitiveKind.U32, PrimitiveKind.U64 },
            [PrimitiveKind.U16] = new[] { PrimitiveKind.U32, PrimitiveKind.U64 },
            [PrimitiveKind.U32] = new[] { PrimitiveKind.U64 },
            
            // Float widening
            [PrimitiveKind.F32] = new[] { PrimitiveKind.F64 },
            
            // Integer to float (lossy but commonly useful)
            [PrimitiveKind.I32] = new[] { PrimitiveKind.F64 },  // i32 -> f64 is safe
            [PrimitiveKind.U32] = new[] { PrimitiveKind.F64 },  // u32 -> f64 is safe
        };

        if (coercionRules.TryGetValue(from.Kind, out var allowedTargets))
        {
            return allowedTargets.Contains(to.Kind);
        }

        return false;
    }
}
