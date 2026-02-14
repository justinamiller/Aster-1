using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.Lexer;

namespace Aster.Compiler.Frontend.Parser;

/// <summary>
/// Hand-written recursive descent parser for the Aster language.
/// Implements Pratt parsing for expressions with operator precedence.
/// Supports error recovery to continue parsing after errors.
/// </summary>
public sealed class AsterParser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _position;

    public DiagnosticBag Diagnostics { get; } = new();

    public AsterParser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _position = 0;
    }

    // ========== Entry Point ==========

    /// <summary>Parse a complete program.</summary>
    public ProgramNode ParseProgram()
    {
        var declarations = new List<AstNode>();
        var startSpan = Current.Span;

        while (!IsAtEnd)
        {
            try
            {
                var decl = ParseDeclaration();
                if (decl != null)
                    declarations.Add(decl);
            }
            catch (ParseException)
            {
                Synchronize();
            }
        }

        return new ProgramNode(declarations, startSpan);
    }

    // ========== Declarations ==========

    private AstNode? ParseDeclaration()
    {
        var isPublic = false;
        if (Check(TokenKind.Pub))
        {
            isPublic = true;
            Advance();
        }

        if (Check(TokenKind.Fn) || (Check(TokenKind.Async) && Peek(1).Kind == TokenKind.Fn))
            return ParseFunctionDecl(isPublic);
        if (Check(TokenKind.Struct))
            return ParseStructDecl(isPublic);
        if (Check(TokenKind.Enum))
            return ParseEnumDecl(isPublic);
        if (Check(TokenKind.Trait))
            return ParseTraitDecl(isPublic);
        if (Check(TokenKind.Impl))
            return ParseImplDecl();
        if (Check(TokenKind.Module))
            return ParseModuleDecl(isPublic);
        if (Check(TokenKind.Let))
            return ParseLetStmt();

        // Try parsing as expression statement
        return ParseExpressionStatement();
    }

    private FunctionDeclNode ParseFunctionDecl(bool isPublic)
    {
        var startSpan = Current.Span;
        var isAsync = false;

        if (Check(TokenKind.Async))
        {
            isAsync = true;
            Advance();
        }

        Expect(TokenKind.Fn, "Expected 'fn'");
        var name = ExpectIdentifier("Expected function name");

        Expect(TokenKind.LeftParen, "Expected '(' after function name");
        var parameters = ParseParameterList();
        Expect(TokenKind.RightParen, "Expected ')' after parameters");

        TypeAnnotationNode? returnType = null;
        if (Check(TokenKind.Arrow))
        {
            Advance();
            returnType = ParseTypeAnnotation();
        }

        var body = ParseBlock();

        return new FunctionDeclNode(name, parameters, returnType, body, isPublic, isAsync,
            MakeSpan(startSpan));
    }

    private IReadOnlyList<ParameterNode> ParseParameterList()
    {
        var parameters = new List<ParameterNode>();

        while (!Check(TokenKind.RightParen) && !IsAtEnd)
        {
            var paramSpan = Current.Span;
            var isMut = false;
            if (Check(TokenKind.Mut))
            {
                isMut = true;
                Advance();
            }

            var paramName = ExpectIdentifier("Expected parameter name");

            TypeAnnotationNode? typeAnnotation = null;
            if (Check(TokenKind.Colon))
            {
                Advance();
                typeAnnotation = ParseTypeAnnotation();
            }

            parameters.Add(new ParameterNode(paramName, typeAnnotation, isMut, MakeSpan(paramSpan)));

            if (!Check(TokenKind.RightParen))
                Expect(TokenKind.Comma, "Expected ',' between parameters");
        }

        return parameters;
    }

    private StructDeclNode ParseStructDecl(bool isPublic)
    {
        var startSpan = Current.Span;
        Expect(TokenKind.Struct, "Expected 'struct'");
        var name = ExpectIdentifier("Expected struct name");

        var genericParams = ParseOptionalGenericParams();

        Expect(TokenKind.LeftBrace, "Expected '{' after struct name");

        var fields = new List<FieldDeclNode>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd)
        {
            var fieldPublic = false;
            var fieldSpan = Current.Span;
            if (Check(TokenKind.Pub))
            {
                fieldPublic = true;
                Advance();
            }

            var fieldName = ExpectIdentifier("Expected field name");
            Expect(TokenKind.Colon, "Expected ':' after field name");
            var fieldType = ParseTypeAnnotation();
            fields.Add(new FieldDeclNode(fieldName, fieldType, fieldPublic, MakeSpan(fieldSpan)));

            if (!Check(TokenKind.RightBrace))
            {
                if (Check(TokenKind.Comma))
                    Advance();
            }
        }

        Expect(TokenKind.RightBrace, "Expected '}'");
        return new StructDeclNode(name, fields, isPublic, genericParams, MakeSpan(startSpan));
    }

    private EnumDeclNode ParseEnumDecl(bool isPublic)
    {
        var startSpan = Current.Span;
        Expect(TokenKind.Enum, "Expected 'enum'");
        var name = ExpectIdentifier("Expected enum name");

        var genericParams = ParseOptionalGenericParams();

        Expect(TokenKind.LeftBrace, "Expected '{' after enum name");

        var variants = new List<EnumVariantNode>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd)
        {
            var variantSpan = Current.Span;
            var variantName = ExpectIdentifier("Expected variant name");
            var variantFields = new List<TypeAnnotationNode>();

            if (Check(TokenKind.LeftParen))
            {
                Advance();
                while (!Check(TokenKind.RightParen) && !IsAtEnd)
                {
                    variantFields.Add(ParseTypeAnnotation());
                    if (!Check(TokenKind.RightParen))
                        Expect(TokenKind.Comma, "Expected ','");
                }
                Expect(TokenKind.RightParen, "Expected ')'");
            }

            variants.Add(new EnumVariantNode(variantName, variantFields, MakeSpan(variantSpan)));

            if (!Check(TokenKind.RightBrace))
            {
                if (Check(TokenKind.Comma))
                    Advance();
            }
        }

        Expect(TokenKind.RightBrace, "Expected '}'");
        return new EnumDeclNode(name, variants, isPublic, genericParams, MakeSpan(startSpan));
    }

    private TraitDeclNode ParseTraitDecl(bool isPublic)
    {
        var startSpan = Current.Span;
        Expect(TokenKind.Trait, "Expected 'trait'");
        var name = ExpectIdentifier("Expected trait name");

        var genericParams = ParseOptionalGenericParams();

        Expect(TokenKind.LeftBrace, "Expected '{' after trait name");

        var methods = new List<FunctionDeclNode>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd)
        {
            methods.Add(ParseFunctionDecl(false));
        }

        Expect(TokenKind.RightBrace, "Expected '}'");
        return new TraitDeclNode(name, methods, isPublic, genericParams, MakeSpan(startSpan));
    }

    private ImplDeclNode ParseImplDecl()
    {
        var startSpan = Current.Span;
        Expect(TokenKind.Impl, "Expected 'impl'");
        var targetType = ParseTypeAnnotation();

        TypeAnnotationNode? traitType = null;
        if (Check(TokenKind.For))
        {
            Advance();
            traitType = targetType;
            targetType = ParseTypeAnnotation();
        }

        Expect(TokenKind.LeftBrace, "Expected '{'");

        var methods = new List<FunctionDeclNode>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd)
        {
            var methodPublic = false;
            if (Check(TokenKind.Pub))
            {
                methodPublic = true;
                Advance();
            }
            methods.Add(ParseFunctionDecl(methodPublic));
        }

        Expect(TokenKind.RightBrace, "Expected '}'");
        return new ImplDeclNode(targetType, traitType, methods, MakeSpan(startSpan));
    }

    private ModuleDeclNode ParseModuleDecl(bool isPublic)
    {
        var startSpan = Current.Span;
        Expect(TokenKind.Module, "Expected 'module'");
        var name = ExpectIdentifier("Expected module name");
        Expect(TokenKind.LeftBrace, "Expected '{'");

        var members = new List<AstNode>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd)
        {
            var decl = ParseDeclaration();
            if (decl != null)
                members.Add(decl);
        }

        Expect(TokenKind.RightBrace, "Expected '}'");
        return new ModuleDeclNode(name, members, MakeSpan(startSpan));
    }

    // ========== Statements ==========

    private LetStmtNode ParseLetStmt()
    {
        var startSpan = Current.Span;
        Expect(TokenKind.Let, "Expected 'let'");

        var isMut = false;
        if (Check(TokenKind.Mut))
        {
            isMut = true;
            Advance();
        }

        var name = ExpectIdentifier("Expected variable name");

        TypeAnnotationNode? typeAnnotation = null;
        if (Check(TokenKind.Colon))
        {
            Advance();
            typeAnnotation = ParseTypeAnnotation();
        }

        AstNode? initializer = null;
        if (Check(TokenKind.Equals))
        {
            Advance();
            initializer = ParseExpression();
        }

        // Optional semicolon
        if (Check(TokenKind.Semicolon))
            Advance();

        return new LetStmtNode(name, isMut, typeAnnotation, initializer, MakeSpan(startSpan));
    }

    private ReturnStmtNode ParseReturnStmt()
    {
        var startSpan = Current.Span;
        Expect(TokenKind.Return, "Expected 'return'");

        AstNode? value = null;
        if (!Check(TokenKind.Semicolon) && !Check(TokenKind.RightBrace) && !IsAtEnd)
        {
            value = ParseExpression();
        }

        if (Check(TokenKind.Semicolon))
            Advance();

        return new ReturnStmtNode(value, MakeSpan(startSpan));
    }

    private ForStmtNode ParseForStmt()
    {
        var startSpan = Current.Span;
        Expect(TokenKind.For, "Expected 'for'");
        var variable = ExpectIdentifier("Expected loop variable");
        Expect(TokenKind.Identifier, "Expected 'in'"); // "in" is used as contextual keyword
        var iterable = ParseExpression();
        var body = ParseBlock();

        return new ForStmtNode(variable, iterable, body, MakeSpan(startSpan));
    }

    private WhileStmtNode ParseWhileStmt()
    {
        var startSpan = Current.Span;
        Expect(TokenKind.While, "Expected 'while'");
        var condition = ParseExpression();
        var body = ParseBlock();

        return new WhileStmtNode(condition, body, MakeSpan(startSpan));
    }

    private AstNode ParseExpressionStatement()
    {
        var startSpan = Current.Span;
        var expr = ParseExpression();

        if (Check(TokenKind.Semicolon))
            Advance();

        return new ExpressionStmtNode(expr, MakeSpan(startSpan));
    }

    // ========== Expressions (Pratt Parser) ==========

    private AstNode ParseExpression(int minPrecedence = 0)
    {
        var left = ParseUnaryExpression();

        while (!IsAtEnd)
        {
            var (prec, assoc) = GetBinaryPrecedence(Current.Kind);
            if (prec < minPrecedence)
                break;

            var opToken = Current;
            var op = TokenToBinaryOp(opToken.Kind);
            Advance();

            var rightPrec = assoc == Associativity.Left ? prec + 1 : prec;
            var right = ParseExpression(rightPrec);

            left = new BinaryExprNode(left, op, right, MakeSpan(left.Span));
        }

        return left;
    }

    private AstNode ParseUnaryExpression()
    {
        if (Check(TokenKind.Minus) || Check(TokenKind.Bang) || Check(TokenKind.Tilde) || Check(TokenKind.Ampersand))
        {
            var startSpan = Current.Span;
            var opKind = Current.Kind;
            Advance();

            UnaryOperator op;
            if (opKind == TokenKind.Ampersand)
            {
                if (Check(TokenKind.Mut))
                {
                    Advance();
                    op = UnaryOperator.MutRef;
                }
                else
                {
                    op = UnaryOperator.Ref;
                }
            }
            else
            {
                op = opKind switch
                {
                    TokenKind.Minus => UnaryOperator.Negate,
                    TokenKind.Bang => UnaryOperator.Not,
                    TokenKind.Tilde => UnaryOperator.BitNot,
                    _ => UnaryOperator.Not,
                };
            }

            var operand = ParseUnaryExpression();
            return new UnaryExprNode(op, operand, MakeSpan(startSpan));
        }

        return ParsePostfixExpression();
    }

    private AstNode ParsePostfixExpression()
    {
        var expr = ParsePrimaryExpression();

        while (true)
        {
            if (Check(TokenKind.LeftParen))
            {
                Advance();
                var args = new List<AstNode>();
                while (!Check(TokenKind.RightParen) && !IsAtEnd)
                {
                    args.Add(ParseExpression());
                    if (!Check(TokenKind.RightParen))
                        Expect(TokenKind.Comma, "Expected ','");
                }
                Expect(TokenKind.RightParen, "Expected ')'");
                expr = new CallExprNode(expr, args, MakeSpan(expr.Span));
            }
            else if (Check(TokenKind.Dot))
            {
                Advance();
                var member = ExpectIdentifier("Expected member name");
                expr = new MemberAccessExprNode(expr, member, MakeSpan(expr.Span));
            }
            else if (Check(TokenKind.LeftBracket))
            {
                Advance();
                var index = ParseExpression();
                Expect(TokenKind.RightBracket, "Expected ']'");
                expr = new IndexExprNode(expr, index, MakeSpan(expr.Span));
            }
            else
            {
                break;
            }
        }

        // Assignment
        if (Check(TokenKind.Equals))
        {
            Advance();
            var value = ParseExpression();
            return new AssignExprNode(expr, value, MakeSpan(expr.Span));
        }

        return expr;
    }

    private AstNode ParsePrimaryExpression()
    {
        var span = Current.Span;

        // Literals
        if (Check(TokenKind.IntegerLiteral))
        {
            var text = Current.Value;
            Advance();
            if (long.TryParse(text, out var intVal))
                return new LiteralExprNode(intVal, LiteralKind.Integer, span);
            return new LiteralExprNode(0L, LiteralKind.Integer, span);
        }

        if (Check(TokenKind.FloatLiteral))
        {
            var text = Current.Value;
            Advance();
            if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
                return new LiteralExprNode(floatVal, LiteralKind.Float, span);
            return new LiteralExprNode(0.0, LiteralKind.Float, span);
        }

        if (Check(TokenKind.StringLiteral))
        {
            var value = Current.Value;
            Advance();
            return new LiteralExprNode(value, LiteralKind.String, span);
        }

        if (Check(TokenKind.CharLiteral))
        {
            var value = Current.Value;
            Advance();
            return new LiteralExprNode(value.Length > 0 ? value[0] : '\0', LiteralKind.Char, span);
        }

        if (Check(TokenKind.True))
        {
            Advance();
            return new LiteralExprNode(true, LiteralKind.Bool, span);
        }

        if (Check(TokenKind.False))
        {
            Advance();
            return new LiteralExprNode(false, LiteralKind.Bool, span);
        }

        // Identifier or struct initialization
        if (Check(TokenKind.Identifier))
        {
            var name = Current.Value;
            var nameSpan = Current.Span;
            Advance();
            
            // Check for struct initialization: Identifier { ... }
            if (Check(TokenKind.LeftBrace))
            {
                return ParseStructInit(name, nameSpan);
            }
            
            return new IdentifierExprNode(name, span);
        }

        // Grouped expression
        if (Check(TokenKind.LeftParen))
        {
            Advance();
            var expr = ParseExpression();
            Expect(TokenKind.RightParen, "Expected ')'");
            return expr;
        }

        // If expression
        if (Check(TokenKind.If))
            return ParseIfExpr();

        // Match expression
        if (Check(TokenKind.Match))
            return ParseMatchExpr();

        // Block expression
        if (Check(TokenKind.LeftBrace))
            return ParseBlock();

        // Return statement (can appear as expression)
        if (Check(TokenKind.Return))
            return ParseReturnStmt();

        // While
        if (Check(TokenKind.While))
            return ParseWhileStmt();

        // For
        if (Check(TokenKind.For))
            return ParseForStmt();

        // Break
        if (Check(TokenKind.Break))
        {
            Advance();
            return new BreakStmtNode(span);
        }

        // Continue
        if (Check(TokenKind.Continue))
        {
            Advance();
            return new ContinueStmtNode(span);
        }

        // Let (as expression/statement)
        if (Check(TokenKind.Let))
            return ParseLetStmt();

        ReportError("E0100", $"Unexpected token '{Current.Value}'");
        Advance();
        return new LiteralExprNode(0L, LiteralKind.Integer, span);
    }

    private IfExprNode ParseIfExpr()
    {
        var startSpan = Current.Span;
        Expect(TokenKind.If, "Expected 'if'");
        var condition = ParseExpression();
        var thenBranch = ParseBlock();

        AstNode? elseBranch = null;
        if (Check(TokenKind.Else))
        {
            Advance();
            if (Check(TokenKind.If))
                elseBranch = ParseIfExpr();
            else
                elseBranch = ParseBlock();
        }

        return new IfExprNode(condition, thenBranch, elseBranch, MakeSpan(startSpan));
    }

    private MatchExprNode ParseMatchExpr()
    {
        var startSpan = Current.Span;
        Expect(TokenKind.Match, "Expected 'match'");
        var scrutinee = ParseExpression();
        Expect(TokenKind.LeftBrace, "Expected '{'");

        var arms = new List<MatchArmNode>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd)
        {
            var armSpan = Current.Span;
            var pattern = ParsePattern();
            Expect(TokenKind.FatArrow, "Expected '=>'");
            var body = ParseExpression();
            arms.Add(new MatchArmNode(pattern, body, MakeSpan(armSpan)));

            if (!Check(TokenKind.RightBrace))
            {
                if (Check(TokenKind.Comma))
                    Advance();
            }
        }

        Expect(TokenKind.RightBrace, "Expected '}'");
        return new MatchExprNode(scrutinee, arms, MakeSpan(startSpan));
    }

    private PatternNode ParsePattern()
    {
        var span = Current.Span;

        // Wildcard pattern: _
        if (Check(TokenKind.Identifier) && Current.Value == "_")
        {
            Advance();
            return new PatternNode("_", Array.Empty<PatternNode>(), true, null, span);
        }

        // Literal patterns
        if (Check(TokenKind.IntegerLiteral) || Check(TokenKind.FloatLiteral) ||
            Check(TokenKind.StringLiteral) || Check(TokenKind.CharLiteral) ||
            Check(TokenKind.True) || Check(TokenKind.False))
        {
            var lit = (LiteralExprNode)ParsePrimaryExpression();
            return new PatternNode("", Array.Empty<PatternNode>(), false, lit, span);
        }

        // Named/constructor pattern
        if (Check(TokenKind.Identifier))
        {
            var name = Current.Value;
            Advance();

            var subPatterns = new List<PatternNode>();
            if (Check(TokenKind.LeftParen))
            {
                Advance();
                while (!Check(TokenKind.RightParen) && !IsAtEnd)
                {
                    subPatterns.Add(ParsePattern());
                    if (!Check(TokenKind.RightParen))
                        Expect(TokenKind.Comma, "Expected ','");
                }
                Expect(TokenKind.RightParen, "Expected ')'");
            }

            return new PatternNode(name, subPatterns, false, null, MakeSpan(span));
        }

        ReportError("E0101", "Expected pattern");
        Advance();
        return new PatternNode("_", Array.Empty<PatternNode>(), true, null, span);
    }

    private StructInitExprNode ParseStructInit(string structName, Span nameSpan)
    {
        var startSpan = nameSpan;
        Expect(TokenKind.LeftBrace, "Expected '{'");
        
        var fields = new List<FieldInitNode>();
        
        while (!Check(TokenKind.RightBrace) && !IsAtEnd)
        {
            var fieldSpan = Current.Span;
            var fieldName = ExpectIdentifier("Expected field name");
            Expect(TokenKind.Colon, "Expected ':' after field name");
            var fieldValue = ParseExpression();
            
            fields.Add(new FieldInitNode(fieldName, fieldValue, MakeSpan(fieldSpan)));
            
            if (!Check(TokenKind.RightBrace))
            {
                if (Check(TokenKind.Comma))
                    Advance();
                else
                    break; // Allow trailing comma to be optional
            }
        }
        
        Expect(TokenKind.RightBrace, "Expected '}'");
        return new StructInitExprNode(structName, fields, MakeSpan(startSpan));
    }

    private BlockExprNode ParseBlock()
    {
        var startSpan = Current.Span;
        Expect(TokenKind.LeftBrace, "Expected '{'");

        var statements = new List<AstNode>();
        AstNode? tailExpr = null;

        while (!Check(TokenKind.RightBrace) && !IsAtEnd)
        {
            if (Check(TokenKind.Let))
            {
                statements.Add(ParseLetStmt());
            }
            else if (Check(TokenKind.Return))
            {
                statements.Add(ParseReturnStmt());
            }
            else if (Check(TokenKind.While))
            {
                statements.Add(ParseWhileStmt());
            }
            else if (Check(TokenKind.For))
            {
                statements.Add(ParseForStmt());
            }
            else if (Check(TokenKind.Break))
            {
                var s = Current.Span;
                Advance();
                statements.Add(new BreakStmtNode(s));
                if (Check(TokenKind.Semicolon)) Advance();
            }
            else if (Check(TokenKind.Continue))
            {
                var s = Current.Span;
                Advance();
                statements.Add(new ContinueStmtNode(s));
                if (Check(TokenKind.Semicolon)) Advance();
            }
            else
            {
                var expr = ParseExpression();

                if (Check(TokenKind.Semicolon))
                {
                    Advance();
                    statements.Add(new ExpressionStmtNode(expr, expr.Span));
                }
                else if (Check(TokenKind.RightBrace))
                {
                    // This is the tail expression
                    tailExpr = expr;
                }
                else
                {
                    statements.Add(new ExpressionStmtNode(expr, expr.Span));
                }
            }
        }

        Expect(TokenKind.RightBrace, "Expected '}'");
        return new BlockExprNode(statements, tailExpr, MakeSpan(startSpan));
    }

    // ========== Types ==========

    private TypeAnnotationNode ParseTypeAnnotation()
    {
        var span = Current.Span;
        var name = ExpectIdentifier("Expected type name");

        var typeArgs = new List<TypeAnnotationNode>();
        if (Check(TokenKind.Less))
        {
            Advance();
            while (!Check(TokenKind.Greater) && !IsAtEnd)
            {
                typeArgs.Add(ParseTypeAnnotation());
                if (!Check(TokenKind.Greater))
                    Expect(TokenKind.Comma, "Expected ','");
            }
            Expect(TokenKind.Greater, "Expected '>'");
        }

        return new TypeAnnotationNode(name, typeArgs, MakeSpan(span));
    }

    private IReadOnlyList<GenericParamNode> ParseOptionalGenericParams()
    {
        if (!Check(TokenKind.Less))
            return Array.Empty<GenericParamNode>();

        Advance();
        var genericParams = new List<GenericParamNode>();
        while (!Check(TokenKind.Greater) && !IsAtEnd)
        {
            var paramSpan = Current.Span;
            var paramName = ExpectIdentifier("Expected generic parameter name");
            var bounds = new List<TypeAnnotationNode>();

            if (Check(TokenKind.Colon))
            {
                Advance();
                bounds.Add(ParseTypeAnnotation());
                while (Check(TokenKind.Plus))
                {
                    Advance();
                    bounds.Add(ParseTypeAnnotation());
                }
            }

            genericParams.Add(new GenericParamNode(paramName, bounds, MakeSpan(paramSpan)));

            if (!Check(TokenKind.Greater))
                Expect(TokenKind.Comma, "Expected ','");
        }

        Expect(TokenKind.Greater, "Expected '>'");
        return genericParams;
    }

    // ========== Operator Precedence ==========

    private static (int precedence, Associativity assoc) GetBinaryPrecedence(TokenKind kind) => kind switch
    {
        TokenKind.PipePipe => (1, Associativity.Left),
        TokenKind.AmpersandAmpersand => (2, Associativity.Left),
        TokenKind.Pipe => (3, Associativity.Left),
        TokenKind.Caret => (4, Associativity.Left),
        TokenKind.Ampersand => (5, Associativity.Left),
        TokenKind.EqualsEquals or TokenKind.BangEquals => (6, Associativity.Left),
        TokenKind.Less or TokenKind.Greater or TokenKind.LessEquals or TokenKind.GreaterEquals => (7, Associativity.Left),
        TokenKind.DotDot => (8, Associativity.Left),
        TokenKind.Plus or TokenKind.Minus => (9, Associativity.Left),
        TokenKind.Star or TokenKind.Slash or TokenKind.Percent => (10, Associativity.Left),
        _ => (-1, Associativity.Left),
    };

    private static BinaryOperator TokenToBinaryOp(TokenKind kind) => kind switch
    {
        TokenKind.Plus => BinaryOperator.Add,
        TokenKind.Minus => BinaryOperator.Sub,
        TokenKind.Star => BinaryOperator.Mul,
        TokenKind.Slash => BinaryOperator.Div,
        TokenKind.Percent => BinaryOperator.Mod,
        TokenKind.AmpersandAmpersand => BinaryOperator.And,
        TokenKind.PipePipe => BinaryOperator.Or,
        TokenKind.Ampersand => BinaryOperator.BitAnd,
        TokenKind.Pipe => BinaryOperator.BitOr,
        TokenKind.Caret => BinaryOperator.BitXor,
        TokenKind.EqualsEquals => BinaryOperator.Eq,
        TokenKind.BangEquals => BinaryOperator.Ne,
        TokenKind.Less => BinaryOperator.Lt,
        TokenKind.LessEquals => BinaryOperator.Le,
        TokenKind.Greater => BinaryOperator.Gt,
        TokenKind.GreaterEquals => BinaryOperator.Ge,
        TokenKind.DotDot => BinaryOperator.Range,
        _ => BinaryOperator.Add,
    };

    private enum Associativity { Left, Right }

    // ========== Helpers ==========

    private Token Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    private Token Peek(int offset)
    {
        var idx = _position + offset;
        return idx < _tokens.Count ? _tokens[idx] : _tokens[^1];
    }

    private bool IsAtEnd => _position >= _tokens.Count || Current.Kind == TokenKind.Eof;

    private bool Check(TokenKind kind) => !IsAtEnd && Current.Kind == kind;

    private Token Advance()
    {
        var token = Current;
        if (!IsAtEnd) _position++;
        return token;
    }

    private void Expect(TokenKind kind, string message)
    {
        if (!Check(kind))
        {
            ReportError("E0102", $"{message}, found '{Current.Value}'");
            return;
        }
        Advance();
    }

    private string ExpectIdentifier(string message)
    {
        if (!Check(TokenKind.Identifier))
        {
            ReportError("E0103", $"{message}, found '{Current.Value}'");
            return "<error>";
        }
        var name = Current.Value;
        Advance();
        return name;
    }

    private void ReportError(string code, string message)
    {
        Diagnostics.ReportError(code, message, Current.Span);
        throw new ParseException(message);
    }

    private Span MakeSpan(Span start)
    {
        var end = _position > 0 ? _tokens[_position - 1] : Current;
        return new Span(start.File, start.Line, start.Column, start.Start, end.Span.End - start.Start);
    }

    private void Synchronize()
    {
        while (!IsAtEnd)
        {
            if (Current.Kind == TokenKind.Semicolon)
            {
                Advance();
                return;
            }

            switch (Current.Kind)
            {
                case TokenKind.Fn:
                case TokenKind.Let:
                case TokenKind.Struct:
                case TokenKind.Enum:
                case TokenKind.Trait:
                case TokenKind.Impl:
                case TokenKind.Module:
                case TokenKind.Pub:
                case TokenKind.If:
                case TokenKind.For:
                case TokenKind.While:
                case TokenKind.Return:
                    return;
            }

            Advance();
        }
    }

    private sealed class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
    }
}
