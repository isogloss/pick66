#!/bin/bash

echo "================================================"
echo "     Building Pick66 OBS Game Capture Clone    "
echo "================================================"

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean

# Restore packages
echo "Restoring packages..."
dotnet restore

# Build solution
echo "Building solution..."
dotnet build --configuration Release

# Build single-file executable for Windows
echo "Creating single-file Windows executable..."
dotnet publish src/Pick6.Launcher/Pick6.Launcher.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output ./dist \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true

if [ $? -eq 0 ]; then
    echo "================================================"
    echo "✅ Build completed successfully!"
    echo ""
    echo "Executable created: ./dist/Pick6.Launcher.exe"
    echo ""
    echo "To use:"
    echo "1. Copy Pick6.Launcher.exe to target Windows machine"
    echo "2. Run Pick6.Launcher.exe --help for options"
    echo "3. Or run Pick6.Launcher.exe for interactive mode"
    echo "================================================"
else
    echo "❌ Build failed!"
    exit 1
fi