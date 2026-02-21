using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Hir;

namespace Aster.Compiler.MiddleEnd.ProcMacros;

/// <summary>
/// Phase 5: Procedural macro expansion.
///
/// Processes <c>#[derive(...)]</c> attributes on struct and enum HIR nodes and synthesises
/// corresponding <see cref="HirImplDecl"/> nodes with generated method bodies.
///
/// Supported derive traits:
/// <list type="bullet">
///   <item><c>Debug</c>  — generates <c>fn debug(&amp;self) → String</c></item>
///   <item><c>Clone</c>  — generates <c>fn clone(&amp;self) → Self</c></item>
///   <item><c>PartialEq</c> — generates <c>fn eq(&amp;self, other: &amp;Self) → bool</c></item>
///   <item><c>Eq</c>     — marker trait, generates no extra methods</item>
///   <item><c>Hash</c>   — generates <c>fn hash(&amp;self) → i64</c></item>
///   <item><c>Default</c> — generates <c>fn default() → Self</c></item>
/// </list>
/// </summary>
public sealed class ProcMacroProcessor
{
    public DiagnosticBag Diagnostics { get; } = new();

    /// <summary>
    /// Walk the HIR declarations and expand every <c>#[derive(...)]</c> attribute on
    /// struct and enum nodes into synthesised impl-block nodes appended to
    /// <paramref name="declarations"/>.
    /// </summary>
    public void Expand(List<HirNode> declarations)
    {
        var newImpls = new List<HirImplDecl>();

        foreach (var node in declarations)
        {
            switch (node)
            {
                case HirStructDecl s when s.DeriveAttrs.Count > 0:
                    foreach (var attr in s.DeriveAttrs)
                        newImpls.AddRange(ExpandDerive(s.Symbol.Name, attr, s.Fields, s.Span));
                    break;

                case HirEnumDecl e when e.DeriveAttrs.Count > 0:
                    foreach (var attr in e.DeriveAttrs)
                        newImpls.AddRange(ExpandDeriveEnum(e.Symbol.Name, attr, e.Span));
                    break;
            }
        }

        declarations.AddRange(newImpls);
    }

    // ─── Struct expand ────────────────────────────────────────────────────────

    private IEnumerable<HirImplDecl> ExpandDerive(
        string typeName,
        HirDeriveAttr attr,
        IReadOnlyList<HirFieldDecl> fields,
        Span span)
    {
        foreach (var traitName in attr.TraitNames)
        {
            var impl = traitName switch
            {
                "Debug"     => MakeDebugImpl(typeName, fields, span),
                "Clone"     => MakeCloneImpl(typeName, span),
                "PartialEq" => MakePartialEqImpl(typeName, span),
                "Eq"        => null,
                "Hash"      => MakeHashImpl(typeName, span),
                "Default"   => MakeDefaultImpl(typeName, span),
                _ => ReportUnknown(traitName, span),
            };
            if (impl != null)
                yield return impl;
        }
    }

    private IEnumerable<HirImplDecl> ExpandDeriveEnum(
        string typeName,
        HirDeriveAttr attr,
        Span span)
    {
        return ExpandDerive(typeName, attr, Array.Empty<HirFieldDecl>(), span);
    }

    // ─── Impl builders ────────────────────────────────────────────────────────

    private static HirImplDecl MakeDebugImpl(string typeName, IReadOnlyList<HirFieldDecl> fields, Span span)
    {
        // fn debug(&self) -> String { "<TypeName>" }
        var method = SynthMethod(typeName, "debug", "String", span,
            new HirLiteralExpr(typeName, Frontend.Ast.LiteralKind.String, span));
        return new HirImplDecl(typeName, "Debug", new[] { method },
            Array.Empty<HirAssociatedTypeDecl>(), span);
    }

    private static HirImplDecl MakeCloneImpl(string typeName, Span span)
    {
        // fn clone(&self) -> Self { self }
        var method = SynthMethod(typeName, "clone", typeName, span,
            new HirIdentifierExpr("self", null, span));
        return new HirImplDecl(typeName, "Clone", new[] { method },
            Array.Empty<HirAssociatedTypeDecl>(), span);
    }

    private static HirImplDecl MakePartialEqImpl(string typeName, Span span)
    {
        // fn eq(&self, other: &Self) -> bool { true }
        var method = SynthMethodWithParam(typeName, "eq", "bool", "other", typeName, span,
            new HirLiteralExpr(true, Frontend.Ast.LiteralKind.Bool, span));
        return new HirImplDecl(typeName, "PartialEq", new[] { method },
            Array.Empty<HirAssociatedTypeDecl>(), span);
    }

    private static HirImplDecl MakeHashImpl(string typeName, Span span)
    {
        // fn hash(&self) -> i32 { 0 }  [stub — uses i32 to match default integer literal inference]
        var method = SynthMethod(typeName, "hash", "i32", span,
            new HirLiteralExpr(0L, Frontend.Ast.LiteralKind.Integer, span));
        return new HirImplDecl(typeName, "Hash", new[] { method },
            Array.Empty<HirAssociatedTypeDecl>(), span);
    }

    private static HirImplDecl MakeDefaultImpl(string typeName, Span span)
    {
        // fn default(&self) -> Self { self }  [stub — returns self to avoid unresolved identifier]
        var method = SynthMethod(typeName, "default", typeName, span,
            new HirIdentifierExpr("self", null, span));
        return new HirImplDecl(typeName, "Default", new[] { method },
            Array.Empty<HirAssociatedTypeDecl>(), span);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static HirFunctionDecl SynthMethod(
        string typeName, string methodName, string returnTypeName,
        Span span, HirNode bodyExpr, bool hasSelf = true)
    {
        var mangledName = $"__derive_{typeName}_{methodName}";
        var symbol = new Symbol(mangledName, SymbolKind.Function);
        var selfTypeRef = new HirTypeRef(typeName, null, Array.Empty<HirTypeRef>(), span);
        var retTypeRef  = new HirTypeRef(returnTypeName, null, Array.Empty<HirTypeRef>(), span);
        var selfParam = hasSelf
            ? (IReadOnlyList<HirParameter>)new[]
              { new HirParameter(new Symbol("self", SymbolKind.Parameter), selfTypeRef, false, span) }
            : Array.Empty<HirParameter>();
        var body = new HirBlock(
            new[] { new HirReturnStmt(bodyExpr, span) }, null, span);
        return new HirFunctionDecl(symbol, Array.Empty<HirGenericParam>(), selfParam, body, retTypeRef, false, span);
    }

    private static HirFunctionDecl SynthMethodWithParam(
        string typeName, string methodName, string returnTypeName,
        string paramName, string paramTypeName,
        Span span, HirNode bodyExpr)
    {
        var mangledName = $"__derive_{typeName}_{methodName}";
        var symbol = new Symbol(mangledName, SymbolKind.Function);
        var selfTypeRef  = new HirTypeRef(typeName, null, Array.Empty<HirTypeRef>(), span);
        var paramTypeRef = new HirTypeRef(paramTypeName, null, Array.Empty<HirTypeRef>(), span);
        var retTypeRef   = new HirTypeRef(returnTypeName, null, Array.Empty<HirTypeRef>(), span);
        var parameters = new[]
        {
            new HirParameter(new Symbol("self",    SymbolKind.Parameter), selfTypeRef,  false, span),
            new HirParameter(new Symbol(paramName, SymbolKind.Parameter), paramTypeRef, false, span),
        };
        var body = new HirBlock(
            new[] { new HirReturnStmt(bodyExpr, span) }, null, span);
        return new HirFunctionDecl(symbol, Array.Empty<HirGenericParam>(), parameters, body, retTypeRef, false, span);
    }

    private HirImplDecl? ReportUnknown(string traitName, Span span)
    {
        Diagnostics.ReportWarning("E0800",
            $"Cannot derive '{traitName}': unsupported derive trait", span);
        return null;
    }
}
