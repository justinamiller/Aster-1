using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Aster.Compiler.Observability;

/// <summary>
/// Generates comprehensive crash reports when the compiler encounters an internal error.
/// </summary>
public sealed class CrashReporter
{
    private readonly string _version;
    private readonly string _lastPhase;
    private readonly List<string> _recentDiagnostics;
    private readonly string _command;

    public CrashReporter(string version, string command, string lastPhase, List<string> recentDiagnostics)
    {
        _version = version;
        _command = command;
        _lastPhase = lastPhase;
        _recentDiagnostics = recentDiagnostics;
    }

    /// <summary>Generate a crash report from an exception.</summary>
    public string GenerateReport(Exception exception)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("================================================================================");
        sb.AppendLine("ASTER COMPILER CRASH REPORT");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        // Header
        sb.AppendLine("We're sorry! The compiler crashed unexpectedly.");
        sb.AppendLine("This is a bug in the compiler, not your code.");
        sb.AppendLine("Please consider filing an issue with this crash report.");
        sb.AppendLine();

        // Version info
        sb.AppendLine("Version Information:");
        sb.AppendLine($"  Compiler Version: {_version}");
        sb.AppendLine($"  OS: {RuntimeInformation.OSDescription}");
        sb.AppendLine($"  Architecture: {RuntimeInformation.OSArchitecture}");
        sb.AppendLine($"  Runtime: .NET {Environment.Version}");
        sb.AppendLine();

        // Execution context
        sb.AppendLine("Execution Context:");
        sb.AppendLine($"  Command: {_command}");
        sb.AppendLine($"  Last Phase: {_lastPhase}");
        sb.AppendLine($"  Working Directory: {Environment.CurrentDirectory}");
        sb.AppendLine();

        // Exception details
        sb.AppendLine("Exception:");
        sb.AppendLine($"  Type: {exception.GetType().FullName}");
        sb.AppendLine($"  Message: {exception.Message}");
        sb.AppendLine();

        sb.AppendLine("Stack Trace:");
        sb.AppendLine(exception.StackTrace ?? "(no stack trace available)");
        sb.AppendLine();

        // Inner exceptions
        if (exception.InnerException != null)
        {
            sb.AppendLine("Inner Exception:");
            sb.AppendLine($"  Type: {exception.InnerException.GetType().FullName}");
            sb.AppendLine($"  Message: {exception.InnerException.Message}");
            sb.AppendLine($"  Stack: {exception.InnerException.StackTrace}");
            sb.AppendLine();
        }

        // Recent diagnostics
        if (_recentDiagnostics.Count > 0)
        {
            sb.AppendLine("Recent Diagnostics:");
            foreach (var diag in _recentDiagnostics.TakeLast(10))
            {
                sb.AppendLine($"  {diag}");
            }
            sb.AppendLine();
        }

        // Repro command
        sb.AppendLine("Reproduction Command:");
        sb.AppendLine($"  {_command}");
        sb.AppendLine();

        sb.AppendLine("================================================================================");
        sb.AppendLine($"Crash report timestamp: {DateTime.UtcNow:O}");
        sb.AppendLine("================================================================================");

        return sb.ToString();
    }

    /// <summary>Write crash report to a file.</summary>
    public string WriteCrashReport(Exception exception)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var filename = $"aster_crash_{timestamp}.txt";
        var path = Path.Combine(Environment.CurrentDirectory, filename);

        var report = GenerateReport(exception);
        File.WriteAllText(path, report);

        return path;
    }

    /// <summary>Display friendly crash message to user.</summary>
    public static void DisplayCrashMessage(string reportPath)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("================================================================================");
        Console.Error.WriteLine("The compiler has encountered an internal error and crashed.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("This is a bug in the compiler itself, not in your code.");
        Console.Error.WriteLine();
        Console.Error.WriteLine($"A crash report has been written to:");
        Console.Error.WriteLine($"  {reportPath}");
        Console.Error.WriteLine();
        Console.Error.WriteLine("You can view the report with:");
        Console.Error.WriteLine($"  aster crash-report {reportPath}");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Please consider filing an issue at:");
        Console.Error.WriteLine("  https://github.com/justinamiller/Aster-1/issues");
        Console.Error.WriteLine("================================================================================");
    }

    /// <summary>Install global exception handler for crash reporting.</summary>
    public static void InstallGlobalHandler(string version, Func<string> getCommand, Func<string> getLastPhase, Func<List<string>> getRecentDiagnostics)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                var reporter = new CrashReporter(
                    version,
                    getCommand(),
                    getLastPhase(),
                    getRecentDiagnostics());

                var path = reporter.WriteCrashReport(ex);
                DisplayCrashMessage(path);
            }

            Environment.Exit(1);
        };
    }
}
