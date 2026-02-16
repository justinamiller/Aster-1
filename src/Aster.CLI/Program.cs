using Aster.Compiler.Driver;
using Aster.Compiler.Telemetry;
using Aster.Compiler.Observability;
using Aster.Cli.Diagnostics;
using Aster.Formatter;
using Aster.Linter;
using Aster.Packages;
using Aster.DocGen;
using Aster.Testing;
using Aster.Lsp;
using Aster.Compiler.Fuzzing;
using Aster.Compiler.Fuzzing.Harnesses;
using Aster.Compiler.Differential;
using Aster.Compiler.Reducers;
using System.Text.Json;

namespace Aster.CLI;

/// <summary>
/// Command-line interface for the Aster compiler.
/// Supports: build, run, check, emit-llvm, emit-tokens, fmt, lint, init, add, doc, test, lsp, fuzz, differential, reduce, explain, crash-report
/// </summary>
public static class Program
{
    private static string _lastPhase = "startup";
    private static readonly List<string> _recentDiagnostics = new();

    public static int Main(string[] args)
    {
        // Install global crash handler
        CrashReporter.InstallGlobalHandler(
            "0.2.0",
            () => string.Join(" ", args),
            () => _lastPhase,
            () => _recentDiagnostics);

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
            "emit-ast-json" => EmitAstJson(args.Skip(1).ToArray()),
            "emit-symbols-json" => EmitSymbolsJson(args.Skip(1).ToArray()),
            "run" => Run(args.Skip(1).ToArray()),
            "fmt" => Fmt(args.Skip(1).ToArray()),
            "lint" => Lint(args.Skip(1).ToArray()),
            "init" => Init(args.Skip(1).ToArray()),
            "add" => Add(args.Skip(1).ToArray()),
            "doc" => Doc(args.Skip(1).ToArray()),
            "test" => Test(args.Skip(1).ToArray()),
            "lsp" => Lsp(),
            "fuzz" => Fuzz(args.Skip(1).ToArray()),
            "differential" => Differential(args.Skip(1).ToArray()),
            "reduce" => Reduce(args.Skip(1).ToArray()),
            "explain" => Explain(args.Skip(1).ToArray()),
            "crash-report" => CrashReport(args.Skip(1).ToArray()),
            "--help" or "-h" => PrintUsage(),
            "--version" or "-v" => PrintVersion(),
            _ => UnknownCommand(command),
        };
    }

    private static int Build(string[] args)
    {
        bool stage1Mode = false;
        string? outputPath = null;
        var inputFiles = new List<string>();

        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--stage1")
            {
                stage1Mode = true;
            }
            else if (args[i] == "-o" && i + 1 < args.Length)
            {
                outputPath = args[i + 1];
                i++; // Skip next argument
            }
            else if (!args[i].StartsWith("--") && !args[i].StartsWith("-"))
            {
                inputFiles.Add(args[i]);
            }
        }

        if (inputFiles.Count == 0)
        {
            Console.Error.WriteLine("error: no input file specified");
            return 1;
        }

        // Verify all input files exist
        foreach (var file in inputFiles)
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"error: file not found: {file}");
                return 1;
            }
        }

        // For now, compile files individually and merge LLVM IR
        // In a full implementation, we'd have a proper linker
        var allLlvmIr = new List<string>();
        var driver = new CompilationDriver(stage1Mode);

        foreach (var filePath in inputFiles)
        {
            var source = File.ReadAllText(filePath);
            var llvmIr = driver.Compile(source, filePath);

            if (llvmIr == null)
            {
                Console.Error.Write(driver.FormatDiagnostics());
                return 1;
            }

            allLlvmIr.Add(llvmIr);
        }

        // Merge LLVM IR (simplified - just concatenate for now)
        var mergedLlvmIr = string.Join("\n\n", allLlvmIr);

        // Determine output path
        if (outputPath == null)
        {
            // Default: use first input file name with .ll extension
            outputPath = Path.ChangeExtension(inputFiles[0], ".ll");
        }

        // If output path doesn't have .ll extension, we want to create a native executable
        bool createExecutable = !outputPath.EndsWith(".ll", StringComparison.OrdinalIgnoreCase);

        if (createExecutable)
        {
            // Write LLVM IR to temporary file
            var tempLlFile = Path.GetTempFileName() + ".ll";
            File.WriteAllText(tempLlFile, mergedLlvmIr);
            
            // Also save a copy for debugging
            var debugLlFile = outputPath + ".ll";
            File.WriteAllText(debugLlFile, mergedLlvmIr);

            try
            {
                // Invoke clang to create native executable
                var result = CompileToNative(tempLlFile, outputPath);
                if (result != 0)
                {
                    Console.Error.WriteLine("error: failed to create native executable");
                    Console.Error.WriteLine($"LLVM IR saved to: {debugLlFile}");
                    Console.Error.WriteLine("Make sure clang is installed and in your PATH");
                    return 1;
                }

                if (stage1Mode)
                {
                    Console.WriteLine($"Compiled {inputFiles.Count} file(s) -> {outputPath} [Stage1 mode]");
                }
                else
                {
                    Console.WriteLine($"Compiled {inputFiles.Count} file(s) -> {outputPath}");
                }
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(tempLlFile))
                {
                    try { File.Delete(tempLlFile); } catch { }
                }
            }
        }
        else
        {
            // Just write LLVM IR
            File.WriteAllText(outputPath, mergedLlvmIr);
            
            if (stage1Mode)
            {
                Console.WriteLine($"Compiled {inputFiles.Count} file(s) -> {outputPath} [Stage1 mode]");
            }
            else
            {
                Console.WriteLine($"Compiled {inputFiles.Count} file(s) -> {outputPath}");
            }
        }
        
        return 0;
    }

    /// <summary>
    /// Compile LLVM IR to native executable using clang.
    /// </summary>
    private static int CompileToNative(string llvmIrPath, string outputPath)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "clang",
                Arguments = $"\"{llvmIrPath}\" -o \"{outputPath}\" -Wno-override-module",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                Console.Error.WriteLine("error: failed to start clang process");
                return 1;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(output))
                    Console.Error.WriteLine(output);
                if (!string.IsNullOrWhiteSpace(error))
                    Console.Error.WriteLine(error);
                return process.ExitCode;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static int Check(string[] args)
    {
        bool stage1Mode = false;
        string? filePath = null;

        // Parse arguments
        foreach (var arg in args)
        {
            if (arg == "--stage1")
            {
                stage1Mode = true;
            }
            else if (!arg.StartsWith("--") && filePath == null)
            {
                filePath = arg;
            }
        }

        if (filePath == null)
        {
            Console.Error.WriteLine("error: no input file specified");
            return 1;
        }

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"error: file not found: {filePath}");
            return 1;
        }

        var source = File.ReadAllText(filePath);
        var driver = new CompilationDriver(stage1Mode);
        var ok = driver.Check(source, filePath);

        if (!ok)
        {
            Console.Error.Write(driver.FormatDiagnostics());
            return 1;
        }

        if (stage1Mode)
        {
            Console.WriteLine("Check passed. [Stage1 mode]");
        }
        else
        {
            Console.WriteLine("Check passed.");
        }
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

    private static int EmitAstJson(string[] args)
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
        var ast = driver.EmitAst(source, filePath);

        if (ast == null)
        {
            Console.Error.Write(driver.FormatDiagnostics());
            return 1;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };

        var json = JsonSerializer.Serialize(ast, options);
        Console.Write(json);
        return 0;
    }

    private static int EmitSymbolsJson(string[] args)
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
        var symbols = driver.EmitSymbols(source, filePath);

        if (symbols == null)
        {
            Console.Error.Write(driver.FormatDiagnostics());
            return 1;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };

        var json = JsonSerializer.Serialize(symbols, options);
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
        Console.WriteLine("  build         Compile source to LLVM IR");
        Console.WriteLine("  check         Type-check without compiling");
        Console.WriteLine("  emit-llvm     Emit LLVM IR to stdout");
        Console.WriteLine("  emit-tokens   Emit token stream as JSON (for differential testing)");
        Console.WriteLine("  emit-ast-json Emit AST as JSON (for differential testing)");
        Console.WriteLine("  emit-symbols-json Emit symbol table as JSON (for differential testing)");
        Console.WriteLine("  run           Compile and prepare for execution");
        Console.WriteLine("  fmt           Format source files");
        Console.WriteLine("  lint          Lint source files");
        Console.WriteLine("  init          Initialize a new package");
        Console.WriteLine("  add           Add a dependency");
        Console.WriteLine("  doc           Generate documentation");
        Console.WriteLine("  test          Run tests");
        Console.WriteLine("  lsp           Start language server");
        Console.WriteLine("  fuzz          Run fuzzing harness");
        Console.WriteLine("  differential  Run differential testing");
        Console.WriteLine("  reduce        Reduce/minimize a test case");
        Console.WriteLine("  explain       Explain a diagnostic code");
        Console.WriteLine("  crash-report  View crash report details");
        Console.WriteLine();
        Console.WriteLine("  Options:");
        Console.WriteLine("  --stage1       Enforce Stage1 (Core-0) language subset");
        Console.WriteLine("  --help, -h     Show this help message");
        Console.WriteLine("  --version, -v  Show version");
        return 0;
    }

    private static int PrintVersion()
    {
        // Try to read version from VERSION file
        var versionFilePath = Path.Combine(AppContext.BaseDirectory, "VERSION");
        var version = "0.2.0"; // Default fallback
        
        if (File.Exists(versionFilePath))
        {
            try
            {
                version = File.ReadAllText(versionFilePath).Trim();
            }
            catch
            {
                // Fallback to default
            }
        }
        
        Console.WriteLine($"aster {version}");
        Console.WriteLine();
        Console.WriteLine("ASTER Programming Language Compiler");
        Console.WriteLine("Copyright (c) 2026 Aster Project");
        Console.WriteLine();
        Console.WriteLine("Features:");
        Console.WriteLine("  - Full ahead-of-time compilation to native code");
        Console.WriteLine("  - Effect system (io, alloc, async, unsafe, ffi)");
        Console.WriteLine("  - Ownership and borrowing");
        Console.WriteLine("  - Stage1 self-hosting support (--stage1 flag)");
        Console.WriteLine();
        Console.WriteLine("For more information, visit: https://github.com/justinamiller/Aster-1");
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

    private static int Fuzz(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no harness specified");
            Console.Error.WriteLine("Available harnesses: parser, typesystem, mirbuilder, optimizer");
            return 1;
        }

        var harness = args[0].ToLower();
        var smoke = args.Contains("--smoke");
        var seed = 0;
        
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--seed" && int.TryParse(args[i + 1], out var parsedSeed))
            {
                seed = parsedSeed;
            }
        }

        var config = smoke ? FuzzConfig.Smoke(seed) : FuzzConfig.Nightly(seed);

        FuzzRunner runner = harness switch
        {
            "parser" => new ParserFuzz(config),
            "typesystem" => new TypeSystemFuzz(config),
            "mirbuilder" => new MirBuilderFuzz(config),
            "optimizer" => new OptimizerFuzz(config),
            _ => throw new ArgumentException($"Unknown harness: {harness}")
        };

        var summary = runner.Run();
        Console.WriteLine();
        Console.WriteLine(summary);

        return summary.Crashes + summary.WrongCode + summary.Hangs > 0 ? 1 : 0;
    }

    private static int Differential(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no test directory specified");
            return 1;
        }

        var directory = args[0];
        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"error: directory not found: {directory}");
            return 1;
        }

        var config = new DiffConfig();
        var runner = new DifferentialTestRunner(config);
        var summary = runner.TestDirectory(directory);

        Console.WriteLine();
        Console.WriteLine(summary);

        return summary.Mismatches > 0 ? 1 : 0;
    }

    private static int Reduce(string[] args)
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

        var input = File.ReadAllText(filePath);

        // Simple predicate: file still causes a compilation error
        bool IsInteresting(string source)
        {
            var driver = new CompilationDriver();
            var result = driver.Compile(source, "reduce.ast");
            return result == null; // Interesting if it fails
        }

        var reducer = new DeltaReducer(IsInteresting);
        var reduced = reducer.Reduce(input);

        var outputPath = Path.ChangeExtension(filePath, ".reduced.ast");
        File.WriteAllText(outputPath, reduced);
        Console.WriteLine($"Reduced test case saved to: {outputPath}");

        return 0;
    }

    private static int Explain(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no diagnostic code specified");
            Console.Error.WriteLine("Usage: aster explain <code>");
            Console.Error.WriteLine("Example: aster explain E0001");
            return 1;
        }

        var code = args[0];
        var explanation = DiagnosticExplainer.GetExplanation(code);

        if (explanation == null)
        {
            Console.Error.WriteLine($"error: unknown diagnostic code: {code}");
            return 1;
        }

        Console.WriteLine(DiagnosticExplainer.Format(explanation));
        return 0;
    }

    private static int CrashReport(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("error: no crash report file specified");
            return 1;
        }

        var reportPath = args[0];
        if (!File.Exists(reportPath))
        {
            Console.Error.WriteLine($"error: crash report not found: {reportPath}");
            return 1;
        }

        var report = File.ReadAllText(reportPath);
        Console.WriteLine(report);
        return 0;
    }
}
