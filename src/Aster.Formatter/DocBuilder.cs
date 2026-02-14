using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.Parser;

namespace Aster.Formatter;

/// <summary>
/// Builds a Doc tree from an Aster AST for pretty-printing.
/// Implements the canonical Aster formatting style.
/// </summary>
public sealed class DocBuilder
{
    private const int IndentSize = 4;

    /// <summary>
    /// Build a document tree from a parsed program.
    /// </summary>
    public Doc Build(ProgramNode program)
    {
        var docs = new List<Doc>();

        foreach (var decl in program.Declarations)
        {
            docs.Add(BuildDeclaration(decl));
            docs.Add(Doc.HardLine);
        }

        return Doc.Concat(docs.ToArray());
    }

    private Doc BuildDeclaration(AstNode node)
    {
        return node switch
        {
            FunctionDeclNode fn => BuildFunction(fn),
            StructDeclNode s => BuildStruct(s),
            EnumDeclNode e => BuildEnum(e),
            TraitDeclNode t => BuildTrait(t),
            ImplDeclNode i => BuildImpl(i),
            ModuleDeclNode m => BuildModule(m),
            _ => Doc.Text(node.ToString() ?? "")
        };
    }

    private Doc BuildFunction(FunctionDeclNode fn)
    {
        var header = Doc.Text("fn ");
        header = Doc.Concat(header, Doc.Text(fn.Name));

        // Parameters
        var parameters = fn.Parameters.Select(BuildParameter);
        header = Doc.Concat(header,
            Doc.Group(Doc.Concat(
                Doc.Text("("),
                Doc.Indent(IndentSize, Doc.Concat(Doc.SoftLine, Doc.Join(Doc.Concat(Doc.Text(","), Doc.Line), parameters))),
                Doc.SoftLine,
                Doc.Text(")")
            ))
        );

        // Return type
        if (fn.ReturnType != null)
        {
            header = Doc.Concat(header, Doc.Text(" -> "), BuildTypeAnnotation(fn.ReturnType));
        }

        // Body
        header = Doc.Concat(header, Doc.Text(" {"));
        header = Doc.Concat(header, Doc.Indent(IndentSize, Doc.Concat(Doc.HardLine, BuildBody(fn.Body))));
        header = Doc.Concat(header, Doc.HardLine, Doc.Text("}"));

        return header;
    }

    private Doc BuildParameter(ParameterNode param)
    {
        var doc = Doc.Empty;
        if (param.IsMutable) doc = Doc.Concat(doc, Doc.Text("mut "));
        doc = Doc.Concat(doc, Doc.Text(param.Name));
        if (param.TypeAnnotation != null)
        {
            doc = Doc.Concat(doc, Doc.Text(": "), BuildTypeAnnotation(param.TypeAnnotation));
        }
        return doc;
    }

    private Doc BuildTypeAnnotation(TypeAnnotationNode type)
    {
        return Doc.Text(type.Name);
    }

    private Doc BuildBody(BlockExprNode block)
    {
        if (block.Statements.Count == 0 && block.TailExpression == null)
            return Doc.Empty;

        var docs = new List<Doc>();
        foreach (var s in block.Statements)
            docs.Add(Doc.Text(s.ToString() ?? ""));
        if (block.TailExpression != null)
            docs.Add(Doc.Text(block.TailExpression.ToString() ?? ""));

        return Doc.Join(Doc.HardLine, docs);
    }

    private Doc BuildStruct(StructDeclNode s)
    {
        var doc = Doc.Text($"struct {s.Name}");
        if (s.GenericParams.Count > 0)
        {
            var generics = s.GenericParams.Select(g => Doc.Text(g.Name));
            doc = Doc.Concat(doc, Doc.Text("<"), Doc.Join(Doc.Text(", "), generics), Doc.Text(">"));
        }
        doc = Doc.Concat(doc, Doc.Text(" {"));
        if (s.Fields.Count > 0)
        {
            var fields = s.Fields.Select(f =>
                Doc.Text($"{f.Name}: {f.TypeAnnotation.Name}"));
            doc = Doc.Concat(doc, Doc.Indent(IndentSize, Doc.Concat(Doc.HardLine, Doc.Join(Doc.Concat(Doc.Text(","), Doc.HardLine), fields))));
            doc = Doc.Concat(doc, Doc.HardLine);
        }
        doc = Doc.Concat(doc, Doc.Text("}"));
        return doc;
    }

    private Doc BuildEnum(EnumDeclNode e)
    {
        var doc = Doc.Text($"enum {e.Name}");
        if (e.GenericParams.Count > 0)
        {
            var generics = e.GenericParams.Select(g => Doc.Text(g.Name));
            doc = Doc.Concat(doc, Doc.Text("<"), Doc.Join(Doc.Text(", "), generics), Doc.Text(">"));
        }
        doc = Doc.Concat(doc, Doc.Text(" {"));
        if (e.Variants.Count > 0)
        {
            var variants = e.Variants.Select(v => Doc.Text(v.Name));
            doc = Doc.Concat(doc, Doc.Indent(IndentSize, Doc.Concat(Doc.HardLine, Doc.Join(Doc.Concat(Doc.Text(","), Doc.HardLine), variants))));
            doc = Doc.Concat(doc, Doc.HardLine);
        }
        doc = Doc.Concat(doc, Doc.Text("}"));
        return doc;
    }

    private Doc BuildTrait(TraitDeclNode t)
    {
        var doc = Doc.Text($"trait {t.Name}");
        if (t.GenericParams.Count > 0)
        {
            var generics = t.GenericParams.Select(g => Doc.Text(g.Name));
            doc = Doc.Concat(doc, Doc.Text("<"), Doc.Join(Doc.Text(", "), generics), Doc.Text(">"));
        }
        doc = Doc.Concat(doc, Doc.Text(" {"));
        if (t.Methods.Count > 0)
        {
            var methods = t.Methods.Select(BuildFunction);
            doc = Doc.Concat(doc, Doc.Indent(IndentSize, Doc.Concat(Doc.HardLine, Doc.Join(Doc.Concat(Doc.HardLine, Doc.HardLine), methods))));
            doc = Doc.Concat(doc, Doc.HardLine);
        }
        doc = Doc.Concat(doc, Doc.Text("}"));
        return doc;
    }

    private Doc BuildImpl(ImplDeclNode i)
    {
        Doc doc;
        if (i.TraitType != null)
            doc = Doc.Concat(Doc.Text($"impl {i.TraitType.Name} for "), Doc.Text(i.TargetType.Name));
        else
            doc = Doc.Text($"impl {i.TargetType.Name}");
        doc = Doc.Concat(doc, Doc.Text(" {"));
        if (i.Methods.Count > 0)
        {
            var methods = i.Methods.Select(BuildFunction);
            doc = Doc.Concat(doc, Doc.Indent(IndentSize, Doc.Concat(Doc.HardLine, Doc.Join(Doc.Concat(Doc.HardLine, Doc.HardLine), methods))));
            doc = Doc.Concat(doc, Doc.HardLine);
        }
        doc = Doc.Concat(doc, Doc.Text("}"));
        return doc;
    }

    private Doc BuildModule(ModuleDeclNode m)
    {
        var doc = Doc.Text($"mod {m.Name}");
        doc = Doc.Concat(doc, Doc.Text(" {"));
        if (m.Members.Count > 0)
        {
            var decls = m.Members.Select(BuildDeclaration);
            doc = Doc.Concat(doc, Doc.Indent(IndentSize, Doc.Concat(Doc.HardLine, Doc.Join(Doc.Concat(Doc.HardLine, Doc.HardLine), decls))));
            doc = Doc.Concat(doc, Doc.HardLine);
        }
        doc = Doc.Concat(doc, Doc.Text("}"));
        return doc;
    }
}
