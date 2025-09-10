#!/bin/bash

echo "================================================"
echo "     Building Pick6 OBS Game Capture Clone     "
echo "================================================"

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean

# Restore packages
echo "Restoring packages..."
dotnet restore

# Build solution (console components)
echo "Building console solution..."
dotnet build --configuration Release

# Build GUI for Windows (Windows only)
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
    echo "- Pick6.GUI.exe       (Windows Forms GUI - recommended)"
    echo "- Pick6.Launcher.exe  (Console mode + GUI launcher)"
    echo ""
    echo "To use:"
    echo "1. Copy executables to target Windows machine"
    echo "2. Run Pick6.GUI.exe for best experience (OBS-style interface)"
    echo "3. Or run Pick6.Launcher.exe for console mode"
    echo "4. Use Pick6.Launcher.exe --help for command line options"
    echo "================================================"
else
    echo "❌ Build failed!"
    exit 1
fi