namespace Aster.Compiler.Backends.LLVM;

/// <summary>
/// Represents a target platform for LLVM IR code generation.
/// </summary>
public enum TargetTriple
{
    /// <summary>x86-64 Linux (default)</summary>
    X86_64Linux,
    /// <summary>x86-64 macOS</summary>
    X86_64Darwin,
    /// <summary>AArch64 macOS (Apple Silicon)</summary>
    Aarch64Darwin,
    /// <summary>x86-64 Windows (MSVC ABI)</summary>
    X86_64Windows,
}

/// <summary>
/// Target-specific information used by the LLVM backend for
/// correct data layout, pointer size, and calling convention.
/// </summary>
public sealed class LlvmTargetInfo
{
    /// <summary>LLVM target triple string (e.g. "x86_64-unknown-linux-gnu").</summary>
    public string Triple { get; }

    /// <summary>LLVM data layout string encoding ABI alignment rules.</summary>
    public string DataLayout { get; }

    /// <summary>Native pointer size in bits (32 or 64).</summary>
    public int PointerBits { get; }

    /// <summary>LLVM integer type to use for <c>usize</c>/<c>isize</c>.</summary>
    public string NativeIntType => PointerBits == 64 ? "i64" : "i32";

    private LlvmTargetInfo(string triple, string dataLayout, int pointerBits)
    {
        Triple = triple;
        DataLayout = dataLayout;
        PointerBits = pointerBits;
    }

    /// <summary>Get target info for the given <see cref="TargetTriple"/>.</summary>
    public static LlvmTargetInfo For(TargetTriple target) => target switch
    {
        TargetTriple.X86_64Linux => new(
            "x86_64-unknown-linux-gnu",
            "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128",
            64),
        TargetTriple.X86_64Darwin => new(
            "x86_64-apple-macosx10.15.0",
            "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128",
            64),
        TargetTriple.Aarch64Darwin => new(
            "aarch64-apple-macosx11.0.0",
            "e-m:o-i64:64-i128:128-n32:64-S128",
            64),
        TargetTriple.X86_64Windows => new(
            "x86_64-pc-windows-msvc",
            "e-m:w-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128",
            64),
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, null),
    };

    /// <summary>Default target (x86-64 Linux).</summary>
    public static readonly LlvmTargetInfo Default = For(TargetTriple.X86_64Linux);
}
