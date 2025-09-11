#!/bin/bash

# Exit on any error
set -euo pipefail

echo "================================================"
echo "           Building pick6 (Windows-only)       "
echo "================================================"
echo

# Clean previous builds
echo "🧹 Cleaning previous builds..."
if ! dotnet clean; then
    echo "❌ Clean failed!"
    exit 1
fi

# Restore packages
echo "📦 Restoring packages..."
if ! dotnet restore; then
    echo "❌ Restore failed!"
    exit 1
fi

# Build solution (Windows components)
echo "🔨 Building Windows solution..."
if ! dotnet build --configuration Release; then
    echo "❌ Build failed!"
    exit 1
fi

# Build GUI for Windows
echo "🖥️ Creating Windows GUI executable..."
if ! dotnet publish src/Pick6.GUI/Pick6.GUI.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output ./dist \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true; then
    echo "❌ GUI publish failed!"
    exit 1
fi

# Build console launcher for Windows
echo "⌨️ Creating console launcher executable..."
if ! dotnet publish src/Pick6.Launcher/Pick6.Launcher.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output ./dist \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true; then
    echo "❌ Console launcher publish failed!"
    exit 1
fi

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