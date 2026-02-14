using Aster.Compiler.Driver;

namespace Aster.CLI;

/// <summary>
/// Command-line interface for the Aster compiler.
/// Supports: build, run, check, emit-llvm
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
            "run" => Run(args.Skip(1).ToArray()),
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
        Console.WriteLine("  build       Compile source to LLVM IR");
        Console.WriteLine("  check       Type-check without compiling");
        Console.WriteLine("  emit-llvm   Emit LLVM IR to stdout");
        Console.WriteLine("  run         Compile and prepare for execution");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h     Show this help message");
        Console.WriteLine("  --version, -v  Show version");
        return 0;
    }

    private static int PrintVersion()
    {
        Console.WriteLine("aster 0.1.0");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"error: unknown command '{command}'");
        PrintUsage();
        return 1;
    }
}
