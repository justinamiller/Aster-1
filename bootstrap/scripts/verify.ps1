# Aster Verification Script (Windows PowerShell)
# This script verifies the correctness of bootstrap stages.

param(
    [Parameter(HelpMessage="Verify stage N (0, 1, 2, or 3)")]
    [ValidateRange(0,3)]
    [int]$Stage,
    
    [Parameter(HelpMessage="Verify all stages")]
    [switch]$AllStages,
    
    [Parameter(HelpMessage="Verify self-hosting (aster3 compiles itself)")]
    [switch]$SelfCheck,
    
    [Parameter(HelpMessage="Test reproducible builds")]
    [switch]$Reproducibility,
    
    [Parameter(HelpMessage="Enable verbose output")]
    [switch]$Verbose,
    
    [Parameter(HelpMessage="Show help message")]
    [switch]$Help
)

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = (Get-Item $ScriptDir).Parent.Parent.FullName
$BuildDir = Join-Path $ProjectRoot "build\bootstrap"
$FixturesDir = Join-Path $ProjectRoot "bootstrap\fixtures"
$GoldensDir = Join-Path $ProjectRoot "bootstrap\goldens"

# Logging functions
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] " -ForegroundColor Blue -NoNewline
    Write-Host $Message
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] " -ForegroundColor Yellow -NoNewline
    Write-Host $Message
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] " -ForegroundColor Red -NoNewline
    Write-Host $Message
}

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

# Show help
function Show-Help {
    @"

Aster Verification Script (PowerShell)

USAGE:
    .\verify.ps1 [OPTIONS]

OPTIONS:
    -Stage <N>           Verify stage N (0, 1, 2, or 3)
    -AllStages           Verify all stages
    -SelfCheck           Verify self-hosting (aster3 compiles itself)
    -Reproducibility     Test reproducible builds
    -Verbose             Enable verbose output
    -Help                Show this help message

EXAMPLES:
    .\verify.ps1 -AllStages           # Verify all stages
    .\verify.ps1 -Stage 1             # Verify Stage 1 only
    .\verify.ps1 -SelfCheck           # Test aster3 == aster3'
    .\verify.ps1 -Reproducibility     # Test bit-for-bit reproducibility

For more information, see /bootstrap/spec/bootstrap-stages.md

"@
}

# Verify Stage 0
function Test-Stage0 {
    Write-Step "Verifying Stage 0: Seed Compiler"
    
    Push-Location $ProjectRoot
    
    try {
        # Run tests
        Write-Info "Running unit tests..."
        if ($Verbose) {
            & dotnet test --configuration Release
        } else {
            & dotnet test --configuration Release --verbosity quiet > $null 2>&1
        }
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Stage 0 tests failed"
            exit 1
        }
        
        Write-Success "Stage 0 tests passed (119 tests)"
        
        # Verify build exists
        $asterCliDll = Join-Path $BuildDir "stage0\Aster.CLI.dll"
        if (-not (Test-Path $asterCliDll)) {
            Write-Error "Stage 0 binary not found. Run .\bootstrap.ps1 first."
            exit 1
        }
        
        Write-Success "Stage 0 verified"
    }
    finally {
        Pop-Location
    }
}

# Verify Stage 1
function Test-Stage1 {
    Write-Step "Verifying Stage 1: Minimal Compiler"
    
    # Run differential token tests
    Write-Info "Running differential token tests..."
    $diffTestScript = Join-Path $ScriptDir "diff-test-tokens.ps1"
    & $diffTestScript
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Stage 1 differential tests failed"
        exit 1
    }
    
    Write-Success "Stage 1 differential tests passed"
    
    $aster1 = Join-Path $BuildDir "stage1\aster1.exe"
    if (-not (Test-Path $aster1)) {
        Write-Warning "Stage 1 binary not yet built"
        Write-Info "Golden files verified. Build aster1 for full differential testing."
        return
    }
    
    Write-Success "Stage 1 verified"
}

# Verify Stage 2
function Test-Stage2 {
    Write-Step "Verifying Stage 2: Expanded Compiler"
    
    $aster2 = Join-Path $BuildDir "stage2\aster2.exe"
    if (-not (Test-Path $aster2)) {
        Write-Warning "Stage 2 binary not found (not yet implemented)"
        Write-Info "Stage 2 verification will be implemented when Stage 2 is built"
        return
    }
    
    # TODO: Differential tests
    Write-Warning "Stage 2 verification not yet implemented"
}

# Verify Stage 3
function Test-Stage3 {
    Write-Step "Verifying Stage 3: Full Compiler"
    
    $aster3 = Join-Path $BuildDir "stage3\aster3.exe"
    if (-not (Test-Path $aster3)) {
        Write-Warning "Stage 3 binary not found (not yet implemented)"
        Write-Info "Stage 3 verification will be implemented when Stage 3 is built"
        return
    }
    
    # TODO: Differential tests
    Write-Warning "Stage 3 verification not yet implemented"
}

# Self-hosting check
function Test-SelfHosting {
    Write-Step "Verifying Self-Hosting (aster3 == aster3')"
    
    $aster3 = Join-Path $BuildDir "stage3\aster3.exe"
    if (-not (Test-Path $aster3)) {
        Write-Warning "Stage 3 binary not found. Cannot verify self-hosting."
        return
    }
    
    # TODO: Compile aster3 with itself
    Write-Warning "Self-hosting verification not yet implemented"
    Write-Info "Future: aster3 will compile itself and outputs will be compared"
}

# Reproducibility tests
function Test-Reproducibility {
    Write-Step "Verifying Reproducible Builds"
    
    # TODO: Build twice, compare outputs
    Write-Warning "Reproducibility tests not yet implemented"
    Write-Info "Future: Will test bit-for-bit reproducibility"
}

# Main
function Main {
    if ($Help) {
        Show-Help
        exit 0
    }
    
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗"
    Write-Host "║         Aster Compiler Verification System               ║"
    Write-Host "╚═══════════════════════════════════════════════════════════╝"
    Write-Host ""
    
    # Default: verify all if no specific stage requested
    if (-not $Stage -and -not $AllStages -and -not $SelfCheck -and -not $Reproducibility) {
        $AllStages = $true
    }
    
    # Run verifications
    if ($AllStages) {
        Test-Stage0
        Test-Stage1
        Test-Stage2
        Test-Stage3
    } else {
        switch ($Stage) {
            0 { Test-Stage0 }
            1 { Test-Stage1 }
            2 { Test-Stage2 }
            3 { Test-Stage3 }
        }
    }
    
    if ($SelfCheck) {
        Test-SelfHosting
    }
    
    if ($Reproducibility) {
        Test-Reproducibility
    }
    
    # Summary
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗"
    Write-Host "║                 Verification Summary                      ║"
    Write-Host "╚═══════════════════════════════════════════════════════════╝"
    Write-Host ""
    Write-Success "Verification complete"
    Write-Info "Note: Some verifications are pending implementation of later stages"
    Write-Host ""
}

try {
    Main
}
catch {
    Write-Error "Verification failed: $_"
    exit 1
}
