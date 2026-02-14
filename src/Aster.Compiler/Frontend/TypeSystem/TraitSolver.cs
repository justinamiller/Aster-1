using Aster.Compiler.Diagnostics;

namespace Aster.Compiler.Frontend.TypeSystem;

/// <summary>
/// Represents a trait implementation.
/// </summary>
public sealed class TraitImpl
{
    public string TraitName { get; }
    public AsterType ForType { get; }
    public IReadOnlyList<TraitBound> WhereClause { get; }

    public TraitImpl(string traitName, AsterType forType, IReadOnlyList<TraitBound>? whereClause = null)
    {
        TraitName = traitName;
        ForType = forType;
        WhereClause = whereClause ?? Array.Empty<TraitBound>();
    }

    public override string ToString() => $"impl {TraitName} for {ForType.DisplayName}";
}

/// <summary>
/// Represents an obligation to prove that a type implements a trait.
/// </summary>
public sealed class Obligation
{
    public AsterType Type { get; }
    public TraitBound Bound { get; }
    public Span Span { get; }

    public Obligation(AsterType type, TraitBound bound, Span span)
    {
        Type = type;
        Bound = bound;
        Span = span;
    }

    public override string ToString() => $"{Type.DisplayName}: {Bound}";
}

/// <summary>
/// Trait solver that resolves trait obligations.
/// Implements a mini logic engine with cycle detection.
/// </summary>
public sealed class TraitSolver
{
    private readonly List<TraitImpl> _impls = new();
    private readonly Dictionary<string, bool> _cache = new();
    private readonly HashSet<string> _inProgress = new();
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>Register a trait implementation.</summary>
    public void RegisterImpl(TraitImpl impl)
    {
        _impls.Add(impl);
    }

    /// <summary>Register built-in trait implementations.</summary>
    public void RegisterBuiltins()
    {
        // Copy trait for primitive types
        var copyTypes = new[]
        {
            PrimitiveType.I8, PrimitiveType.I16, PrimitiveType.I32, PrimitiveType.I64,
            PrimitiveType.U8, PrimitiveType.U16, PrimitiveType.U32, PrimitiveType.U64,
            PrimitiveType.F32, PrimitiveType.F64,
            PrimitiveType.Bool, PrimitiveType.Char
        };

        foreach (var type in copyTypes)
        {
            RegisterImpl(new TraitImpl("Copy", type));
        }

        // Clone trait for all Copy types
        foreach (var type in copyTypes)
        {
            RegisterImpl(new TraitImpl("Clone", type));
        }
    }

    /// <summary>Resolve an obligation.</summary>
    public bool Resolve(Obligation obligation, ConstraintSolver solver)
    {
        var normalizedType = solver.Resolve(obligation.Type);
        var cacheKey = $"{normalizedType.DisplayName}:{obligation.Bound}";

        // Check cache
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        // Detect cycles
        if (_inProgress.Contains(cacheKey))
        {
            Diagnostics.ReportError(
                "E0320",
                $"Cycle detected while resolving trait bound '{obligation}'",
                obligation.Span);
            return false;
        }

        _inProgress.Add(cacheKey);
        var result = ResolveImpl(normalizedType, obligation.Bound, obligation.Span, solver);
        _inProgress.Remove(cacheKey);

        _cache[cacheKey] = result;
        return result;
    }

    private bool ResolveImpl(AsterType type, TraitBound bound, Span span, ConstraintSolver solver)
    {
        // Try to find a matching implementation
        foreach (var impl in _impls)
        {
            if (impl.TraitName != bound.TraitName)
                continue;

            // Try to unify the type with the impl's target type
            var unificationSolver = new ConstraintSolver();
            if (unificationSolver.Unify(type, impl.ForType))
            {
                // Check where clause conditions
                var allConditionsMet = true;
                foreach (var condition in impl.WhereClause)
                {
                    var condType = unificationSolver.ApplySubstitutions(type);
                    var subObligation = new Obligation(condType, condition, span);
                    if (!Resolve(subObligation, solver))
                    {
                        allConditionsMet = false;
                        break;
                    }
                }

                if (allConditionsMet)
                    return true;
            }
        }

        // Handle type variables - they might be resolved later
        if (type is TypeVariable)
            return true;

        // Handle generic parameters with bounds
        if (type is GenericParameter gp)
        {
            return gp.Bounds.Any(b => b.TraitName == bound.TraitName);
        }

        // No implementation found
        Diagnostics.ReportError(
            "E0321",
            $"Type '{type.DisplayName}' does not implement trait '{bound.TraitName}'",
            span);
        return false;
    }

    /// <summary>Check all pending trait constraints.</summary>
    public bool CheckConstraints(IEnumerable<TraitConstraint> constraints, ConstraintSolver solver)
    {
        var allSatisfied = true;
        foreach (var constraint in constraints)
        {
            var obligation = new Obligation(constraint.Type, constraint.Bound, constraint.Span);
            if (!Resolve(obligation, solver))
            {
                allSatisfied = false;
            }
        }
        return allSatisfied;
    }
}
