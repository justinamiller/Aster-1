# Aster Bootstrap Stage Check and Advance Script (PowerShell)
# This script verifies the current bootstrap stage and advances to the next stage if ready.
#
# Usage:
#   .\check-and-advance.ps1                    # Check current stage and advance
#   .\check-and-advance.ps1 -CheckOnly         # Only check current stage, don't build
#   .\check-and-advance.ps1 -ForceStage 0      # Force build stage N
#   .\check-and-advance.ps1 -Help              # Show help

param(
    [switch]$CheckOnly = $false,
    [int]$ForceStage = -1,
    [switch]$Verbose = $false,
    [switch]$Help = $false
)

$ErrorActionPreference = "Stop"

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = (Get-Item $ScriptDir).Parent.Parent.FullName
$BuildDir = Join-Path $ProjectRoot "build\bootstrap"
$AsterDir = Join-Path $ProjectRoot "aster"
$SrcDir = Join-Path $ProjectRoot "src"

# Functions
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Show-Help {
    Write-Host @"
Aster Bootstrap Stage Check and Advance Script

USAGE:
    .\check-and-advance.ps1 [OPTIONS]

OPTIONS:
    -CheckOnly        Only check current stage, don't build next
    -ForceStage N     Force build stage N (0, 1, 2, or 3)
    -Verbose          Enable verbose output
    -Help             Show this help message

DESCRIPTION:
    This script verifies the current bootstrap stage and automatically
    advances to the next stage if prerequisites are met.

BOOTSTRAP STAGES:
    Stage 0: Seed compiler (C# implementation) - Always buildable
    Stage 1: Minimal Aster compiler (lexer, parser, basic IR)
    Stage 2: Expanded Aster compiler (types, traits, effects, ownership)
    Stage 3: Full Aster compiler (complete language, tooling)

EXAMPLES:
    .\check-and-advance.ps1               # Check and advance to next stage
    .\check-and-advance.ps1 -CheckOnly    # Only check current stage
    .\check-and-advance.ps1 -ForceStage 0 # Force rebuild Stage 0

For more information, see /bootstrap/spec/bootstrap-stages.md
"@
}

function Test-StageBuilt {
    param([int]$Stage)
    
    switch ($Stage) {
        0 { Test-Path (Join-Path $BuildDir "stage0\Aster.CLI.dll") }
        1 { Test-Path (Join-Path $BuildDir "stage1\aster1.exe") -or (Test-Path (Join-Path $BuildDir "stage1\aster1")) }
        2 { Test-Path (Join-Path $BuildDir "stage2\aster2.exe") -or (Test-Path (Join-Path $BuildDir "stage2\aster2")) }
        3 { Test-Path (Join-Path $BuildDir "stage3\aster3.exe") -or (Test-Path (Join-Path $BuildDir "stage3\aster3")) }
        default { $false }
    }
}

function Test-StageSource {
    param([int]$Stage)
    
    switch ($Stage) {
        0 { Test-Path (Join-Path $SrcDir "Aster.Compiler") }
        1 { 
            $astFiles = Get-ChildItem -Path (Join-Path $AsterDir "compiler") -Filter "*.ast" -Recurse -ErrorAction SilentlyContinue
            $astFiles.Count -gt 0
        }
        2 { Test-Path (Join-Path $AsterDir "compiler\stage2") }
        3 { Test-Path (Join-Path $AsterDir "compiler\stage3") }
        default { $false }
    }
}

function Get-CurrentStage {
    for ($stage = 3; $stage -ge 0; $stage--) {
        if (Test-StageBuilt $stage) {
            return $stage
        }
    }
    return -1
}

function Show-CurrentStageDetails {
    param([int]$CurrentStage)
    
    Write-Step "Current Bootstrap Stage"
    
    if ($CurrentStage -eq -1) {
        Write-Info "No stages built yet"
        Write-Info "Status: Bootstrap not started"
    } else {
        Write-Success "Current stage: Stage $CurrentStage"
        
        switch ($CurrentStage) {
            0 {
                Write-Info "Stage 0: Seed Compiler (C#) - ✓ Built"
                Write-Info "Location: $(Join-Path $BuildDir 'stage0\Aster.CLI.dll')"
            }
            1 {
                Write-Info "Stage 1: Minimal Aster Compiler - ✓ Built"
                Write-Info "Location: $(Join-Path $BuildDir 'stage1\aster1')"
            }
            2 {
                Write-Info "Stage 2: Expanded Aster Compiler - ✓ Built"
                Write-Info "Location: $(Join-Path $BuildDir 'stage2\aster2')"
            }
            3 {
                Write-Info "Stage 3: Full Aster Compiler - ✓ Built"
                Write-Info "Location: $(Join-Path $BuildDir 'stage3\aster3')"
            }
        }
    }
}

function Get-NextStage {
    param([int]$CurrentStage)
    
    if ($CurrentStage -eq -1) {
        return 0
    }
    
    $nextStage = $CurrentStage + 1
    
    if ($nextStage -gt 3) {
        return "complete"
    }
    
    if (Test-StageSource $nextStage) {
        return $nextStage
    } else {
        return "waiting"
    }
}

function Show-NextStageDetails {
    param($NextStage)
    
    Write-Step "Next Stage to Build"
    
    if ($NextStage -eq "complete") {
        Write-Success "All stages complete! Bootstrap finished."
    } elseif ($NextStage -eq "waiting") {
        Write-Warning "Next stage source not available"
        Write-Info "Status: Waiting for stage implementation"
    } else {
        Write-Info "Next stage: Stage $NextStage"
    }
}

function Show-Status {
    param([int]$CurrentStage, $NextStage)
    
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗"
    Write-Host "║            Bootstrap Stage Status Report                 ║"
    Write-Host "╚═══════════════════════════════════════════════════════════╝"
    Write-Host ""
    
    Write-Host ("{0,-15} {1,-15} {2,-30}" -f "Stage", "Status", "Location")
    Write-Host ("{0,-15} {1,-15} {2,-30}" -f "-----", "------", "--------")
    
    for ($stage = 0; $stage -le 3; $stage++) {
        $status = "Not Built"
        $location = "-"
        
        if (Test-StageBuilt $stage) {
            $status = "✓ Built"
            switch ($stage) {
                0 { $location = Join-Path $BuildDir "stage0\Aster.CLI.dll" }
                1 { $location = Join-Path $BuildDir "stage1\aster1" }
                2 { $location = Join-Path $BuildDir "stage2\aster2" }
                3 { $location = Join-Path $BuildDir "stage3\aster3" }
            }
        } elseif (Test-StageSource $stage) {
            $status = "Source Ready"
        } else {
            $status = "Pending"
        }
        
        Write-Host ("{0,-15} {1,-15} {2,-30}" -f "Stage $stage", $status, $location)
    }
    
    Write-Host ""
    
    if ($CurrentStage -ne -1) {
        Write-Host "Current Stage: Stage $CurrentStage" -ForegroundColor Green
    } else {
        Write-Host "Current Stage: None (bootstrap not started)" -ForegroundColor Yellow
    }
    
    if ($NextStage -eq "complete") {
        Write-Host "Next Stage: Bootstrap Complete! 🎉" -ForegroundColor Green
    } elseif ($NextStage -eq "waiting") {
        Write-Host "Next Stage: Waiting for implementation" -ForegroundColor Yellow
    } else {
        Write-Host "Next Stage: Stage $NextStage" -ForegroundColor Cyan
    }
    
    Write-Host ""
}

function Build-Stage {
    param([int]$Stage)
    
    Write-Step "Building Stage $Stage"
    
    if ($Stage -eq 0) {
        # Build C# compiler
        Push-Location $ProjectRoot
        try {
            Write-Info "Building C# seed compiler..."
            if ($Verbose) {
                dotnet build Aster.slnx --configuration Release
            } else {
                dotnet build Aster.slnx --configuration Release | Out-Null
            }
            
            $dllPath = Join-Path $SrcDir "Aster.CLI\bin\Release\net10.0\Aster.CLI.dll"
            if (-not (Test-Path $dllPath)) {
                Write-Error "Stage 0 build failed: Aster.CLI.dll not found"
                return $false
            }
            
            # Copy to build directory
            $stage0Dir = Join-Path $BuildDir "stage0"
            New-Item -ItemType Directory -Force -Path $stage0Dir | Out-Null
            Copy-Item -Path (Join-Path $SrcDir "Aster.CLI\bin\Release\net10.0\*") -Destination $stage0Dir -Recurse -Force
            
            Write-Success "Stage 0 built successfully"
            return $true
        } finally {
            Pop-Location
        }
    } else {
        # For stages 1-3, use bootstrap script
        $bootstrapScript = Join-Path $ScriptDir "bootstrap.ps1"
        if ($Verbose) {
            & $bootstrapScript -Stage $Stage -Verbose
        } else {
            & $bootstrapScript -Stage $Stage
        }
        
        return Test-StageBuilt $Stage
    }
}

# Main script
function Main {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗"
    Write-Host "║     Aster Bootstrap Stage Check and Advance Tool         ║"
    Write-Host "╚═══════════════════════════════════════════════════════════╝"
    Write-Host ""
    
    if ($Help) {
        Show-Help
        exit 0
    }
    
    # Validate force stage
    if ($ForceStage -ne -1 -and ($ForceStage -lt 0 -or $ForceStage -gt 3)) {
        Write-Error "Invalid stage: $ForceStage (must be 0, 1, 2, or 3)"
        exit 1
    }
    
    # If force stage is specified, build it directly
    if ($ForceStage -ne -1) {
        Write-Info "Force building Stage $ForceStage"
        if (Build-Stage $ForceStage) {
            exit 0
        } else {
            exit 1
        }
    }
    
    # Determine current and next stage
    $currentStage = Get-CurrentStage
    Show-CurrentStageDetails $currentStage
    
    $nextStage = Get-NextStage $currentStage
    Show-NextStageDetails $nextStage
    
    Show-Status $currentStage $nextStage
    
    # If check-only, stop here
    if ($CheckOnly) {
        Write-Info "Check-only mode: exiting without building"
        exit 0
    }
    
    # Build next stage if available
    if ($nextStage -match '^\d+$') {
        Write-Host ""
        Write-Info "Proceeding to build Stage $nextStage"
        
        if (Build-Stage $nextStage) {
            Write-Success "Successfully advanced to Stage $nextStage"
            
            # Show updated status
            $currentStage = $nextStage
            $nextStage = Get-NextStage $currentStage
            Write-Host ""
            Show-Status $currentStage $nextStage
        } else {
            Write-Error "Failed to build Stage $nextStage"
            exit 1
        }
    } elseif ($nextStage -eq "complete") {
        Write-Success "Bootstrap is complete! All stages built."
        Write-Info "You can now run self-hosting verification:"
        Write-Info "  .\bootstrap\scripts\verify.ps1 -SelfCheck"
    } elseif ($nextStage -eq "waiting") {
        Write-Warning "Cannot advance: Next stage source not available"
        Write-Info "Current status: Waiting for stage implementation"
        Write-Info "See /bootstrap/spec/bootstrap-stages.md for details"
    }
    
    Write-Host ""
}

# Run main
Main
