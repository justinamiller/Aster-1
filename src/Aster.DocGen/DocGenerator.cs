using System.Text;
using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.Parser;

namespace Aster.DocGen;

/// <summary>
/// Generates documentation from Aster source code.
/// Extracts doc comments and function signatures to produce markdown documentation.
/// </summary>
public sealed class DocGenerator
{
    /// <summary>
    /// Generate documentation for a source file.
    /// Returns markdown-formatted documentation.
    /// </summary>
    public string Generate(string source, string fileName = "<stdin>")
    {
        var lexer = new AsterLexer(source, fileName);
        var tokens = lexer.Tokenize();
        if (lexer.Diagnostics.HasErrors)
            return $"# {fileName}\n\nFailed to parse source file.\n";

        var parser = new AsterParser(tokens);
        var program = parser.ParseProgram();
        if (parser.Diagnostics.HasErrors)
            return $"# {fileName}\n\nFailed to parse source file.\n";

        return GenerateFromAst(program, fileName);
    }

    private string GenerateFromAst(ProgramNode program, string fileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {Path.GetFileNameWithoutExtension(fileName)}");
        sb.AppendLine();

        foreach (var decl in program.Declarations)
        {
            switch (decl)
            {
                case FunctionDeclNode fn:
                    sb.AppendLine($"## `fn {fn.Name}`");
                    sb.AppendLine();
                    sb.AppendLine("```aster");
                    sb.Append($"fn {fn.Name}(");
                    sb.Append(string.Join(", ", fn.Parameters.Select(p =>
                        $"{p.Name}: {p.TypeAnnotation?.Name ?? "unknown"}")));
                    sb.Append(')');
                    if (fn.ReturnType != null)
                        sb.Append($" -> {fn.ReturnType.Name}");
                    sb.AppendLine();
                    sb.AppendLine("```");
                    sb.AppendLine();
                    break;

                case StructDeclNode s:
                    sb.AppendLine($"## `struct {s.Name}`");
                    sb.AppendLine();
                    sb.AppendLine("```aster");
                    sb.AppendLine($"struct {s.Name} {{");
                    foreach (var field in s.Fields)
                    {
                        sb.AppendLine($"    {field.Name}: {field.TypeAnnotation?.Name ?? "unknown"},");
                    }
                    sb.AppendLine("}");
                    sb.AppendLine("```");
                    sb.AppendLine();
                    break;

                case EnumDeclNode e:
                    sb.AppendLine($"## `enum {e.Name}`");
                    sb.AppendLine();
                    sb.AppendLine("```aster");
                    sb.AppendLine($"enum {e.Name} {{");
                    foreach (var variant in e.Variants)
                    {
                        sb.AppendLine($"    {variant.Name},");
                    }
                    sb.AppendLine("}");
                    sb.AppendLine("```");
                    sb.AppendLine();
                    break;

                case TraitDeclNode t:
                    sb.AppendLine($"## `trait {t.Name}`");
                    sb.AppendLine();
                    break;
            }
        }

        return sb.ToString();
    }
}
