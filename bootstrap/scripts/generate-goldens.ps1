# Generate golden token files from Aster fixtures using aster0 (seed compiler)
# Part of Stage 1 bootstrap differential testing

param(
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$FixturesDir = Join-Path $RepoRoot "bootstrap\fixtures\core0"
$GoldensDir = Join-Path $RepoRoot "bootstrap\goldens\core0"
$Aster0 = Join-Path $RepoRoot "build\bootstrap\stage0\Aster.CLI.exe"

if (-not $IsWindows) {
    $Aster0 = Join-Path $RepoRoot "build\bootstrap\stage0\Aster.CLI"
}

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  Generate Golden Files - Aster Stage 1 Bootstrap" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host

# Check if aster0 exists
if (-not (Test-Path $Aster0)) {
    Write-Host "Error: aster0 not found at $Aster0" -ForegroundColor Red
    Write-Host "Please run bootstrap.ps1 first to build the seed compiler."
    exit 1
}

# Create golden directories
$null = New-Item -ItemType Directory -Force -Path "$GoldensDir\compile-pass\tokens"
$null = New-Item -ItemType Directory -Force -Path "$GoldensDir\compile-fail\tokens"
$null = New-Item -ItemType Directory -Force -Path "$GoldensDir\run-pass\tokens"

Write-Host "Using seed compiler: " -NoNewline -ForegroundColor Yellow
Write-Host $Aster0
Write-Host "Fixtures directory: " -NoNewline -ForegroundColor Yellow
Write-Host $FixturesDir
Write-Host "Goldens directory: " -NoNewline -ForegroundColor Yellow
Write-Host $GoldensDir
Write-Host

# Function to generate golden for a fixture
function Generate-Golden {
    param(
        [string]$FixturePath,
        [string]$Category
    )
    
    $fixtureName = [System.IO.Path]::GetFileNameWithoutExtension($FixturePath)
    $goldenPath = Join-Path $GoldensDir "$Category\tokens\$fixtureName.json"
    
    Write-Host "  $fixtureName ... " -NoNewline
    
    try {
        $output = & $Aster0 emit-tokens $FixturePath 2>&1
        if ($LASTEXITCODE -eq 0) {
            $output | Out-File -FilePath $goldenPath -Encoding UTF8 -NoNewline
            Write-Host "✓" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗" -ForegroundColor Red
            # For compile-fail fixtures, this is expected
            if ($Category -eq "compile-fail") {
                Write-Host "    (Expected failure for compile-fail fixture)" -ForegroundColor Yellow
                '{"error": "Expected compilation failure"}' | Out-File -FilePath $goldenPath -Encoding UTF8
                return $true
            }
            return $false
        }
    } catch {
        Write-Host "✗" -ForegroundColor Red
        if ($Verbose) {
            Write-Host "    Error: $_" -ForegroundColor Red
        }
        return $false
    }
}

# Generate goldens for compile-pass fixtures
Write-Host "[1/3] Processing compile-pass fixtures..." -ForegroundColor Yellow
$successCount = 0
$totalCount = 0
Get-ChildItem -Path "$FixturesDir\compile-pass\*.ast" | ForEach-Object {
    $totalCount++
    if (Generate-Golden -FixturePath $_.FullName -Category "compile-pass") {
        $successCount++
    }
}
Write-Host "  Generated: $successCount/$totalCount"
Write-Host

# Generate goldens for compile-fail fixtures
Write-Host "[2/3] Processing compile-fail fixtures..." -ForegroundColor Yellow
Get-ChildItem -Path "$FixturesDir\compile-fail\*.ast" | ForEach-Object {
    $null = Generate-Golden -FixturePath $_.FullName -Category "compile-fail"
}
Write-Host

# Generate goldens for run-pass fixtures
Write-Host "[3/3] Processing run-pass fixtures..." -ForegroundColor Yellow
$runSuccess = 0
$runTotal = 0
Get-ChildItem -Path "$FixturesDir\run-pass\*.ast" | ForEach-Object {
    $runTotal++
    if (Generate-Golden -FixturePath $_.FullName -Category "run-pass") {
        $runSuccess++
    }
}
Write-Host "  Generated: $runSuccess/$runTotal"
Write-Host

# Summary
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "Golden files generated successfully!" -ForegroundColor Green
Write-Host
Write-Host "Next steps:"
Write-Host "  1. Review golden files in $GoldensDir"
Write-Host "  2. Build aster1 (Stage 1 Aster compiler)"
Write-Host "  3. Run verify.ps1 to test differential equivalence"
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
