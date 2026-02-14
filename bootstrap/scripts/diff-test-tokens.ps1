# Differential test harness for comparing aster0 (seed) vs aster1 token output
# Part of Stage 1 bootstrap validation

param(
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$FixturesDir = Join-Path $RepoRoot "bootstrap\fixtures\core0"
$GoldensDir = Join-Path $RepoRoot "bootstrap\goldens\core0"
$Aster0 = Join-Path $RepoRoot "build\bootstrap\stage0\Aster.CLI.exe"
$Aster1 = Join-Path $RepoRoot "build\bootstrap\stage1\aster1.exe"
$TempDir = Join-Path $env:TEMP "aster-diff-test-$(Get-Random)"

if (-not $IsWindows) {
    $Aster0 = Join-Path $RepoRoot "build\bootstrap\stage0\Aster.CLI"
    $Aster1 = Join-Path $RepoRoot "build\bootstrap\stage1\aster1"
}

# Create temp directory
$null = New-Item -ItemType Directory -Force -Path $TempDir

# Cleanup temp directory on exit
$null = Register-EngineEvent PowerShell.Exiting -Action {
    if (Test-Path $TempDir) {
        Remove-Item -Recurse -Force $TempDir
    }
}

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  Differential Testing - Aster Stage 1 Bootstrap" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host

# Check if aster0 exists
if (-not (Test-Path $Aster0)) {
    Write-Host "Error: aster0 not found at $Aster0" -ForegroundColor Red
    Write-Host "Please run bootstrap.ps1 first."
    exit 1
}

# Check if aster1 exists
$Aster1Ready = $false
if (-not (Test-Path $Aster1)) {
    Write-Host "Note: aster1 not yet built at $Aster1" -ForegroundColor Yellow
    Write-Host "This script will verify golden files exist." -ForegroundColor Yellow
    Write-Host "Once aster1 is built, run this script again for full differential testing." -ForegroundColor Yellow
    Write-Host
} else {
    $Aster1Ready = $true
}

# Function to compare two JSON token files
function Compare-Tokens {
    param(
        [string]$Golden,
        [string]$TestOutput,
        [string]$FixtureName
    )
    
    if (-not (Test-Path $Golden)) {
        Write-Host "  ✗ $FixtureName : golden file not found" -ForegroundColor Red
        return $false
    }
    
    if (-not (Test-Path $TestOutput)) {
        Write-Host "  ✗ $FixtureName : test output not generated" -ForegroundColor Red
        return $false
    }
    
    # Compare files (normalize whitespace)
    $goldenContent = (Get-Content $Golden -Raw).Trim()
    $testContent = (Get-Content $TestOutput -Raw).Trim()
    
    if ($goldenContent -eq $testContent) {
        Write-Host "  ✓ $FixtureName" -ForegroundColor Green
        return $true
    } else {
        Write-Host "  ✗ $FixtureName : outputs differ" -ForegroundColor Red
        if ($Verbose) {
            Write-Host "    Golden length: $($goldenContent.Length)" -ForegroundColor Yellow
            Write-Host "    Test length: $($testContent.Length)" -ForegroundColor Yellow
        }
        return $false
    }
}

# Function to test a fixture category
function Test-Category {
    param(
        [string]$Category
    )
    
    $fixturesPath = Join-Path $FixturesDir $Category
    $goldensPath = Join-Path $GoldensDir "$Category\tokens"
    
    Write-Host "Testing $Category fixtures..." -ForegroundColor Yellow
    
    $passed = 0
    $failed = 0
    $total = 0
    
    Get-ChildItem -Path "$fixturesPath\*.ast" | ForEach-Object {
        $total++
        $fixtureName = $_.BaseName
        $golden = Join-Path $goldensPath "$fixtureName.json"
        
        # Check if golden exists
        if (-not (Test-Path $golden)) {
            Write-Host "  ✗ $fixtureName : golden file missing" -ForegroundColor Red
            $failed++
            return
        }
        
        # If aster1 is ready, generate test output and compare
        if ($Aster1Ready) {
            $testOutput = Join-Path $TempDir "$fixtureName.json"
            
            try {
                $output = & $Aster1 emit-tokens $_.FullName 2>&1
                if ($LASTEXITCODE -eq 0) {
                    $output | Out-File -FilePath $testOutput -Encoding UTF8 -NoNewline
                    
                    if (Compare-Tokens -Golden $golden -TestOutput $testOutput -FixtureName $fixtureName) {
                        $passed++
                    } else {
                        $failed++
                    }
                } else {
                    Write-Host "  ✗ $fixtureName : aster1 failed to tokenize" -ForegroundColor Red
                    $failed++
                }
            } catch {
                Write-Host "  ✗ $fixtureName : exception: $_" -ForegroundColor Red
                $failed++
            }
        } else {
            # Just verify golden exists
            Write-Host "  ○ $fixtureName : golden exists" -ForegroundColor Blue
            $passed++
        }
    }
    
    Write-Host "  Result: $passed/$total passed"
    if ($failed -gt 0) {
        Write-Host "  $failed failed" -ForegroundColor Red
    }
    Write-Host
    
    return $failed
}

# Test all categories
$totalFailures = 0
$totalFailures += Test-Category -Category "compile-pass"
$totalFailures += Test-Category -Category "run-pass"

# Summary
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
if ($totalFailures -eq 0) {
    if ($Aster1Ready) {
        Write-Host "All differential tests passed!" -ForegroundColor Green
        Write-Host "aster0 and aster1 produce identical token streams." -ForegroundColor Green
    } else {
        Write-Host "All golden files verified!" -ForegroundColor Green
        Write-Host "Build aster1 to run full differential testing." -ForegroundColor Yellow
    }
    $exitCode = 0
} else {
    Write-Host "Differential tests failed: $totalFailures failures" -ForegroundColor Red
    Write-Host
    Write-Host "Debug steps:"
    Write-Host "  1. Check golden files in $GoldensDir"
    Write-Host "  2. Verify aster1 lexer implementation"
    Write-Host "  3. Run with -Verbose for detailed diffs"
    $exitCode = 1
}
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

# Cleanup
if (Test-Path $TempDir) {
    Remove-Item -Recurse -Force $TempDir
}

exit $exitCode
