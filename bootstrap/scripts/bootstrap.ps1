# Aster Bootstrap Script (Windows PowerShell)
# This script builds the Aster compiler through all bootstrap stages.
#
# Usage:
#   .\bootstrap.ps1                    # Build all stages
#   .\bootstrap.ps1 -Stage 1           # Build only Stage 1
#   .\bootstrap.ps1 -Stage 2           # Build up to Stage 2
#   .\bootstrap.ps1 -FromSeed          # Rebuild from seed compiler
#   .\bootstrap.ps1 -Clean             # Clean all build artifacts
#   .\bootstrap.ps1 -Help              # Show help

param(
    [Parameter(HelpMessage="Build up to stage N (1, 2, or 3)")]
    [ValidateRange(1,3)]
    [int]$Stage = 3,
    
    [Parameter(HelpMessage="Rebuild from seed compiler")]
    [switch]$FromSeed,
    
    [Parameter(HelpMessage="Clean all build artifacts before building")]
    [switch]$Clean,
    
    [Parameter(HelpMessage="Enable verbose output")]
    [switch]$Verbose,
    
    [Parameter(HelpMessage="Disable reproducible build flags")]
    [switch]$NonReproducible,
    
    [Parameter(HelpMessage="Show help message")]
    [switch]$Help
)

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = (Get-Item $ScriptDir).Parent.Parent.FullName
$BootstrapDir = Join-Path $ProjectRoot "bootstrap"
$SrcDir = Join-Path $ProjectRoot "src"
$AsterDir = Join-Path $ProjectRoot "aster"
$BuildDir = Join-Path $ProjectRoot "build\bootstrap"
$SeedVersionFile = Join-Path $BootstrapDir "seed\aster-seed-version.txt"

# Toolchain versions
$LlvmVersionMajor = "19"
$DotnetVersion = "10"

# Reproducible builds
$Reproducible = -not $NonReproducible

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

Aster Bootstrap Script (PowerShell)

USAGE:
    .\bootstrap.ps1 [OPTIONS]

OPTIONS:
    -Stage <N>          Build up to stage N (1, 2, or 3). Default: 3
    -FromSeed           Rebuild from seed compiler (C# implementation)
    -Clean              Clean all build artifacts before building
    -Verbose            Enable verbose output
    -NonReproducible    Disable reproducible build flags (faster, not deterministic)
    -Help               Show this help message

EXAMPLES:
    .\bootstrap.ps1                      # Build all stages (0 → 1 → 2 → 3)
    .\bootstrap.ps1 -Stage 1             # Build only Stage 1
    .\bootstrap.ps1 -FromSeed            # Rebuild from C# seed compiler
    .\bootstrap.ps1 -Clean -Stage 3      # Clean build of all stages

STAGES:
    Stage 0: Seed compiler (C# implementation)
    Stage 1: Minimal Aster compiler (lexer, parser, basic IR)
    Stage 2: Expanded Aster compiler (types, traits, effects, ownership)
    Stage 3: Full Aster compiler (complete language, tooling)

For more information, see /bootstrap/spec/bootstrap-stages.md

"@
}

# Check prerequisites
function Test-Prerequisites {
    Write-Step "Checking prerequisites"
    
    # Check .NET
    $dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetCmd) {
        Write-Error ".NET SDK not found. Please install .NET $DotnetVersion or later."
        exit 1
    }
    
    $dotnetVersionOutput = & dotnet --version
    $dotnetVersionMajor = $dotnetVersionOutput.Split('.')[0]
    if ([int]$dotnetVersionMajor -lt [int]$DotnetVersion) {
        Write-Warning ".NET version $dotnetVersionOutput found, but $DotnetVersion is recommended"
    } else {
        Write-Info ".NET version: $dotnetVersionOutput"
    }
    
    # Check LLVM (optional for Stage 0)
    $llcCmd = Get-Command llc -ErrorAction SilentlyContinue
    if ($llcCmd) {
        $llcVersion = & llc --version | Select-String "LLVM version" | ForEach-Object { $_.ToString().Split()[2] }
        $llcVersionMajor = $llcVersion.Split('.')[0]
        if ($llcVersionMajor -eq $LlvmVersionMajor) {
            Write-Info "LLVM version: $llcVersion"
        } else {
            Write-Warning "LLVM version $llcVersion found, but $LlvmVersionMajor.x is recommended"
        }
    } else {
        Write-Warning "LLVM (llc) not found. Required for code generation in later stages."
    }
    
    # Check Clang (optional)
    $clangCmd = Get-Command clang -ErrorAction SilentlyContinue
    if ($clangCmd) {
        $clangVersion = & clang --version | Select-Object -First 1
        Write-Info "Clang found: $clangVersion"
    } else {
        Write-Info "Clang not found (optional)"
    }
    
    Write-Success "Prerequisites check complete"
}

# Clean build artifacts
function Clear-BuildArtifacts {
    Write-Step "Cleaning build artifacts"
    
    if (Test-Path $BuildDir) {
        Remove-Item -Path $BuildDir -Recurse -Force
        Write-Info "Removed $BuildDir"
    }
    
    # Clean .NET build artifacts
    $compilerBinDir = Join-Path $SrcDir "Aster.Compiler\bin"
    $compilerObjDir = Join-Path $SrcDir "Aster.Compiler\obj"
    if (Test-Path $compilerBinDir) {
        Remove-Item -Path $compilerBinDir -Recurse -Force
        Remove-Item -Path $compilerObjDir -Recurse -Force
        Write-Info "Removed .NET build artifacts"
    }
    
    Write-Success "Clean complete"
}

# Build Stage 0 (Seed Compiler)
function Build-Stage0 {
    Write-Step "Building Stage 0: Seed Compiler (C#)"
    
    Push-Location $ProjectRoot
    
    try {
        # Build the C# compiler
        Write-Info "Building C# compiler..."
        if ($Verbose) {
            & dotnet build Aster.slnx --configuration Release
        } else {
            & dotnet build Aster.slnx --configuration Release > $null 2>&1
        }
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Stage 0 build failed"
            exit 1
        }
        
        # Verify build
        $asterCliDll = Join-Path $SrcDir "Aster.CLI\bin\Release\net10.0\Aster.CLI.dll"
        if (-not (Test-Path $asterCliDll)) {
            Write-Error "Stage 0 build failed: Aster.CLI.dll not found"
            exit 1
        }
        
        Write-Success "Stage 0 built successfully"
        
        # Copy to build directory
        $stage0Dir = Join-Path $BuildDir "stage0"
        New-Item -ItemType Directory -Path $stage0Dir -Force > $null
        $releaseDir = Join-Path $SrcDir "Aster.CLI\bin\Release\net10.0"
        Copy-Item -Path "$releaseDir\*" -Destination $stage0Dir -Recurse -Force
        
        Write-Info "Stage 0 binary: $stage0Dir\Aster.CLI.dll"
    }
    finally {
        Pop-Location
    }
}

# Build Stage 1
function Build-Stage1 {
    Write-Step "Building Stage 1: Minimal Aster Compiler"
    
    $asterCompilerDir = Join-Path $AsterDir "compiler"
    if (-not (Test-Path $asterCompilerDir)) {
        Write-Warning "Aster compiler source not found at $asterCompilerDir"
        Write-Warning "Stage 1 implementation is not yet available."
        Write-Warning "This is expected if you're setting up the bootstrap infrastructure."
        Write-Info "To implement Stage 1:"
        Write-Info "  1. Port contracts, lexer, and parser to Aster (Core-0 subset)"
        Write-Info "  2. Place source in $AsterDir\compiler\stage1\"
        Write-Info "  3. Re-run bootstrap.ps1 -Stage 1"
        return
    }
    
    # TODO: Implement Stage 1 compilation
    Write-Warning "Stage 1 compilation not yet implemented"
    Write-Info "Future: aster0 will compile $AsterDir\compiler\stage1\*.ast → aster1"
}

# Build Stage 2
function Build-Stage2 {
    Write-Step "Building Stage 2: Expanded Aster Compiler"
    
    # TODO: Implement Stage 2 compilation
    Write-Warning "Stage 2 compilation not yet implemented"
    Write-Info "Future: aster1 will compile $AsterDir\compiler\stage2\*.ast → aster2"
}

# Build Stage 3
function Build-Stage3 {
    Write-Step "Building Stage 3: Full Aster Compiler"
    
    # TODO: Implement Stage 3 compilation
    Write-Warning "Stage 3 compilation not yet implemented"
    Write-Info "Future: aster2 will compile $AsterDir\compiler\stage3\*.ast → aster3"
}

# Main function
function Main {
    # Show help if requested
    if ($Help) {
        Show-Help
        exit 0
    }
    
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗"
    Write-Host "║         Aster Compiler Bootstrap Build System            ║"
    Write-Host "╚═══════════════════════════════════════════════════════════╝"
    Write-Host ""
    
    # Display configuration
    Write-Info "Target stage: $Stage"
    Write-Info "Build directory: $BuildDir"
    Write-Info "Reproducible builds: $(if ($Reproducible) { 'enabled' } else { 'disabled' })"
    
    Test-Prerequisites
    
    if ($Clean) {
        Clear-BuildArtifacts
    }
    
    # Create build directory
    New-Item -ItemType Directory -Path $BuildDir -Force > $null
    
    # Build stages
    Build-Stage0
    
    if ($Stage -ge 1) {
        Build-Stage1
    }
    
    if ($Stage -ge 2) {
        Build-Stage2
    }
    
    if ($Stage -ge 3) {
        Build-Stage3
    }
    
    # Summary
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗"
    Write-Host "║                    Build Summary                          ║"
    Write-Host "╚═══════════════════════════════════════════════════════════╝"
    Write-Host ""
    Write-Success "Stage 0 (Seed) ✓"
    if ($Stage -ge 1) {
        Write-Info "Stage 1 (Minimal) - Infrastructure ready, implementation pending"
    }
    if ($Stage -ge 2) {
        Write-Info "Stage 2 (Expanded) - Infrastructure ready, implementation pending"
    }
    if ($Stage -ge 3) {
        Write-Info "Stage 3 (Full) - Infrastructure ready, implementation pending"
    }
    Write-Host ""
    Write-Info "Build artifacts: $BuildDir"
    Write-Info "Next steps: Run .\bootstrap\scripts\verify.ps1 to verify the build"
    Write-Host ""
}

# Run main
try {
    Main
}
catch {
    Write-Error "Bootstrap failed: $_"
    exit 1
}
