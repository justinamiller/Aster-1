using Aster.Compiler.Frontend.Ast;

namespace Aster.Linter.Rules;

/// <summary>
/// L0002: Detects unreachable code after return, break, or continue statements in blocks.
/// </summary>
public sealed class UnreachableCodeRule : ILintRule
{
    public string LintId => "L0002";
    public string Description => "Detect unreachable code after return/break/continue";
    public LintSeverity DefaultSeverity => LintSeverity.Warning;

    public IReadOnlyList<LintDiagnostic> Check(ProgramNode program)
    {
        var diagnostics = new List<LintDiagnostic>();
        foreach (var decl in program.Declarations)
            WalkNode(decl, diagnostics);
        return diagnostics;
    }

    private static void WalkNode(AstNode node, List<LintDiagnostic> diagnostics)
    {
        switch (node)
        {
            case FunctionDeclNode func:
                CheckBlock(func.Body, diagnostics);
                break;
            case ModuleDeclNode module:
                foreach (var member in module.Members)
                    WalkNode(member, diagnostics);
                break;
            case ImplDeclNode impl:
                foreach (var method in impl.Methods)
                    WalkNode(method, diagnostics);
                break;
            case TraitDeclNode trait:
                foreach (var method in trait.Methods)
                    WalkNode(method, diagnostics);
                break;
        }
    }

    private static void CheckBlock(BlockExprNode block, List<LintDiagnostic> diagnostics)
    {
        var statements = block.Statements;
        for (int i = 0; i < statements.Count; i++)
        {
            var stmt = statements[i];

            // Recurse into nested blocks
            RecurseIntoChildren(stmt, diagnostics);

            // Check if this statement is a terminator (return, break, continue)
            if (IsTerminator(stmt))
            {
                // Any statements after this one are unreachable
                for (int j = i + 1; j < statements.Count; j++)
                {
                    diagnostics.Add(new LintDiagnostic(
                        "L0002",
                        "Unreachable code detected",
                        statements[j].Span,
                        LintSeverity.Warning));
                }

                // Also flag the tail expression as unreachable if present
                if (block.TailExpression is not null)
                {
                    diagnostics.Add(new LintDiagnostic(
                        "L0002",
                        "Unreachable code detected",
                        block.TailExpression.Span,
                        LintSeverity.Warning));
                }

                break; // No need to check further statements
            }
        }
    }

    private static bool IsTerminator(AstNode node) => node is ReturnStmtNode or BreakStmtNode or ContinueStmtNode;

    private static void RecurseIntoChildren(AstNode node, List<LintDiagnostic> diagnostics)
    {
        switch (node)
        {
            case BlockExprNode block:
                CheckBlock(block, diagnostics);
                break;
            case IfExprNode ifExpr:
                CheckBlock(ifExpr.ThenBranch, diagnostics);
                if (ifExpr.ElseBranch is BlockExprNode elseBlock)
                    CheckBlock(elseBlock, diagnostics);
                else if (ifExpr.ElseBranch is not null)
                    RecurseIntoChildren(ifExpr.ElseBranch, diagnostics);
                break;
            case ForStmtNode forStmt:
                CheckBlock(forStmt.Body, diagnostics);
                break;
            case WhileStmtNode whileStmt:
                CheckBlock(whileStmt.Body, diagnostics);
                break;
            case ExpressionStmtNode exprStmt:
                RecurseIntoChildren(exprStmt.Expression, diagnostics);
                break;
            case LetStmtNode let:
                if (let.Initializer is not null)
                    RecurseIntoChildren(let.Initializer, diagnostics);
                break;
            case MatchExprNode matchExpr:
                foreach (var arm in matchExpr.Arms)
                    RecurseIntoChildren(arm.Body, diagnostics);
                break;
        }
    }
}
