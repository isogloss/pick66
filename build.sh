#!/bin/bash

echo "================================================"
echo "           Building pick6 (Windows-only)       "
echo "================================================"

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean

# Restore packages
echo "Restoring packages..."
dotnet restore

# Build solution (Windows components)
echo "Building Windows solution..."
dotnet build --configuration Release

# Build GUI for Windows
echo "Creating Windows GUI executable..."
dotnet publish src/Pick6.GUI/Pick6.GUI.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output ./dist \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true

# Build console launcher for Windows
echo "Creating console launcher executable..."
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
    echo "Executables created in ./dist/:"
    echo "- Pick6.GUI.exe       (Windows GUI - recommended)"
    echo "- Pick6.Launcher.exe  (Console launcher)"
    echo ""
    echo "Usage:"
    echo "1. Copy executables to Windows machine"
    echo "2. Run Pick6.GUI.exe for GUI interface"
    echo "3. Or run Pick6.Launcher.exe for console interface"
    echo "4. Use Pick6.Launcher.exe --help for command line options"
    echo "================================================"
else
    echo "❌ Build failed!"
    exit 1
fi