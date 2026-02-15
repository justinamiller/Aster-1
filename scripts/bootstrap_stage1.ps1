# Bootstrap Stage 1 Compiler (PowerShell)
# Compiles aster1 (written in Aster) using aster0 (C# compiler)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

Write-Host "======================================"
Write-Host "  Aster Stage 1 Bootstrap"
Write-Host "======================================"
Write-Host ""

# Step 1: Build aster0 (C# seed compiler)
Write-Host "[1/5] Building aster0 (C# seed compiler)..." -ForegroundColor Yellow
Set-Location $ProjectRoot
dotnet build --configuration Release > $null 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ aster0 built successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to build aster0" -ForegroundColor Red
    exit 1
}

# Step 2: Compile aster1 source with aster0
Write-Host "[2/5] Compiling aster1 source with aster0..." -ForegroundColor Yellow
$Aster0 = "dotnet run --project $ProjectRoot\src\Aster.CLI --no-build --"
$Aster1Src = "$ProjectRoot\src\aster1"
$BuildDir = "$ProjectRoot\build\bootstrap\stage1"

New-Item -ItemType Directory -Force -Path $BuildDir | Out-Null

# Note: This will fail initially because aster1 is not yet complete
Write-Host "  (aster1 source is not yet complete - this step will be enabled later)"
Write-Host "⚠ aster1 compilation skipped (implementation incomplete)" -ForegroundColor Yellow

# Step 3: Compile test program with aster0
Write-Host "[3/5] Compiling test program with aster0..." -ForegroundColor Yellow
$TestProgram = "$ProjectRoot\examples\simple_hello.ast"
if (Test-Path $TestProgram) {
    & $Aster0 build $TestProgram -o "$BuildDir\test0.ll" 2>&1 | Out-Null
    if (Test-Path "$BuildDir\test0.ll") {
        Write-Host "✓ Test program compiled with aster0" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to compile test program" -ForegroundColor Red
    }
} else {
    Write-Host "⚠ Test program not found: $TestProgram" -ForegroundColor Yellow
}

# Step 4: (Future) Compile test program with aster1
Write-Host "[4/5] Compiling test program with aster1..." -ForegroundColor Yellow
Write-Host "  (skipped - aster1 binary not available yet)"

# Step 5: (Future) Differential testing
Write-Host "[5/5] Running differential tests..." -ForegroundColor Yellow
Write-Host "  (skipped - aster1 binary not available yet)"

Write-Host ""
Write-Host "======================================"
Write-Host "Bootstrap process completed" -ForegroundColor Green
Write-Host "======================================"
Write-Host ""
Write-Host "Status:"
Write-Host "  ✓ aster0 (C# compiler) - Ready"
Write-Host "  ⚠ aster1 source - Skeleton created, implementation pending"
Write-Host "  ⚠ aster1 binary - Not yet available"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Implement missing components in src\aster1\"
Write-Host "  2. Test individual components"
Write-Host "  3. Run full bootstrap when complete"
Write-Host ""
Write-Host "Output directory: $BuildDir"
