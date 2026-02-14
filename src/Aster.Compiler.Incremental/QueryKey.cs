namespace Aster.Compiler.Incremental;

/// <summary>
/// Unique key for a query in the incremental compilation system.
/// Queries are pure functions: input -&gt; output
/// </summary>
public abstract record QueryKey
{
    /// <summary>Compute a stable hash for this query key.</summary>
    public abstract ulong ComputeHash();
}

/// <summary>Query key for compiling a module.</summary>
public sealed record ModuleQueryKey(string ModuleName, ulong SourceHash) : QueryKey
{
    public override ulong ComputeHash()
    {
        var hasher = new StableHasher();
        hasher.HashString("Module");
        hasher.HashString(ModuleName);
        hasher.HashInt64((long)SourceHash);
        return hasher.Finalize();
    }
}

/// <summary>Query key for compiling a function.</summary>
public sealed record FunctionQueryKey(string ModuleName, string FunctionName, ulong InputHash) : QueryKey
{
    public override ulong ComputeHash()
    {
        var hasher = new StableHasher();
        hasher.HashString("Function");
        hasher.HashString(ModuleName);
        hasher.HashString(FunctionName);
        hasher.HashInt64((long)InputHash);
        return hasher.Finalize();
    }
}

/// <summary>Query key for type checking a function.</summary>
public sealed record TypeCheckQueryKey(string ModuleName, string FunctionName, ulong AstHash) : QueryKey
{
    public override ulong ComputeHash()
    {
        var hasher = new StableHasher();
        hasher.HashString("TypeCheck");
        hasher.HashString(ModuleName);
        hasher.HashString(FunctionName);
        hasher.HashInt64((long)AstHash);
        return hasher.Finalize();
    }
}

/// <summary>Query key for optimizing MIR.</summary>
public sealed record OptimizeQueryKey(string ModuleName, string FunctionName, ulong MirHash) : QueryKey
{
    public override ulong ComputeHash()
    {
        var hasher = new StableHasher();
        hasher.HashString("Optimize");
        hasher.HashString(ModuleName);
        hasher.HashString(FunctionName);
        hasher.HashInt64((long)MirHash);
        return hasher.Finalize();
    }
}

/// <summary>Query key for code generation.</summary>
public sealed record CodegenQueryKey(string ModuleName, string FunctionName, ulong OptimizedMirHash) : QueryKey
{
    public override ulong ComputeHash()
    {
        var hasher = new StableHasher();
        hasher.HashString("Codegen");
        hasher.HashString(ModuleName);
        hasher.HashString(FunctionName);
        hasher.HashInt64((long)OptimizedMirHash);
        return hasher.Finalize();
    }
}
