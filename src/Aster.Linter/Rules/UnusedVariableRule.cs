using Aster.Compiler.Frontend.Ast;

namespace Aster.Linter.Rules;

/// <summary>
/// L0001: Detects let bindings whose variables are never referenced.
/// </summary>
public sealed class UnusedVariableRule : ILintRule
{
    public string LintId => "L0001";
    public string Description => "Detect unused variables";
    public LintSeverity DefaultSeverity => LintSeverity.Warning;

    public IReadOnlyList<LintDiagnostic> Check(ProgramNode program)
    {
        var diagnostics = new List<LintDiagnostic>();
        foreach (var decl in program.Declarations)
        {
            if (decl is FunctionDeclNode func)
                CheckFunction(func, diagnostics);
            else if (decl is ModuleDeclNode module)
                CheckModule(module, diagnostics);
            else if (decl is ImplDeclNode impl)
                foreach (var method in impl.Methods)
                    CheckFunction(method, diagnostics);
            else if (decl is TraitDeclNode trait)
                foreach (var method in trait.Methods)
                    CheckFunction(method, diagnostics);
        }
        return diagnostics;
    }

    private static void CheckModule(ModuleDeclNode module, List<LintDiagnostic> diagnostics)
    {
        foreach (var member in module.Members)
        {
            if (member is FunctionDeclNode func)
                CheckFunction(func, diagnostics);
            else if (member is ImplDeclNode impl)
                foreach (var method in impl.Methods)
                    CheckFunction(method, diagnostics);
            else if (member is TraitDeclNode trait)
                foreach (var method in trait.Methods)
                    CheckFunction(method, diagnostics);
            else if (member is ModuleDeclNode nested)
                CheckModule(nested, diagnostics);
        }
    }

    private static void CheckFunction(FunctionDeclNode func, List<LintDiagnostic> diagnostics)
    {
        // Collect all let-bound variable names and their spans
        var bindings = new List<(string Name, LetStmtNode Node)>();
        CollectLetBindings(func.Body, bindings);

        // Collect all identifier references in the function body
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        CollectReferences(func.Body, referenced);

        foreach (var (name, node) in bindings)
        {
            // Skip variables starting with _ (conventional unused marker)
            if (name.StartsWith('_'))
                continue;

            if (!referenced.Contains(name))
            {
                diagnostics.Add(new LintDiagnostic(
                    "L0001",
                    $"Variable '{name}' is declared but never used",
                    node.Span,
                    LintSeverity.Warning,
                    $"Prefix with underscore: _{name}"));
            }
        }
    }

    private static void CollectLetBindings(AstNode node, List<(string Name, LetStmtNode Node)> bindings)
    {
        switch (node)
        {
            case BlockExprNode block:
                foreach (var stmt in block.Statements)
                    CollectLetBindings(stmt, bindings);
                if (block.TailExpression is not null)
                    CollectLetBindings(block.TailExpression, bindings);
                break;
            case LetStmtNode let:
                bindings.Add((let.Name, let));
                if (let.Initializer is not null)
                    CollectLetBindings(let.Initializer, bindings);
                break;
            case IfExprNode ifExpr:
                CollectLetBindings(ifExpr.Condition, bindings);
                CollectLetBindings(ifExpr.ThenBranch, bindings);
                if (ifExpr.ElseBranch is not null)
                    CollectLetBindings(ifExpr.ElseBranch, bindings);
                break;
            case ForStmtNode forStmt:
                CollectLetBindings(forStmt.Iterable, bindings);
                CollectLetBindings(forStmt.Body, bindings);
                break;
            case WhileStmtNode whileStmt:
                CollectLetBindings(whileStmt.Condition, bindings);
                CollectLetBindings(whileStmt.Body, bindings);
                break;
            case ExpressionStmtNode exprStmt:
                CollectLetBindings(exprStmt.Expression, bindings);
                break;
            case MatchExprNode matchExpr:
                CollectLetBindings(matchExpr.Scrutinee, bindings);
                foreach (var arm in matchExpr.Arms)
                    CollectLetBindings(arm.Body, bindings);
                break;
            case ReturnStmtNode ret:
                if (ret.Value is not null)
                    CollectLetBindings(ret.Value, bindings);
                break;
        }
    }

    private static void CollectReferences(AstNode node, HashSet<string> referenced)
    {
        switch (node)
        {
            case IdentifierExprNode ident:
                referenced.Add(ident.Name);
                break;
            case BlockExprNode block:
                foreach (var stmt in block.Statements)
                    CollectReferences(stmt, referenced);
                if (block.TailExpression is not null)
                    CollectReferences(block.TailExpression, referenced);
                break;
            case LetStmtNode let:
                // Do not add let.Name as a reference; only walk the initializer
                if (let.Initializer is not null)
                    CollectReferences(let.Initializer, referenced);
                break;
            case ExpressionStmtNode exprStmt:
                CollectReferences(exprStmt.Expression, referenced);
                break;
            case ReturnStmtNode ret:
                if (ret.Value is not null)
                    CollectReferences(ret.Value, referenced);
                break;
            case BinaryExprNode binary:
                CollectReferences(binary.Left, referenced);
                CollectReferences(binary.Right, referenced);
                break;
            case UnaryExprNode unary:
                CollectReferences(unary.Operand, referenced);
                break;
            case CallExprNode call:
                CollectReferences(call.Callee, referenced);
                foreach (var arg in call.Arguments)
                    CollectReferences(arg, referenced);
                break;
            case MemberAccessExprNode memberAccess:
                CollectReferences(memberAccess.Object, referenced);
                break;
            case IndexExprNode indexExpr:
                CollectReferences(indexExpr.Object, referenced);
                CollectReferences(indexExpr.Index, referenced);
                break;
            case AssignExprNode assign:
                CollectReferences(assign.Target, referenced);
                CollectReferences(assign.Value, referenced);
                break;
            case IfExprNode ifExpr:
                CollectReferences(ifExpr.Condition, referenced);
                CollectReferences(ifExpr.ThenBranch, referenced);
                if (ifExpr.ElseBranch is not null)
                    CollectReferences(ifExpr.ElseBranch, referenced);
                break;
            case ForStmtNode forStmt:
                CollectReferences(forStmt.Iterable, referenced);
                CollectReferences(forStmt.Body, referenced);
                break;
            case WhileStmtNode whileStmt:
                CollectReferences(whileStmt.Condition, referenced);
                CollectReferences(whileStmt.Body, referenced);
                break;
            case MatchExprNode matchExpr:
                CollectReferences(matchExpr.Scrutinee, referenced);
                foreach (var arm in matchExpr.Arms)
                    CollectReferences(arm.Body, referenced);
                break;
        }
    }
}
