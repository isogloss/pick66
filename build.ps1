# Pick6 Complete Build Script (PowerShell)
# Builds native components and prepares loader for deployment

param(
    [switch]$SkipNative,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Pick6 Complete Build Script" -ForegroundColor Cyan
Write-Host "Builds native components and prepares loader for deployment" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Save starting directory
$StartDir = Get-Location

# Check prerequisites
function Test-Prerequisites {
    Write-Host "[1/3] Checking prerequisites..." -ForegroundColor Yellow
    
    # Check for .NET SDK
    try {
        $dotnetVersion = dotnet --version 2>&1
        Write-Host "  [OK] .NET SDK found: $dotnetVersion" -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR: .NET SDK not found. Please install .NET 8 SDK." -ForegroundColor Red
        Write-Host "Download from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
        exit 1
    }
    
    # Check for CMake (for native build)
    $cmakeAvailable = Get-Command cmake -ErrorAction SilentlyContinue
    if (-not $cmakeAvailable) {
        Write-Host "  WARNING: CMake not found. Native DLL build will be skipped." -ForegroundColor Yellow
        Write-Host "  If you need Pick6Native.dll, install CMake from: https://cmake.org/download/" -ForegroundColor Yellow
        return $false
    }
    else {
        $cmakeVersion = cmake --version | Select-Object -First 1
        Write-Host "  [OK] CMake found: $cmakeVersion" -ForegroundColor Green
        return $true
    }
}

# Build native DLL
function Build-NativeDll {
    param([bool]$CmakeAvailable)
    
    Write-Host ""
    Write-Host "[2/3] Building native components..." -ForegroundColor Yellow
    
    if (-not $CmakeAvailable -or $SkipNative) {
        Write-Host "  Skipping native DLL build" -ForegroundColor Gray
        Write-Host "  Note: Pick6Native.dll provides better injection compatibility" -ForegroundColor Gray
        return
    }
    
    # Navigate to native project
    $nativeDir = Join-Path $StartDir "src\Pick6.Native"
    if (-not (Test-Path $nativeDir)) {
        Write-Host "ERROR: Native project directory not found: $nativeDir" -ForegroundColor Red
        exit 1
    }
    
    Set-Location $nativeDir
    
    # Check if build.bat exists
    $buildScript = Join-Path $nativeDir "build.bat"
    if (-not (Test-Path $buildScript)) {
        Write-Host "ERROR: build.bat not found in src\Pick6.Native" -ForegroundColor Red
        Set-Location $StartDir
        exit 1
    }
    
    # Run the native build script
    Write-Host "  Building Pick6Native.dll..." -ForegroundColor Cyan
    & cmd /c $buildScript
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Native DLL build failed" -ForegroundColor Red
        Set-Location $StartDir
        exit 1
    }
    
    Set-Location $StartDir
    Write-Host "  [OK] Native DLL build completed" -ForegroundColor Green
}

# Copy DLLs to Loader project
function Copy-DllsToLoader {
    Write-Host ""
    Write-Host "[3/3] Copying DLLs to Loader project..." -ForegroundColor Yellow
    
    $loaderDir = Join-Path $StartDir "src\Pick6.Loader"
    $nativeBuildDir = Join-Path $StartDir "src\Pick6.Native\build\Release"
    
    # Create loader directory if it doesn't exist
    if (-not (Test-Path $loaderDir)) {
        Write-Host "ERROR: Loader directory not found: $loaderDir" -ForegroundColor Red
        exit 1
    }
    
    # Copy Pick6Native.dll if it exists
    $nativeDll = Join-Path $nativeBuildDir "Pick6Native.dll"
    if (Test-Path $nativeDll) {
        Write-Host "  Copying Pick6Native.dll..." -ForegroundColor Cyan
        try {
            Copy-Item $nativeDll $loaderDir -Force
            Write-Host "  [OK] Pick6Native.dll copied" -ForegroundColor Green
        }
        catch {
            Write-Host "  WARNING: Failed to copy Pick6Native.dll: $_" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "  [SKIP] Pick6Native.dll not found (native build may have failed)" -ForegroundColor Gray
    }
    
    # Check for Pick6VulkanHook.dll
    $vulkanHookDll = Join-Path $loaderDir "Pick6VulkanHook.dll"
    if (-not (Test-Path $vulkanHookDll)) {
        Write-Host ""
        Write-Host "  WARNING: Pick6VulkanHook.dll not found in Loader directory!" -ForegroundColor Yellow
        Write-Host "  This DLL is REQUIRED for the application to work." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  Please build or obtain Pick6VulkanHook.dll and place it in:" -ForegroundColor Yellow
        Write-Host "  $loaderDir" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  See: $loaderDir\Pick6VulkanHook.dll.placeholder.md" -ForegroundColor Gray
        Write-Host ""
    }
    else {
        Write-Host "  [OK] Pick6VulkanHook.dll present" -ForegroundColor Green
    }
}

# Show completion summary
function Show-Summary {
    $loaderDir = Join-Path $StartDir "src\Pick6.Loader"
    
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "Build Summary" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Check what DLLs are ready in the Loader directory
    $dllCount = 0
    $missingCount = 0
    
    Write-Host "Native DLLs in Loader project:" -ForegroundColor White
    
    # Required DLLs
    $vulkanHookDll = Join-Path $loaderDir "Pick6VulkanHook.dll"
    if (Test-Path $vulkanHookDll) {
        $dllCount++
        Write-Host "  [✓] Pick6VulkanHook.dll (REQUIRED)" -ForegroundColor Green
    }
    else {
        $missingCount++
        Write-Host "  [✗] Pick6VulkanHook.dll (REQUIRED - MISSING!)" -ForegroundColor Red
    }
    
    # Optional DLLs
    $nativeDll = Join-Path $loaderDir "Pick6Native.dll"
    if (Test-Path $nativeDll) {
        $dllCount++
        Write-Host "  [✓] Pick6Native.dll (optional)" -ForegroundColor Green
    }
    else {
        Write-Host "  [○] Pick6Native.dll (optional, not present)" -ForegroundColor Gray
    }
    
    # Check for proxy DLLs
    $proxyDlls = @("dxgi.dll", "d3d11.dll", "vulkan-1.dll")
    foreach ($proxyDll in $proxyDlls) {
        $proxyPath = Join-Path $loaderDir $proxyDll
        if (Test-Path $proxyPath) {
            $dllCount++
            Write-Host "  [✓] $proxyDll (proxy, optional)" -ForegroundColor Green
        }
    }
    
    Write-Host ""
    Write-Host "Found $dllCount DLL(s) ready for deployment" -ForegroundColor White
    
    if ($missingCount -gt 0) {
        Write-Host ""
        Write-Host "⚠️  WARNING: $missingCount required DLL(s) missing!" -ForegroundColor Yellow
        Write-Host "The application will NOT work without Pick6VulkanHook.dll" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor White
        Write-Host "  1. Build or obtain Pick6VulkanHook.dll" -ForegroundColor Gray
        Write-Host "  2. Copy it to: $loaderDir" -ForegroundColor Gray
        Write-Host "  3. Re-run this build script or build the Loader project" -ForegroundColor Gray
        Write-Host ""
    }
    else {
        Write-Host ""
        Write-Host "✅ All required DLLs are present!" -ForegroundColor Green
        Write-Host ""
        Write-Host "You can now build the loader:" -ForegroundColor White
        Write-Host "  dotnet publish src\Pick6.Loader\Pick6.Loader.csproj --configuration Release --runtime win-x64 --self-contained true" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Or use the installer script:" -ForegroundColor White
        Write-Host "  install.bat" -ForegroundColor Cyan
        Write-Host ""
    }
    
    Write-Host "For more information, see:" -ForegroundColor White
    Write-Host "  - DLL_DEPLOYMENT_GUIDE.md" -ForegroundColor Gray
    Write-Host "  - src\Pick6.Loader\README_DLL_REQUIREMENTS.md" -ForegroundColor Gray
    Write-Host ""
}

# Main execution
try {
    $cmakeAvailable = Test-Prerequisites
    Build-NativeDll -CmakeAvailable $cmakeAvailable
    Copy-DllsToLoader
    Show-Summary
    
    Write-Host "Build completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Build failed with error: $_" -ForegroundColor Red
    exit 1
}
finally {
    Set-Location $StartDir
}
