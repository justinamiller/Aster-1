using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Backends.Abstractions;

/// <summary>
/// Interface for code generation backends.
/// </summary>
public interface IBackend
{
    /// <summary>Emit code from a MIR module.</summary>
    string Emit(MirModule module);
}
