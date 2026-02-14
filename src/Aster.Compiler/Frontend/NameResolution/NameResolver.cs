using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.Hir;

namespace Aster.Compiler.Frontend.NameResolution;

/// <summary>
/// Two-pass name resolver.
/// Pass 1: Collect all declarations into scope.
/// Pass 2: Resolve all references and lower AST to HIR.
/// </summary>
public sealed class NameResolver
{
    private Scope _currentScope;
    public DiagnosticBag Diagnostics { get; } = new();

    public NameResolver()
    {
        _currentScope = new Scope(ScopeKind.Module);
        RegisterBuiltins();
    }

    private void RegisterBuiltins()
    {
        // Register built-in types
        _currentScope.Define(new Symbol("i32", SymbolKind.Type));
        _currentScope.Define(new Symbol("i64", SymbolKind.Type));
        _currentScope.Define(new Symbol("f32", SymbolKind.Type));
        _currentScope.Define(new Symbol("f64", SymbolKind.Type));
        _currentScope.Define(new Symbol("bool", SymbolKind.Type));
        _currentScope.Define(new Symbol("String", SymbolKind.Type));
        _currentScope.Define(new Symbol("char", SymbolKind.Type));
        _currentScope.Define(new Symbol("void", SymbolKind.Type));
        _currentScope.Define(new Symbol("u8", SymbolKind.Type));
        _currentScope.Define(new Symbol("u16", SymbolKind.Type));
        _currentScope.Define(new Symbol("u32", SymbolKind.Type));
        _currentScope.Define(new Symbol("u64", SymbolKind.Type));
        _currentScope.Define(new Symbol("i8", SymbolKind.Type));
        _currentScope.Define(new Symbol("i16", SymbolKind.Type));

        // Register built-in functions
        _currentScope.Define(new Symbol("print", SymbolKind.Function));
        _currentScope.Define(new Symbol("println", SymbolKind.Function));
    }

    /// <summary>Resolve an AST program into HIR.</summary>
    public HirProgram Resolve(ProgramNode program)
    {
        // Pass 1: Collect declarations
        CollectDeclarations(program.Declarations);

        // Pass 2: Resolve references and build HIR
        var hirDecls = new List<HirNode>();
        foreach (var decl in program.Declarations)
        {
            var hir = ResolveNode(decl);
            if (hir != null) hirDecls.Add(hir);
        }

        return new HirProgram(hirDecls, program.Span);
    }

    private void CollectDeclarations(IReadOnlyList<AstNode> declarations)
    {
        foreach (var decl in declarations)
        {
            switch (decl)
            {
                case FunctionDeclNode fn:
                    if (!_currentScope.Define(new Symbol(fn.Name, SymbolKind.Function, fn.IsPublic)))
                        Diagnostics.ReportError("E0200", $"Duplicate definition of '{fn.Name}'", fn.Span);
                    break;
                case StructDeclNode s:
                    if (!_currentScope.Define(new Symbol(s.Name, SymbolKind.Type, s.IsPublic)))
                        Diagnostics.ReportError("E0200", $"Duplicate definition of '{s.Name}'", s.Span);
                    break;
                case EnumDeclNode e:
                    if (!_currentScope.Define(new Symbol(e.Name, SymbolKind.Type, e.IsPublic)))
                        Diagnostics.ReportError("E0200", $"Duplicate definition of '{e.Name}'", e.Span);
                    break;
                case TraitDeclNode t:
                    if (!_currentScope.Define(new Symbol(t.Name, SymbolKind.Trait, t.IsPublic)))
                        Diagnostics.ReportError("E0200", $"Duplicate definition of '{t.Name}'", t.Span);
                    break;
                case ModuleDeclNode m:
                    if (!_currentScope.Define(new Symbol(m.Name, SymbolKind.Module)))
                        Diagnostics.ReportError("E0200", $"Duplicate definition of '{m.Name}'", m.Span);
                    break;
            }
        }
    }

    private HirNode? ResolveNode(AstNode node) => node switch
    {
        FunctionDeclNode fn => ResolveFunctionDecl(fn),
        StructDeclNode s => ResolveStructDecl(s),
        EnumDeclNode e => ResolveEnumDecl(e),
        LetStmtNode let => ResolveLetStmt(let),
        ReturnStmtNode ret => new HirReturnStmt(ret.Value != null ? ResolveNode(ret.Value) : null, ret.Span),
        ExpressionStmtNode es => new HirExprStmt(ResolveNode(es.Expression)!, es.Span),
        CallExprNode call => ResolveCallExpr(call),
        IdentifierExprNode id => ResolveIdentifier(id),
        LiteralExprNode lit => new HirLiteralExpr(lit.Value, lit.LiteralKind, lit.Span),
        BinaryExprNode bin => new HirBinaryExpr(ResolveNode(bin.Left)!, bin.Operator, ResolveNode(bin.Right)!, bin.Span),
        UnaryExprNode un => new HirUnaryExpr(un.Operator, ResolveNode(un.Operand)!, un.Span),
        IfExprNode ifExpr => ResolveIfExpr(ifExpr),
        WhileStmtNode ws => new HirWhileStmt(ResolveNode(ws.Condition)!, ResolveBlock(ws.Body), ws.Span),
        BlockExprNode block => ResolveBlock(block),
        AssignExprNode assign => new HirAssignExpr(ResolveNode(assign.Target)!, ResolveNode(assign.Value)!, assign.Span),
        MemberAccessExprNode ma => new HirMemberAccessExpr(ResolveNode(ma.Object)!, ma.Member, ma.Span),
        StructInitExprNode structInit => ResolveStructInit(structInit),
        _ => null,
    };

    private HirStructInitExpr ResolveStructInit(StructInitExprNode structInit)
    {
        // Verify the struct type exists
        var symbol = _currentScope.Lookup(structInit.StructName);
        if (symbol == null || symbol.Kind != SymbolKind.Type)
        {
            Diagnostics.ReportError("E0201", $"Unknown type '{structInit.StructName}'", structInit.Span);
        }

        // Resolve field initializations
        var hirFields = structInit.Fields.Select(f =>
            new HirFieldInit(f.FieldName, ResolveNode(f.Value)!, f.Span)
        ).ToList();

        return new HirStructInitExpr(structInit.StructName, hirFields, structInit.Span);
    }

    private HirFunctionDecl ResolveFunctionDecl(FunctionDeclNode fn)
    {
        var symbol = _currentScope.Lookup(fn.Name) ?? new Symbol(fn.Name, SymbolKind.Function, fn.IsPublic);

        var prevScope = _currentScope;
        _currentScope = _currentScope.CreateChild(ScopeKind.Function);

        var hirParams = new List<HirParameter>();
        foreach (var param in fn.Parameters)
        {
            var paramSymbol = new Symbol(param.Name, SymbolKind.Parameter);
            _currentScope.Define(paramSymbol);
            var typeRef = param.TypeAnnotation != null ? ResolveTypeRef(param.TypeAnnotation) : null;
            hirParams.Add(new HirParameter(paramSymbol, typeRef, param.IsMutable, param.Span));
        }

        var returnType = fn.ReturnType != null ? ResolveTypeRef(fn.ReturnType) : null;
        var body = ResolveBlock(fn.Body);

        _currentScope = prevScope;
        return new HirFunctionDecl(symbol, hirParams, body, returnType, fn.IsAsync, fn.Span);
    }

    private HirStructDecl ResolveStructDecl(StructDeclNode s)
    {
        var symbol = _currentScope.Lookup(s.Name) ?? new Symbol(s.Name, SymbolKind.Type, s.IsPublic);
        var fields = new List<HirFieldDecl>();
        foreach (var field in s.Fields)
        {
            var typeRef = ResolveTypeRef(field.TypeAnnotation);
            fields.Add(new HirFieldDecl(field.Name, typeRef, field.Span));
        }
        return new HirStructDecl(symbol, fields, s.Span);
    }

    private HirEnumDecl ResolveEnumDecl(EnumDeclNode e)
    {
        var symbol = _currentScope.Lookup(e.Name) ?? new Symbol(e.Name, SymbolKind.Type, e.IsPublic);
        var variants = new List<HirEnumVariant>();
        foreach (var variant in e.Variants)
        {
            var fields = variant.Fields.Select(f => ResolveTypeRef(f)).ToList();
            variants.Add(new HirEnumVariant(variant.Name, fields, variant.Span));
        }
        return new HirEnumDecl(symbol, variants, e.Span);
    }

    private HirLetStmt ResolveLetStmt(LetStmtNode let)
    {
        var symbol = new Symbol(let.Name, SymbolKind.Value);
        if (_currentScope.LookupLocal(let.Name) != null)
        {
            Diagnostics.ReportWarning("W0001", $"Variable '{let.Name}' shadows an existing definition", let.Span);
        }
        _currentScope.Define(symbol);
        var typeRef = let.TypeAnnotation != null ? ResolveTypeRef(let.TypeAnnotation) : null;
        var init = let.Initializer != null ? ResolveNode(let.Initializer) : null;
        return new HirLetStmt(symbol, let.IsMutable, typeRef, init, let.Span);
    }

    private HirCallExpr ResolveCallExpr(CallExprNode call)
    {
        var callee = ResolveNode(call.Callee)!;
        var args = call.Arguments.Select(a => ResolveNode(a)!).ToList();
        return new HirCallExpr(callee, args, call.Span);
    }

    private HirIdentifierExpr ResolveIdentifier(IdentifierExprNode id)
    {
        var symbol = _currentScope.Lookup(id.Name);
        if (symbol == null)
        {
            Diagnostics.ReportError("E0201", $"Undefined symbol '{id.Name}'", id.Span);
        }
        return new HirIdentifierExpr(id.Name, symbol, id.Span);
    }

    private HirIfExpr ResolveIfExpr(IfExprNode ifExpr)
    {
        var condition = ResolveNode(ifExpr.Condition)!;
        var thenBranch = ResolveBlock(ifExpr.ThenBranch);
        HirNode? elseBranch = null;
        if (ifExpr.ElseBranch != null)
        {
            elseBranch = ifExpr.ElseBranch is BlockExprNode block ? ResolveBlock(block) : ResolveNode(ifExpr.ElseBranch);
        }
        return new HirIfExpr(condition, thenBranch, elseBranch, ifExpr.Span);
    }

    private HirBlock ResolveBlock(BlockExprNode block)
    {
        var prevScope = _currentScope;
        _currentScope = _currentScope.CreateChild(ScopeKind.Block);

        var stmts = new List<HirNode>();
        foreach (var stmt in block.Statements)
        {
            var hir = ResolveNode(stmt);
            if (hir != null) stmts.Add(hir);
        }

        HirNode? tail = block.TailExpression != null ? ResolveNode(block.TailExpression) : null;

        _currentScope = prevScope;
        return new HirBlock(stmts, tail, block.Span);
    }

    private HirTypeRef ResolveTypeRef(TypeAnnotationNode typeAnnotation)
    {
        var symbol = _currentScope.Lookup(typeAnnotation.Name);
        if (symbol == null)
        {
            Diagnostics.ReportError("E0202", $"Undefined type '{typeAnnotation.Name}'", typeAnnotation.Span);
        }
        var args = typeAnnotation.TypeArguments.Select(a => ResolveTypeRef(a)).ToList();
        return new HirTypeRef(typeAnnotation.Name, symbol, args, typeAnnotation.Span);
    }
}
