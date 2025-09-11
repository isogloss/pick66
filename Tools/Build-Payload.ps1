param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputPath,
    
    [string]$Configuration = "Release",
    
    [string[]]$PayloadProjects = @(
        "src/Pick6.Core",
        "src/Pick6.Projection"
    ),
    
    [string]$ManifestTemplate = "docs/auto-update-manifest.sample.json"
)

Write-Host "Building Pick6 Payload v$Version" -ForegroundColor Green

# Validate parameters
if (-not (Test-Path $ManifestTemplate)) {
    Write-Error "Manifest template not found at: $ManifestTemplate"
    exit 1
}

# Create output directory
$OutputPath = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
if (-not $OutputPath) {
    Write-Host "Creating output directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    $OutputPath = Resolve-Path $OutputPath
}

$StagingPath = Join-Path $OutputPath "payload-staging"
$ZipPath = Join-Path $OutputPath "pick6-payload-$Version.zip"
$ManifestPath = Join-Path $OutputPath "manifest-$Version.json"

# Clean staging directory
Write-Host "Preparing staging directory..." -ForegroundColor Yellow
if (Test-Path $StagingPath) {
    Remove-Item $StagingPath -Recurse -Force
}
New-Item -ItemType Directory -Path $StagingPath -Force | Out-Null

# Build payload projects
Write-Host "Building payload projects..." -ForegroundColor Yellow
foreach ($project in $PayloadProjects) {
    Write-Host "  Building $project..." -ForegroundColor Cyan
    
    if (-not (Test-Path $project)) {
        Write-Warning "Project path not found: $project"
        continue
    }
    
    $result = dotnet build $project -c $Configuration --no-restore 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $project"
        Write-Host $result -ForegroundColor Red
        exit 1
    }
}

# Collect assemblies
Write-Host "Collecting payload assemblies..." -ForegroundColor Yellow
$collectedFiles = @()

foreach ($project in $PayloadProjects) {
    $projectName = Split-Path $project -Leaf
    $binPath = Join-Path $project "bin/$Configuration"
    
    # Find the target framework directory
    $frameworkDirs = Get-ChildItem $binPath -Directory | Where-Object { $_.Name -like "net*" }
    if ($frameworkDirs.Count -eq 0) {
        Write-Warning "No framework output found for $project"
        continue
    }
    
    $targetFramework = $frameworkDirs[0].Name
    $assemblyPath = Join-Path $binPath "$targetFramework"
    
    if (Test-Path $assemblyPath) {
        Write-Host "  Collecting from $assemblyPath..." -ForegroundColor Cyan
        
        # Copy DLL files
        $dlls = Get-ChildItem "$assemblyPath/*.dll" -ErrorAction SilentlyContinue
        foreach ($dll in $dlls) {
            $destPath = Join-Path $StagingPath $dll.Name
            Copy-Item $dll.FullName $destPath -Force
            $collectedFiles += $dll.Name
            Write-Host "    -> $($dll.Name)" -ForegroundColor Gray
        }
        
        # Copy PDB files if they exist (for debugging)
        $pdbs = Get-ChildItem "$assemblyPath/*.pdb" -ErrorAction SilentlyContinue
        foreach ($pdb in $pdbs) {
            $destPath = Join-Path $StagingPath $pdb.Name
            Copy-Item $pdb.FullName $destPath -Force
            Write-Host "    -> $($pdb.Name)" -ForegroundColor Gray
        }
    } else {
        Write-Warning "Assembly path not found: $assemblyPath"
    }
}

if ($collectedFiles.Count -eq 0) {
    Write-Error "No assemblies were collected. Check your project paths and build output."
    exit 1
}

Write-Host "Collected $($collectedFiles.Count) files" -ForegroundColor Green

# Create payload manifest inside the staging directory
Write-Host "Creating payload manifest..." -ForegroundColor Yellow
$manifestContent = Get-Content $ManifestTemplate | ConvertFrom-Json
$manifestContent.payloadVersion = $Version

# Assume the first collected DLL is the entry assembly (customize as needed)
$entryAssembly = $collectedFiles | Where-Object { $_ -like "*.dll" } | Select-Object -First 1
if ($entryAssembly) {
    $manifestContent.entryAssembly = $entryAssembly
    Write-Host "  Entry assembly set to: $entryAssembly" -ForegroundColor Cyan
}

# Save manifest inside payload
$payloadManifestPath = Join-Path $StagingPath "payload-manifest.json"
$manifestContent | ConvertTo-Json -Depth 10 | Out-File $payloadManifestPath -Encoding UTF8

# Create ZIP file
Write-Host "Creating payload ZIP..." -ForegroundColor Yellow
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($StagingPath, $ZipPath)

# Calculate SHA256
Write-Host "Calculating SHA256 hash..." -ForegroundColor Yellow
$hash = Get-FileHash $ZipPath -Algorithm SHA256
$sha256 = $hash.Hash.ToLower()

Write-Host "  SHA256: $sha256" -ForegroundColor Cyan

# Create deployment manifest
Write-Host "Creating deployment manifest..." -ForegroundColor Yellow
$manifestContent.sha256 = $sha256
# Note: payloadUrl should be updated manually for deployment
$manifestContent | ConvertTo-Json -Depth 10 | Out-File $ManifestPath -Encoding UTF8

# Clean up staging directory
Remove-Item $StagingPath -Recurse -Force

# Summary
Write-Host "`nBuild completed successfully!" -ForegroundColor Green
Write-Host "Payload ZIP: $ZipPath" -ForegroundColor White
Write-Host "Manifest: $ManifestPath" -ForegroundColor White
Write-Host "Version: $Version" -ForegroundColor White
Write-Host "SHA256: $sha256" -ForegroundColor White

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Upload the ZIP file to your web server" -ForegroundColor White
Write-Host "2. Update the payloadUrl in the manifest to point to the uploaded ZIP" -ForegroundColor White
Write-Host "3. Upload the updated manifest to your manifest URL" -ForegroundColor White