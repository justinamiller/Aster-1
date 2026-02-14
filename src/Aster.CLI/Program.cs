using Aster.Compiler.Driver;
using Aster.Formatter;
using Aster.Linter;
using Aster.Packages;
using Aster.DocGen;
using Aster.Testing;
using Aster.Lsp;
using System.Text.Json;

namespace Aster.CLI;

/// <summary>
/// Command-line interface for the Aster compiler.
/// Supports: build, run, check, emit-llvm, fmt, lint, init, add, doc, test, lsp
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0];
        return command switch
        {
            "build" => Build(args.Skip(1).ToArray()),
            "check" => Check(args.Skip(1).ToArray()),
            "emit-llvm" => EmitLlvm(args.Skip(1).ToArray()),
            "emit-tokens" => EmitTokens(args.Skip(1).ToArray()),
            "run" => Run(args.Skip(1).ToArray()),
            "fmt" => Fmt(args.Skip(1).ToArray()),
            "lint" => Lint(args.Skip(1).ToArray()),
            "init" => Init(args.Skip(1).ToArray()),
            "add" => Add(args.Skip(1).ToArray()),
            "doc" => Doc(args.Skip(1).ToArray()),
            "test" => Test(args.Skip(1).ToArray()),
            "lsp" => Lsp(),
            "--help" or "-h" => PrintUsage(),
            "--version" or "-v" => PrintVersion(),
            _ => UnknownCommand(command),
        };
    }

    private static int Build(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no input file specified");
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"error: file not found: {filePath}");
            return 1;
        }

        var source = File.ReadAllText(filePath);
        var driver = new CompilationDriver();
        var llvmIr = driver.Compile(source, filePath);

        if (llvmIr == null)
        {
            Console.Error.Write(driver.FormatDiagnostics());
            return 1;
        }

        var outputPath = Path.ChangeExtension(filePath, ".ll");
        File.WriteAllText(outputPath, llvmIr);
        Console.WriteLine($"Compiled {filePath} -> {outputPath}");
        return 0;
    }

    private static int Check(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no input file specified");
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"error: file not found: {filePath}");
            return 1;
        }

        var source = File.ReadAllText(filePath);
        var driver = new CompilationDriver();
        var ok = driver.Check(source, filePath);

        if (!ok)
        {
            Console.Error.Write(driver.FormatDiagnostics());
            return 1;
        }

        Console.WriteLine("Check passed.");
        return 0;
    }

    private static int EmitLlvm(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no input file specified");
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"error: file not found: {filePath}");
            return 1;
        }

        var source = File.ReadAllText(filePath);
        var driver = new CompilationDriver();
        var llvmIr = driver.Compile(source, filePath);

        if (llvmIr == null)
        {
            Console.Error.Write(driver.FormatDiagnostics());
            return 1;
        }

        Console.Write(llvmIr);
        return 0;
    }

    private static int EmitTokens(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no input file specified");
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"error: file not found: {filePath}");
            return 1;
        }

        var source = File.ReadAllText(filePath);
        var driver = new CompilationDriver();
        var tokens = driver.EmitTokens(source, filePath);

        if (tokens == null)
        {
            Console.Error.Write(driver.FormatDiagnostics());
            return 1;
        }

        // Serialize tokens to JSON
        var tokenData = tokens.Select(t => new
        {
            kind = t.Kind.ToString(),
            value = t.Value,
            span = new
            {
                file = t.Span.File,
                line = t.Span.Line,
                column = t.Span.Column,
                start = t.Span.Start,
                length = t.Span.Length
            }
        }).ToList();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(tokenData, options);
        Console.Write(json);
        return 0;
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no input file specified");
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"error: file not found: {filePath}");
            return 1;
        }

        var source = File.ReadAllText(filePath);
        var driver = new CompilationDriver();
        var llvmIr = driver.Compile(source, filePath);

        if (llvmIr == null)
        {
            Console.Error.Write(driver.FormatDiagnostics());
            return 1;
        }

        // Write LLVM IR to temp file
        var llPath = Path.ChangeExtension(filePath, ".ll");
        File.WriteAllText(llPath, llvmIr);
        Console.WriteLine($"LLVM IR written to {llPath}");
        Console.WriteLine("Note: To produce a native binary, run: clang {0} -o output", llPath);
        return 0;
    }

    private static int PrintUsage()
    {
        Console.WriteLine("ASTER Compiler");
        Console.WriteLine();
        Console.WriteLine("Usage: aster <command> [options] <file>");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  build        Compile source to LLVM IR");
        Console.WriteLine("  check        Type-check without compiling");
        Console.WriteLine("  emit-llvm    Emit LLVM IR to stdout");
        Console.WriteLine("  emit-tokens  Emit token stream as JSON (for bootstrap)");
        Console.WriteLine("  run          Compile and prepare for execution");
        Console.WriteLine("  fmt          Format source files");
        Console.WriteLine("  lint         Lint source files");
        Console.WriteLine("  init         Initialize a new package");
        Console.WriteLine("  add          Add a dependency");
        Console.WriteLine("  doc          Generate documentation");
        Console.WriteLine("  test         Run tests");
        Console.WriteLine("  lsp          Start language server");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h     Show this help message");
        Console.WriteLine("  --version, -v  Show version");
        return 0;
    }

    private static int PrintVersion()
    {
        Console.WriteLine("aster 0.2.0");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"error: unknown command '{command}'");
        PrintUsage();
        return 1;
    }

    private static int Fmt(string[] args)
    {
        bool checkMode = false;
        var fileArgs = args.ToList();

        if (fileArgs.Contains("--check"))
        {
            checkMode = true;
            fileArgs.Remove("--check");
        }

        if (fileArgs.Count == 0)
        {
            Console.Error.WriteLine("error: no input file specified");
            return 1;
        }

        var filePath = fileArgs[0];
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"error: file not found: {filePath}");
            return 1;
        }

        var source = File.ReadAllText(filePath);
        var formatter = new AsterFormatter();
        var formatted = formatter.Format(source, filePath);

        if (checkMode)
        {
            if (source != formatted)
            {
                Console.Error.WriteLine($"{filePath}: not formatted");
                return 1;
            }

            Console.WriteLine($"{filePath}: ok");
            return 0;
        }

        File.WriteAllText(filePath, formatted);
        Console.WriteLine($"Formatted {filePath}");
        return 0;
    }

    private static int Lint(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no input file specified");
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"error: file not found: {filePath}");
            return 1;
        }

        var source = File.ReadAllText(filePath);
        var runner = LintRunner.CreateDefault();
        var diagnostics = runner.Lint(source, filePath);

        if (diagnostics.Count == 0)
        {
            Console.WriteLine("No lint issues found.");
            return 0;
        }

        foreach (var d in diagnostics)
        {
            Console.WriteLine($"{d.Span.File}:{d.Span.Line}:{d.Span.Column}: [{d.Severity}] {d.LintId}: {d.Message}");
        }

        return 1;
    }

    private static int Init(string[] args)
    {
        var directory = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        var pm = new PackageManager();
        pm.Init(directory);
        Console.WriteLine($"Initialized package in {directory}");
        return 0;
    }

    private static int Add(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no package name specified");
            return 1;
        }

        var packageName = args[0];
        var version = args.Length > 1 ? args[1] : "*";
        var directory = Directory.GetCurrentDirectory();

        var pm = new PackageManager();
        pm.Add(directory, packageName, version);
        Console.WriteLine($"Added dependency {packageName} ({version})");
        return 0;
    }

    private static int Doc(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no directory specified");
            return 1;
        }

        var directory = args[0];
        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"error: directory not found: {directory}");
            return 1;
        }

        var outputDir = Path.Combine(directory, "docs");
        var builder = new DocSiteBuilder();
        builder.Build(directory, outputDir);
        Console.WriteLine($"Documentation generated in {outputDir}");
        return 0;
    }

    private static int Test(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no directory specified");
            return 1;
        }

        var directory = args[0];
        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"error: directory not found: {directory}");
            return 1;
        }

        var runner = new TestRunner();
        var result = runner.RunDirectory(directory);
        Console.Write(result.Summary());
        return result.AllPassed ? 0 : 1;
    }

    private static int Lsp()
    {
        var server = new LspServer(Console.OpenStandardInput(), Console.OpenStandardOutput());
        server.RunAsync().GetAwaiter().GetResult();
        return 0;
    }
}
