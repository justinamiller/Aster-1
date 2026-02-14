using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Hir;

namespace Aster.Compiler.Frontend.Effects;

/// <summary>
/// Represents a set of effects that a function may perform.
/// </summary>
[Flags]
public enum Effect
{
    None = 0,
    Io = 1 << 0,
    Alloc = 1 << 1,
    Async = 1 << 2,
    Unsafe = 1 << 3,
    Ffi = 1 << 4,
    Throw = 1 << 5,
}

/// <summary>
/// Tracks and infers effect sets for functions.
/// </summary>
public sealed class EffectSet
{
    public Effect Effects { get; private set; }

    public EffectSet(Effect effects = Effect.None) => Effects = effects;

    public bool Has(Effect effect) => (Effects & effect) != 0;
    public void Add(Effect effect) => Effects |= effect;
    public void Merge(EffectSet other) => Effects |= other.Effects;

    public override string ToString()
    {
        var parts = new List<string>();
        if (Has(Effect.Io)) parts.Add("io");
        if (Has(Effect.Alloc)) parts.Add("alloc");
        if (Has(Effect.Async)) parts.Add("async");
        if (Has(Effect.Unsafe)) parts.Add("unsafe");
        if (Has(Effect.Ffi)) parts.Add("ffi");
        if (Has(Effect.Throw)) parts.Add("throw");
        return parts.Count > 0 ? string.Join(", ", parts) : "pure";
    }
}

/// <summary>
/// Effect checker that infers and verifies function effect annotations.
/// </summary>
public sealed class EffectChecker
{
    private readonly Dictionary<int, EffectSet> _functionEffects = new();
    private readonly Dictionary<int, EffectSet> _declaredEffects = new();
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>Check effects for an HIR program.</summary>
    public void Check(HirProgram program)
    {
        // Phase 1: Infer actual effects
        foreach (var decl in program.Declarations)
        {
            if (decl is HirFunctionDecl fn)
                InferFunctionEffects(fn);
        }

        // Phase 2: Validate against declared effects
        foreach (var decl in program.Declarations)
        {
            if (decl is HirFunctionDecl fn)
                ValidateFunctionEffects(fn);
        }
    }

    /// <summary>Set the declared effect set for a function.</summary>
    public void SetDeclaredEffects(Symbol symbol, EffectSet effects)
    {
        _declaredEffects[symbol.Id] = effects;
    }

    private EffectSet InferFunctionEffects(HirFunctionDecl fn)
    {
        var effects = new EffectSet();

        if (fn.IsAsync)
            effects.Add(Effect.Async);

        InferBlockEffects(fn.Body, effects);

        _functionEffects[fn.Symbol.Id] = effects;
        return effects;
    }

    private void ValidateFunctionEffects(HirFunctionDecl fn)
    {
        if (!_declaredEffects.TryGetValue(fn.Symbol.Id, out var declared))
            return; // No declared effects, skip validation

        if (!_functionEffects.TryGetValue(fn.Symbol.Id, out var inferred))
            return;

        // Check if inferred effects exceed declared effects
        var excess = inferred.Effects & ~declared.Effects;
        if (excess != Effect.None)
        {
            var excessSet = new EffectSet(excess);
            Diagnostics.ReportError(
                "E0330",
                $"Function '{fn.Symbol.Name}' has undeclared effects: {excessSet}. Declared: {declared}, Actual: {inferred}",
                fn.Span);
        }
    }

    private void InferBlockEffects(HirBlock block, EffectSet effects)
    {
        foreach (var stmt in block.Statements)
            InferNodeEffects(stmt, effects);
        if (block.TailExpression != null)
            InferNodeEffects(block.TailExpression, effects);
    }

    private void InferNodeEffects(HirNode node, EffectSet effects)
    {
        switch (node)
        {
            case HirCallExpr call:
                if (call.Callee is HirIdentifierExpr id)
                {
                    // Built-in print has IO effect
                    if (id.Name == "print" || id.Name == "println")
                        effects.Add(Effect.Io);

                    // Propagate callee effects
                    if (id.ResolvedSymbol != null && _functionEffects.TryGetValue(id.ResolvedSymbol.Id, out var calleeEffects))
                        effects.Merge(calleeEffects);
                }
                foreach (var arg in call.Arguments)
                    InferNodeEffects(arg, effects);
                break;

            case HirLetStmt let:
                if (let.Initializer != null)
                    InferNodeEffects(let.Initializer, effects);
                break;

            case HirExprStmt es:
                InferNodeEffects(es.Expression, effects);
                break;

            case HirReturnStmt ret:
                if (ret.Value != null)
                    InferNodeEffects(ret.Value, effects);
                break;

            case HirIfExpr ifExpr:
                InferNodeEffects(ifExpr.Condition, effects);
                InferBlockEffects(ifExpr.ThenBranch, effects);
                if (ifExpr.ElseBranch is HirBlock elseBlock)
                    InferBlockEffects(elseBlock, effects);
                else if (ifExpr.ElseBranch != null)
                    InferNodeEffects(ifExpr.ElseBranch, effects);
                break;

            case HirWhileStmt ws:
                InferNodeEffects(ws.Condition, effects);
                InferBlockEffects(ws.Body, effects);
                break;

            case HirBlock block:
                InferBlockEffects(block, effects);
                break;

            case HirBinaryExpr bin:
                InferNodeEffects(bin.Left, effects);
                InferNodeEffects(bin.Right, effects);
                break;

            case HirUnaryExpr un:
                InferNodeEffects(un.Operand, effects);
                break;

            case HirAssignExpr assign:
                InferNodeEffects(assign.Target, effects);
                InferNodeEffects(assign.Value, effects);
                break;
        }
    }

    /// <summary>Get the inferred effects for a function.</summary>
    public EffectSet? GetEffects(Symbol symbol) =>
        _functionEffects.TryGetValue(symbol.Id, out var effects) ? effects : null;
}
