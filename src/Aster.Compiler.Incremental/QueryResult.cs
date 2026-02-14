namespace Aster.Compiler.Incremental;

/// <summary>
/// Result of a query execution.
/// </summary>
public abstract record QueryResult
{
    /// <summary>Hash of this result's content.</summary>
    public abstract ulong ComputeHash();
}

/// <summary>Result containing compiled module data.</summary>
public sealed record ModuleQueryResult(byte[] Data, ulong Hash) : QueryResult
{
    public override ulong ComputeHash() => Hash;
}

/// <summary>Result containing function compilation data.</summary>
public sealed record FunctionQueryResult(string FunctionName, byte[] MirData, ulong Hash) : QueryResult
{
    public override ulong ComputeHash() => Hash;
}

/// <summary>Result containing type checking information.</summary>
public sealed record TypeCheckResult(bool Success, string[] Errors, ulong Hash) : QueryResult
{
    public override ulong ComputeHash() => Hash;
}

/// <summary>Result containing optimized MIR.</summary>
public sealed record OptimizedMirResult(byte[] OptimizedMir, ulong Hash) : QueryResult
{
    public override ulong ComputeHash() => Hash;
}

/// <summary>Result containing generated code.</summary>
public sealed record CodegenResult(string Code, ulong Hash) : QueryResult
{
    public override ulong ComputeHash() => Hash;
}
