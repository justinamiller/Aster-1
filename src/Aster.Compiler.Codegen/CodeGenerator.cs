using Aster.Compiler.Backends.Abstractions;
using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Codegen;

/// <summary>
/// Wrapper for code generation backends.
/// Provides a unified interface for emitting optimized code.
/// </summary>
public sealed class CodeGenerator
{
    private readonly IBackend _backend;

    public CodeGenerator(IBackend backend)
    {
        _backend = backend;
    }

    /// <summary>Generate code from MIR module.</summary>
    public string Generate(MirModule module)
    {
        return _backend.Emit(module);
    }
}
