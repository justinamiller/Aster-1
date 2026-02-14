using Aster.Compiler.Analysis;
using Aster.Compiler.MiddleEnd.Mir;
using Aster.Compiler.Optimizations;

namespace Aster.Compiler.MidEnd;

/// <summary>
/// Optimization pipeline that orchestrates all mid-end passes.
/// Provides standard optimization levels (O0, O1, O2, O3).
/// </summary>
public sealed class OptimizationPipeline
{
    private readonly PassManager _passManager;
    private readonly int _optimizationLevel;

    public OptimizationPipeline(int optimizationLevel = 2, ProfileData? profileData = null)
    {
        _optimizationLevel = optimizationLevel;
        var context = new PassContext
        {
            OptimizationLevel = optimizationLevel,
            ProfileData = profileData
        };

        _passManager = new PassManager(context);
        ConfigurePasses();
    }

    private void ConfigurePasses()
    {
        switch (_optimizationLevel)
        {
            case 0:
                // O0: No optimizations, just verification
                break;

            case 1:
                // O1: Basic optimizations
                _passManager.Add(new SimplifyCfgPass());
                _passManager.Add(new DeadCodeEliminationPass());
                break;

            case 2:
                // O2: Standard optimizations
                _passManager.Add(new SimplifyCfgPass());
                _passManager.Add(new ConstantFoldingPass());
                _passManager.Add(new CopyPropagationPass());
                _passManager.Add(new DeadCodeEliminationPass());
                _passManager.Add(new CommonSubexpressionEliminationPass());
                _passManager.Add(new DropElisionPass());
                break;

            case 3:
                // O3: Aggressive optimizations
                _passManager.Add(new SimplifyCfgPass());
                _passManager.Add(new ConstantFoldingPass());
                _passManager.Add(new CopyPropagationPass());
                _passManager.Add(new CommonSubexpressionEliminationPass());
                _passManager.Add(new DeadCodeEliminationPass());
                _passManager.Add(new InliningPass());
                _passManager.Add(new EscapeAnalysisPass());
                _passManager.Add(new SroaPass());
                _passManager.Add(new DevirtualizationPass());
                _passManager.Add(new DropElisionPass());
                break;
        }
    }

    /// <summary>Run the optimization pipeline on a function.</summary>
    public PassManagerResult OptimizeFunction(MirFunction function)
    {
        // Always verify before optimization
        var verifier = new MirVerifier();
        var verifyResult = verifier.VerifyFunction(function);
        
        if (!verifyResult.IsValid)
        {
            throw new InvalidOperationException(
                $"MIR verification failed before optimization:\n{verifyResult}"
            );
        }

        // Run optimizations
        var result = _passManager.RunToFixpoint(function);

        // Verify after optimization
        verifyResult = verifier.VerifyFunction(function);
        if (!verifyResult.IsValid)
        {
            throw new InvalidOperationException(
                $"MIR verification failed after optimization:\n{verifyResult}"
            );
        }

        return result;
    }

    /// <summary>Run the optimization pipeline on a module.</summary>
    public ModulePassResult OptimizeModule(MirModule module)
    {
        return _passManager.RunOnModule(module);
    }
}
