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
    private int _closureCounter;
    /// <summary>Phase 4: registered user-defined macros (macro_rules!).</summary>
    private readonly Dictionary<string, MacroDeclNode> _macroTable = new(StringComparer.Ordinal);
    public DiagnosticBag Diagnostics { get; } = new();

    private static readonly Dictionary<string, (string Name, SymbolKind Kind)[]> StdlibExports =
        new(StringComparer.Ordinal)
        {
            ["std::fmt"] = new[] {
                ("print", SymbolKind.Function),
                ("println", SymbolKind.Function),
                ("eprint", SymbolKind.Function),
                ("eprintln", SymbolKind.Function),
                ("format", SymbolKind.Function),
            },
            ["std::alloc"] = new[] {
                ("vec_new", SymbolKind.Function),
                ("vec_push", SymbolKind.Function),
                ("vec_pop", SymbolKind.Function),
                ("vec_len", SymbolKind.Function),
                ("vec_get", SymbolKind.Function),
                ("vec_is_empty", SymbolKind.Function),
                ("new_string", SymbolKind.Function),
                ("concat", SymbolKind.Function),
                ("box_new", SymbolKind.Function),
            },
            ["std::collections"] = new[] {
                ("Vec", SymbolKind.Type),
                ("HashMap", SymbolKind.Type),
                ("hash_new", SymbolKind.Function),
                ("hash_insert", SymbolKind.Function),
                ("hash_get", SymbolKind.Function),
                ("hash_contains", SymbolKind.Function),
                ("hash_remove", SymbolKind.Function),
                ("hash_len", SymbolKind.Function),
            },
            ["std::core"] = new[] {
                ("Option", SymbolKind.Type),
                ("Result", SymbolKind.Type),
                ("Some", SymbolKind.Function),
                ("None", SymbolKind.Value),
                ("Ok", SymbolKind.Function),
                ("Err", SymbolKind.Function),
            },
            ["std::io"] = new[] {
                ("stdin", SymbolKind.Function),
                ("stdout", SymbolKind.Function),
                ("stderr", SymbolKind.Function),
                ("read_line", SymbolKind.Function),
            },
            ["std::math"] = new[] {
                ("abs", SymbolKind.Function),
                ("min", SymbolKind.Function),
                ("max", SymbolKind.Function),
                ("sqrt", SymbolKind.Function),
            },
            ["std::env"] = new[] {
                ("args", SymbolKind.Function),
                ("var", SymbolKind.Function),
                ("set_var", SymbolKind.Function),
            },
        };

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

        // Register built-in generic collection types (Weeks 13-16)
        _currentScope.Define(new Symbol("Vec", SymbolKind.Type));
        _currentScope.Define(new Symbol("Option", SymbolKind.Type));
        _currentScope.Define(new Symbol("Result", SymbolKind.Type));
        _currentScope.Define(new Symbol("HashMap", SymbolKind.Type));

        // Register built-in functions
        _currentScope.Define(new Symbol("print", SymbolKind.Function));
        _currentScope.Define(new Symbol("println", SymbolKind.Function));

        // Register all stdlib module exports as globals so they are accessible
        // without an explicit `use` statement (single source of truth)
        foreach (var exports in StdlibExports.Values)
        {
            foreach (var (name, kind) in exports)
                _currentScope.Define(new Symbol(name, kind));
        }
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
                    // Allow user-defined structs to shadow built-in generic type constructors
                    // (e.g. user code may define their own Vec<T> struct)
                    if (!_currentScope.Define(new Symbol(s.Name, SymbolKind.Type, s.IsPublic)))
                    {
                        if (BuiltinTypeNames.Contains(s.Name))
                            _currentScope.DefineOrReplace(new Symbol(s.Name, SymbolKind.Type, s.IsPublic));
                        else
                            Diagnostics.ReportError("E0200", $"Duplicate definition of '{s.Name}'", s.Span);
                    }
                    break;
                case EnumDeclNode e:
                    if (!_currentScope.Define(new Symbol(e.Name, SymbolKind.Type, e.IsPublic)))
                    {
                        if (BuiltinTypeNames.Contains(e.Name))
                            _currentScope.DefineOrReplace(new Symbol(e.Name, SymbolKind.Type, e.IsPublic));
                        else
                            Diagnostics.ReportError("E0200", $"Duplicate definition of '{e.Name}'", e.Span);
                    }
                    break;
                case TraitDeclNode t:
                    if (!_currentScope.Define(new Symbol(t.Name, SymbolKind.Trait, t.IsPublic)))
                        Diagnostics.ReportError("E0200", $"Duplicate definition of '{t.Name}'", t.Span);
                    break;
                case ModuleDeclNode m:
                    if (!_currentScope.Define(new Symbol(m.Name, SymbolKind.Module)))
                        Diagnostics.ReportError("E0200", $"Duplicate definition of '{m.Name}'", m.Span);
                    break;
                case UseDeclNode use:
                    ResolveUseDeclaration(use);
                    break;
                case TypeAliasDeclNode alias:
                    if (!_currentScope.Define(new Symbol(alias.Name, SymbolKind.Type, alias.IsPublic)))
                        Diagnostics.ReportError("E0200", $"Duplicate definition of '{alias.Name}'", alias.Span);
                    break;
                case MacroDeclNode macro:
                    // Register macro name as a special symbol (kind Function is used as placeholder)
                    _currentScope.Define(new Symbol(macro.Name, SymbolKind.Function, macro.IsPublic));
                    _macroTable[macro.Name] = macro;
                    break;
            }
        }
    }

    // Built-in type names that user code is allowed to redefine (e.g. struct Vec<T>)
    private static readonly HashSet<string> BuiltinTypeNames =
        new(StringComparer.Ordinal) { "Vec", "Option", "Result", "HashMap" };

    private void ResolveUseDeclaration(UseDeclNode use)
    {
        if (use.IsGlob)
        {
            var modulePath = string.Join("::", use.Path);
            if (StdlibExports.TryGetValue(modulePath, out var exports))
            {
                foreach (var (name, kind) in exports)
                {
                    _currentScope.Define(new Symbol(name, kind));
                }
            }
        }
        else if (use.Path.Count >= 2)
        {
            var parentPath = string.Join("::", use.Path.Take(use.Path.Count - 1));
            var symbolName = use.Path[use.Path.Count - 1];

            if (StdlibExports.TryGetValue(parentPath, out var parentExports))
            {
                var found = false;
                foreach (var (name, kind) in parentExports)
                {
                    if (name == symbolName)
                    {
                        _currentScope.Define(new Symbol(name, kind));
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Diagnostics.ReportWarning("W0010", $"Symbol '{symbolName}' not found in module '{parentPath}'", use.Span);
                }
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
        MethodCallExprNode mc => ResolveMethodCallExpr(mc),
        IdentifierExprNode id => ResolveIdentifier(id),
        PathExprNode path => ResolvePath(path),
        LiteralExprNode lit => new HirLiteralExpr(lit.Value, lit.LiteralKind, lit.Span),
        BinaryExprNode bin => new HirBinaryExpr(ResolveNode(bin.Left)!, bin.Operator, ResolveNode(bin.Right)!, bin.Span),
        UnaryExprNode un => new HirUnaryExpr(un.Operator, ResolveNode(un.Operand)!, un.Span),
        IfExprNode ifExpr => ResolveIfExpr(ifExpr),
        WhileStmtNode ws => new HirWhileStmt(ResolveNode(ws.Condition)!, ResolveBlock(ws.Body), ws.Span),
        BlockExprNode block => ResolveBlock(block),
        AssignExprNode assign => new HirAssignExpr(ResolveNode(assign.Target)!, ResolveNode(assign.Value)!, assign.Span),
        MemberAccessExprNode ma => new HirMemberAccessExpr(ResolveNode(ma.Object)!, ma.Member, ma.Span),
        StructInitExprNode structInit => ResolveStructInit(structInit),
        TraitDeclNode trait => ResolveTraitDecl(trait),
        ImplDeclNode impl => ResolveImplDecl(impl),
        ModuleDeclNode module => ResolveModuleDecl(module),
        ForStmtNode forStmt => ResolveForStmt(forStmt),
        BreakStmtNode br => new HirBreakStmt(br.Span),
        ContinueStmtNode cont => new HirContinueStmt(cont.Span),
        IndexExprNode idx => new HirIndexExpr(ResolveNode(idx.Object)!, ResolveNode(idx.Index)!, idx.Span),
        MatchExprNode matchExpr => ResolveMatchExpr(matchExpr),
        ClosureExprNode closure => ResolveClosureExpr(closure),
        TypeAliasDeclNode alias => ResolveTypeAliasDecl(alias),
        MacroDeclNode macro => ResolveMacroDecl(macro),
        MacroInvocationExprNode macroInv => ResolveMacroInvocation(macroInv),
        UseDeclNode => null,
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

        // Register generic type parameters so they can be resolved as types; preserve bounds
        var hirGenericParams = new List<HirGenericParam>();
        foreach (var gp in fn.GenericParams)
        {
            var gpSymbol = new Symbol(gp.Name, SymbolKind.Type);
            _currentScope.Define(gpSymbol);
            var bounds = gp.Bounds.Select(b => b.Name).ToList();
            hirGenericParams.Add(new HirGenericParam(gp.Name, bounds));
        }

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
        return new HirFunctionDecl(symbol, hirGenericParams, hirParams, body, returnType, fn.IsAsync, fn.Span);
    }

    private HirStructDecl ResolveStructDecl(StructDeclNode s)
    {
        var symbol = _currentScope.Lookup(s.Name) ?? new Symbol(s.Name, SymbolKind.Type, s.IsPublic);

        // Register generic type parameters in a child scope for field type resolution; preserve bounds
        var prevScope = _currentScope;
        var hirGenericParams = new List<HirGenericParam>(s.GenericParams.Count);
        if (s.GenericParams.Count > 0)
        {
            _currentScope = _currentScope.CreateChild(ScopeKind.Block);
            foreach (var gp in s.GenericParams)
            {
                _currentScope.Define(new Symbol(gp.Name, SymbolKind.Type));
                hirGenericParams.Add(new HirGenericParam(gp.Name, gp.Bounds.Select(b => b.Name).ToList()));
            }
        }
        var fields = new List<HirFieldDecl>();
        foreach (var field in s.Fields)
        {
            var typeRef = ResolveTypeRef(field.TypeAnnotation);
            fields.Add(new HirFieldDecl(field.Name, typeRef, field.Span));
        }

        _currentScope = prevScope;
        return new HirStructDecl(symbol, hirGenericParams, fields, s.Span);
    }

    private HirEnumDecl ResolveEnumDecl(EnumDeclNode e)
    {
        var symbol = _currentScope.Lookup(e.Name) ?? new Symbol(e.Name, SymbolKind.Type, e.IsPublic);

        // Register generic type parameters for variant type resolution
        var prevScope = _currentScope;
        if (e.GenericParams.Count > 0)
        {
            _currentScope = _currentScope.CreateChild(ScopeKind.Block);
            foreach (var gp in e.GenericParams)
            {
                _currentScope.Define(new Symbol(gp.Name, SymbolKind.Type));
            }
        }

        var variants = new List<HirEnumVariant>();
        foreach (var variant in e.Variants)
        {
            var fields = variant.Fields.Select(f => ResolveTypeRef(f)).ToList();
            variants.Add(new HirEnumVariant(variant.Name, fields, variant.Span));
        }

        _currentScope = prevScope;
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

    private HirPathExpr ResolvePath(PathExprNode path)
    {
        // For now, just pass through the path
        // In a full implementation, we'd resolve each segment
        // For Core-0, we mainly need this for enum variants like Option::Some
        return new HirPathExpr(path.Segments, path.Span);
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
        // Phase 4: `dyn TraitName` — don't look up "dyn" as a symbol; treat as trait object
        if (typeAnnotation.Name == "dyn")
        {
            var args = typeAnnotation.TypeArguments.Select(a => ResolveTypeRef(a)).ToList();
            return new HirTypeRef("dyn", null, args, typeAnnotation.Span);
        }

        var symbol = _currentScope.Lookup(typeAnnotation.Name);
        if (symbol == null)
        {
            Diagnostics.ReportError("E0202", $"Undefined type '{typeAnnotation.Name}'", typeAnnotation.Span);
        }
        var typeArgs = typeAnnotation.TypeArguments.Select(a => ResolveTypeRef(a)).ToList();
        return new HirTypeRef(typeAnnotation.Name, symbol, typeArgs, typeAnnotation.Span);
    }

    // ===== Weeks 17-19: Module declarations =====

    private HirModuleDecl ResolveModuleDecl(ModuleDeclNode m)
    {
        var symbol = _currentScope.Lookup(m.Name) ?? new Symbol(m.Name, SymbolKind.Module);

        // Push a new module scope so members can see each other
        var prevScope = _currentScope;
        _currentScope = _currentScope.CreateChild(ScopeKind.Module);

        // Collect member declarations first (forward-declaration pass)
        CollectDeclarations(m.Members);

        // Resolve each member
        var members = new List<HirNode>();
        foreach (var member in m.Members)
        {
            var hir = ResolveNode(member);
            if (hir != null) members.Add(hir);
        }

        _currentScope = prevScope;
        return new HirModuleDecl(symbol, members, m.Span);
    }

    // ===== Week 20: Trait and impl declarations =====

    private HirTraitDecl ResolveTraitDecl(TraitDeclNode t)
    {
        var symbol = _currentScope.Lookup(t.Name) ?? new Symbol(t.Name, SymbolKind.Trait, t.IsPublic);

        var hirGenericParams = t.GenericParams
            .Select(gp => new HirGenericParam(gp.Name, gp.Bounds.Select(b => b.Name).ToList()))
            .ToList();

        // Resolve method signatures (just names and param type names for now)
        var methods = t.Methods.Select(m =>
        {
            var paramTypeNames = m.Parameters
                .Select(p => p.TypeAnnotation?.Name ?? "unknown")
                .ToList();
            var returnTypeName = m.ReturnType?.Name;
            // IsAbstract == false means the method has a default body in the trait
            return new HirTraitMethod(m.Name, paramTypeNames, returnTypeName, m.Span, hasDefaultBody: !m.IsAbstract);
        }).ToList();

        return new HirTraitDecl(symbol, hirGenericParams, methods, t.Span);
    }

    private HirImplDecl ResolveImplDecl(ImplDeclNode impl)
    {
        var targetTypeName = impl.TargetType.Name;
        var traitName = impl.TraitType?.Name;

        // Resolve each method as a full function
        var methods = new List<HirFunctionDecl>();
        foreach (var m in impl.Methods)
        {
            // Register qualified name so TypeName::method is accessible
            var qualifiedName = $"{targetTypeName}::{m.Name}";
            _currentScope.Define(new Symbol(qualifiedName, SymbolKind.Function, m.IsPublic));

            // Open function scope
            var prevScope = _currentScope;
            _currentScope = _currentScope.CreateChild(ScopeKind.Function);

            // Register generic type parameters
            var hirGenericParams = new List<HirGenericParam>();
            foreach (var gp in m.GenericParams)
            {
                _currentScope.Define(new Symbol(gp.Name, SymbolKind.Type));
                hirGenericParams.Add(new HirGenericParam(gp.Name, gp.Bounds.Select(b => b.Name).ToList()));
            }

            // Register parameters; treat 'self' as a reference to the target type (Phase 3)
            var hirParams = new List<HirParameter>();
            foreach (var p in m.Parameters)
            {
                if (p.Name == "self")
                {
                    var selfSym = new Symbol("self", SymbolKind.Value);
                    _currentScope.Define(selfSym);
                    var selfTypeRef = new HirTypeRef(targetTypeName, _currentScope.Lookup(targetTypeName), new List<HirTypeRef>(), p.Span);
                    hirParams.Add(new HirParameter(selfSym, selfTypeRef, p.IsMutable, p.Span));
                }
                else
                {
                    var paramSym = new Symbol(p.Name, SymbolKind.Parameter);
                    _currentScope.Define(paramSym);
                    var typeRef = p.TypeAnnotation != null ? ResolveTypeRef(p.TypeAnnotation) : null;
                    hirParams.Add(new HirParameter(paramSym, typeRef, p.IsMutable, p.Span));
                }
            }

            var returnType = m.ReturnType != null ? ResolveTypeRef(m.ReturnType) : null;
            var body = ResolveBlock(m.Body);
            _currentScope = prevScope;

            var methodSym = _currentScope.Lookup(qualifiedName) ?? new Symbol(m.Name, SymbolKind.Function, m.IsPublic);
            methods.Add(new HirFunctionDecl(methodSym, hirGenericParams, hirParams, body, returnType, m.IsAsync, m.Span));
        }

        // Phase 4: resolve associated type declarations
        var hirAssocTypes = new List<HirAssociatedTypeDecl>();
        foreach (var assoc in impl.AssociatedTypes)
        {
            var typeRef = assoc.Type != null ? ResolveTypeRef(assoc.Type) : null;
            hirAssocTypes.Add(new HirAssociatedTypeDecl(assoc.Name, targetTypeName, typeRef, assoc.Span));
        }

        return new HirImplDecl(targetTypeName, traitName, methods, hirAssocTypes, impl.Span);
    }

    // ===== Phase 4: Method Calls and Macros =====

    private HirMethodCallExpr ResolveMethodCallExpr(MethodCallExprNode mc)
    {
        var receiver = ResolveNode(mc.Receiver)!;
        var args = mc.Arguments.Select(a => ResolveNode(a)!).ToList();
        return new HirMethodCallExpr(receiver, mc.MethodName, args, mc.Span);
    }

    private HirMacroDecl ResolveMacroDecl(MacroDeclNode macro)
    {
        var symbol = _currentScope.Lookup(macro.Name) ?? new Symbol(macro.Name, SymbolKind.Function, macro.IsPublic);
        return new HirMacroDecl(symbol, macro.Rules.Count, macro.Span);
    }

    /// <summary>
    /// Expand a macro invocation.  Built-in macros (vec!, assert!, println!, panic!) are
    /// expanded to equivalent HIR constructs.  User-defined macros registered in
    /// <see cref="_macroTable"/> use the first rule's body verbatim (simplified).
    /// Unknown macros are left as pass-through <see cref="HirMacroInvocationExpr"/> nodes.
    /// </summary>
    private HirMacroInvocationExpr ResolveMacroInvocation(MacroInvocationExprNode inv)
    {
        HirNode? expanded = null;

        switch (inv.MacroName)
        {
            case "vec":
                // vec![a, b, c] expands to a call to the __vec_literal builtin with the args
                // We represent this as HirCallExpr(__vec_literal, [a, b, c])
                var vecSym = _currentScope.Lookup("vec_new") ?? new Symbol("vec_new", SymbolKind.Function);
                var vecCalleeIdent = new HirIdentifierExpr("__vec_literal", vecSym, inv.Span);
                var vecArgs = inv.Arguments.Select(a => ResolveNode(a)!).ToList();
                expanded = new HirCallExpr(vecCalleeIdent, vecArgs, inv.Span);
                break;

            case "assert":
                // assert!(cond) expands to if !cond { panic("assertion failed") }
                if (inv.Arguments.Count >= 1)
                {
                    var condNode = ResolveNode(inv.Arguments[0])!;
                    var notCond = new HirUnaryExpr(UnaryOperator.Not, condNode, inv.Span);
                    var panicSym = _currentScope.Lookup("panic") ?? new Symbol("panic", SymbolKind.Function);
                    var panicCallee = new HirIdentifierExpr("panic", panicSym, inv.Span);
                    var panicMsg = new HirLiteralExpr("assertion failed", LiteralKind.String, inv.Span);
                    var panicCall = new HirCallExpr(panicCallee, new[] { (HirNode)panicMsg }, inv.Span);
                    var panicBlock = new HirBlock(new[] { (HirNode)new HirExprStmt(panicCall, inv.Span) }, null, inv.Span);
                    expanded = new HirIfExpr(notCond, panicBlock, null, inv.Span);
                }
                break;

            case "println":
            case "print":
                // println!(val) / print!(val) expands to print(val)
                {
                    var printName = inv.MacroName == "println" ? "println" : "print";
                    var printSym = _currentScope.Lookup(printName) ?? new Symbol(printName, SymbolKind.Function);
                    var printCallee = new HirIdentifierExpr(printName, printSym, inv.Span);
                    var printArgs = inv.Arguments.Select(a => ResolveNode(a)!).ToList();
                    expanded = new HirCallExpr(printCallee, printArgs, inv.Span);
                }
                break;

            case "panic":
                // panic!(msg) expands to panic(msg)
                {
                    var panicSym2 = _currentScope.Lookup("panic") ?? new Symbol("panic", SymbolKind.Function);
                    var panicCallee2 = new HirIdentifierExpr("panic", panicSym2, inv.Span);
                    var panicArgs = inv.Arguments.Select(a => ResolveNode(a)!).ToList();
                    expanded = new HirCallExpr(panicCallee2, panicArgs, inv.Span);
                }
                break;

            default:
                // Try user-defined macro
                if (_macroTable.TryGetValue(inv.MacroName, out var userMacro) && userMacro.Rules.Count > 0)
                {
                    // Use the first rule's body (simplified — no pattern matching yet)
                    expanded = ResolveBlock(userMacro.Rules[0].Body);
                }
                break;
        }

        return new HirMacroInvocationExpr(inv.MacroName, expanded, inv.Span);
    }

    // ===== Phase 2 Closeout: for / match / break / continue / index =====

    private HirForStmt ResolveForStmt(ForStmtNode forStmt)
    {
        var iterable = ResolveNode(forStmt.Iterable)!;

        // Open a new scope for the loop variable
        var prevScope = _currentScope;
        _currentScope = _currentScope.CreateChild(ScopeKind.Block);

        var varSymbol = new Symbol(forStmt.Variable, SymbolKind.Value);
        _currentScope.Define(varSymbol);

        var body = ResolveBlock(forStmt.Body);
        _currentScope = prevScope;

        return new HirForStmt(varSymbol, iterable, body, forStmt.Span);
    }

    private HirMatchExpr ResolveMatchExpr(MatchExprNode matchExpr)
    {
        var scrutinee = ResolveNode(matchExpr.Scrutinee)!;

        var arms = matchExpr.Arms.Select(arm =>
        {
            // Open a scope for pattern-bound variables
            var prevScope = _currentScope;
            _currentScope = _currentScope.CreateChild(ScopeKind.Block);

            var pattern = ResolvePattern(arm.Pattern);
            var body = ResolveNode(arm.Body)!;

            _currentScope = prevScope;
            return new HirMatchArm(pattern, body, arm.Span);
        }).ToList();

        return new HirMatchExpr(scrutinee, arms, matchExpr.Span);
    }

    private HirPattern ResolvePattern(PatternNode pattern)
    {
        if (pattern.IsWildcard)
            return new HirPattern(PatternKind.Wildcard, null, null, null, new List<HirPattern>(), pattern.Span);

        if (pattern.Literal != null)
            return new HirPattern(PatternKind.Literal, null, pattern.Literal.Value, null, new List<HirPattern>(), pattern.Span);

        if (pattern.SubPatterns.Count > 0)
        {
            // Constructor pattern: e.g. Some(x), Ok(v)
            var subPatterns = pattern.SubPatterns.Select(sp => ResolvePattern(sp)).ToList();
            return new HirPattern(PatternKind.Constructor, null, null, pattern.Name, subPatterns, pattern.Span);
        }

        // Simple variable binding
        if (!string.IsNullOrEmpty(pattern.Name))
        {
            var varSym = new Symbol(pattern.Name, SymbolKind.Value);
            _currentScope.Define(varSym);
            return new HirPattern(PatternKind.Variable, pattern.Name, null, null, new List<HirPattern>(), pattern.Span);
        }

        // Fallback: wildcard
        return new HirPattern(PatternKind.Wildcard, null, null, null, new List<HirPattern>(), pattern.Span);
    }

    private HirClosureExpr ResolveClosureExpr(ClosureExprNode closure)
    {
        var mangledName = $"__closure_{_closureCounter++}";

        var prevScope = _currentScope;
        _currentScope = _currentScope.CreateChild(ScopeKind.Function);

        var hirParams = new List<HirParameter>();
        foreach (var (name, typeAnnot) in closure.Parameters)
        {
            var paramSym = new Symbol(name, SymbolKind.Parameter);
            _currentScope.Define(paramSym);
            var typeRef = typeAnnot != null ? ResolveTypeRef(typeAnnot) : null;
            hirParams.Add(new HirParameter(paramSym, typeRef, false, closure.Span));
        }

        var body = ResolveNode(closure.Body) ?? new HirLiteralExpr(0L, LiteralKind.Integer, closure.Span);

        _currentScope = prevScope;
        return new HirClosureExpr(hirParams, body, mangledName, closure.Span);
    }

    private HirTypeAliasDecl ResolveTypeAliasDecl(TypeAliasDeclNode alias)
    {
        // Symbol was pre-registered in CollectDeclarations; look it up (should always succeed)
        var symbol = _currentScope.Lookup(alias.Name);
        if (symbol == null)
        {
            // Fallback: define it now if somehow missed in the collect pass
            symbol = new Symbol(alias.Name, SymbolKind.Type, alias.IsPublic);
            _currentScope.Define(symbol);
        }
        var target = ResolveTypeRef(alias.Target);
        return new HirTypeAliasDecl(symbol, target, alias.Span);
    }
}
