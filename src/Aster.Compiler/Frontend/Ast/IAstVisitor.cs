namespace Aster.Compiler.Frontend.Ast;

/// <summary>
/// Visitor pattern interface for traversing AST nodes.
/// </summary>
public interface IAstVisitor<T>
{
    T VisitProgram(ProgramNode node);
    T VisitModuleDecl(ModuleDeclNode node);
    T VisitFunctionDecl(FunctionDeclNode node);
    T VisitParameter(ParameterNode node);
    T VisitTypeAnnotation(TypeAnnotationNode node);
    T VisitStructDecl(StructDeclNode node);
    T VisitFieldDecl(FieldDeclNode node);
    T VisitEnumDecl(EnumDeclNode node);
    T VisitEnumVariant(EnumVariantNode node);
    T VisitTraitDecl(TraitDeclNode node);
    T VisitImplDecl(ImplDeclNode node);
    T VisitGenericParam(GenericParamNode node);
    T VisitBlockExpr(BlockExprNode node);
    T VisitIfExpr(IfExprNode node);
    T VisitMatchExpr(MatchExprNode node);
    T VisitMatchArm(MatchArmNode node);
    T VisitPattern(PatternNode node);
    T VisitCallExpr(CallExprNode node);
    T VisitBinaryExpr(BinaryExprNode node);
    T VisitUnaryExpr(UnaryExprNode node);
    T VisitLiteralExpr(LiteralExprNode node);
    T VisitIdentifierExpr(IdentifierExprNode node);
    T VisitPathExpr(PathExprNode node);
    T VisitMemberAccessExpr(MemberAccessExprNode node);
    T VisitIndexExpr(IndexExprNode node);
    T VisitAssignExpr(AssignExprNode node);
    T VisitStructInitExpr(StructInitExprNode node);
    T VisitFieldInit(FieldInitNode node);
    T VisitLetStmt(LetStmtNode node);
    T VisitReturnStmt(ReturnStmtNode node);
    T VisitForStmt(ForStmtNode node);
    T VisitWhileStmt(WhileStmtNode node);
    T VisitBreakStmt(BreakStmtNode node);
    T VisitContinueStmt(ContinueStmtNode node);
    T VisitExpressionStmt(ExpressionStmtNode node);
}
