using Aster.Compiler.Telemetry;
using Aster.Compiler.Observability;
using Xunit;

namespace Aster.Observability.Tests;

public class CompilationTelemetryTests
{
    [Fact]
    public void AddPhase_IncreasesPhaseCount()
    {
        var telemetry = new CompilationTelemetry();
        var phase = new PhaseMetrics("parsing");

        telemetry.AddPhase(phase);

        Assert.Single(telemetry.Phases);
        Assert.Equal("parsing", telemetry.Phases[0].PhaseName);
    }

    [Fact]
    public void GetOrCreatePhase_CreatesNewPhaseIfNotExists()
    {
        var telemetry = new CompilationTelemetry();
        
        var phase1 = telemetry.GetOrCreatePhase("parsing");
        var phase2 = telemetry.GetOrCreatePhase("parsing");

        Assert.Same(phase1, phase2);
        Assert.Single(telemetry.Phases);
    }

    [Fact]
    public void FormatHuman_ProducesReadableOutput()
    {
        var telemetry = new CompilationTelemetry();
        var phase = new PhaseMetrics("parsing")
        {
            WallTime = TimeSpan.FromMilliseconds(123.45),
            NodesProcessed = 100
        };
        telemetry.AddPhase(phase);

        var output = telemetry.FormatHuman();

        Assert.Contains("Compilation Metrics:", output);
        Assert.Contains("parsing", output);
        Assert.Contains("123.45", output);
        Assert.Contains("100", output);
    }

    [Fact]
    public void FormatJson_ProducesValidJson()
    {
        var telemetry = new CompilationTelemetry();
        var phase = new PhaseMetrics("type-checking")
        {
            WallTime = TimeSpan.FromMilliseconds(50),
            NodesProcessed = 75,
            CacheHits = 10,
            CacheMisses = 5
        };
        telemetry.AddPhase(phase);

        var json = telemetry.FormatJson();

        Assert.Contains("\"totalTimeMs\"", json);
        Assert.Contains("\"phases\"", json);
        Assert.Contains("\"type-checking\"", json);
        Assert.Contains("\"wallTimeMs\"", json);
        Assert.Contains("\"nodesProcessed\"", json);
        Assert.Contains("\"cacheHits\"", json);
    }

    [Fact]
    public void TotalTime_ReflectsElapsedTime()
    {
        var telemetry = new CompilationTelemetry();
        
        Thread.Sleep(50);
        
        Assert.True(telemetry.TotalTime.TotalMilliseconds >= 50);
    }
}

public class PhaseTimerTests
{
    [Fact]
    public void Dispose_RecordsWallTime()
    {
        var metrics = new PhaseMetrics("test");

        using (var timer = new PhaseTimer(metrics))
        {
            Thread.Sleep(10);
        }

        Assert.True(metrics.WallTime.TotalMilliseconds >= 10);
    }

    [Fact]
    public void IncrementNodes_UpdatesCount()
    {
        var metrics = new PhaseMetrics("test");

        using (var timer = new PhaseTimer(metrics))
        {
            timer.IncrementNodes(5);
            timer.IncrementNodes(3);
        }

        Assert.Equal(8, metrics.NodesProcessed);
    }

    [Fact]
    public void RecordCacheHit_IncrementsCounter()
    {
        var metrics = new PhaseMetrics("test");

        using (var timer = new PhaseTimer(metrics))
        {
            timer.RecordCacheHit();
            timer.RecordCacheHit();
        }

        Assert.Equal(2, metrics.CacheHits);
    }

    [Fact]
    public void RecordCacheMiss_IncrementsCounter()
    {
        var metrics = new PhaseMetrics("test");

        using (var timer = new PhaseTimer(metrics))
        {
            timer.RecordCacheMiss();
            timer.RecordCacheMiss();
            timer.RecordCacheMiss();
        }

        Assert.Equal(3, metrics.CacheMisses);
    }
}

public class CrashReporterTests
{
    [Fact]
    public void GenerateReport_IncludesBasicInfo()
    {
        var reporter = new CrashReporter(
            "0.2.0",
            "aster build test.ast",
            "type-checking",
            new List<string> { "error: type mismatch" });

        var exception = new InvalidOperationException("Test crash");
        var report = reporter.GenerateReport(exception);

        Assert.Contains("ASTER COMPILER CRASH REPORT", report);
        Assert.Contains("0.2.0", report);
        Assert.Contains("aster build test.ast", report);
        Assert.Contains("type-checking", report);
        Assert.Contains("InvalidOperationException", report);
        Assert.Contains("Test crash", report);
    }

    [Fact]
    public void GenerateReport_IncludesRecentDiagnostics()
    {
        var diagnostics = new List<string>
        {
            "error E3000: type mismatch",
            "warning W0001: unused variable"
        };

        var reporter = new CrashReporter("0.2.0", "test", "parsing", diagnostics);
        var exception = new Exception("crash");
        var report = reporter.GenerateReport(exception);

        Assert.Contains("Recent Diagnostics:", report);
        Assert.Contains("error E3000", report);
        Assert.Contains("warning W0001", report);
    }

    [Fact]
    public void WriteCrashReport_CreatesFile()
    {
        var tempDir = Path.GetTempPath();
        var originalDir = Environment.CurrentDirectory;
        
        try
        {
            Environment.CurrentDirectory = tempDir;

            var reporter = new CrashReporter("0.2.0", "test", "parsing", new List<string>());
            var exception = new Exception("test crash");
            
            var filePath = reporter.WriteCrashReport(exception);

            Assert.True(File.Exists(filePath));
            Assert.Contains("aster_crash_", filePath);

            // Clean up
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        finally
        {
            Environment.CurrentDirectory = originalDir;
        }
    }
}
