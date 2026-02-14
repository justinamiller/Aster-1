namespace Aster.Packages;

/// <summary>
/// Capabilities-based sandbox for running build scripts.
/// Restricts what operations a build script can perform.
/// </summary>
public sealed class Sandbox
{
    private readonly HashSet<SandboxCapability> _capabilities;

    public Sandbox(IEnumerable<SandboxCapability> capabilities)
    {
        _capabilities = new HashSet<SandboxCapability>(capabilities);
    }

    /// <summary>
    /// Check if a capability is granted.
    /// </summary>
    public bool HasCapability(SandboxCapability capability) =>
        _capabilities.Contains(capability);

    /// <summary>
    /// Execute an action only if the required capability is granted.
    /// </summary>
    public void Execute(SandboxCapability required, Action action)
    {
        if (!HasCapability(required))
            throw new UnauthorizedAccessException($"Build script requires capability: {required}");
        action();
    }

    /// <summary>
    /// Create a sandbox with default build capabilities (read source, write output).
    /// </summary>
    public static Sandbox CreateDefault() => new(new[]
    {
        SandboxCapability.ReadSource,
        SandboxCapability.WriteOutput,
        SandboxCapability.ReadEnvironment
    });

    /// <summary>
    /// Create a sandbox with all capabilities (for trusted builds).
    /// </summary>
    public static Sandbox CreateTrusted() => new(Enum.GetValues<SandboxCapability>());
}

/// <summary>
/// Capabilities that can be granted to build scripts.
/// </summary>
public enum SandboxCapability
{
    ReadSource,
    WriteOutput,
    ReadEnvironment,
    Network,
    FileSystemFull,
    ProcessExec
}
