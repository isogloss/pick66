#!/bin/bash

# Pick6 Release Build Script
# Builds a single-file, self-contained Windows executable

echo "================================================"
echo "    Pick6 - Single-File Release Builder"
echo "================================================"
echo

echo "🔧 Building Release..."
dotnet build -c Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

echo
echo "📦 Publishing single-file executable..."
dotnet publish src/Pick6.Loader -c Release -r win-x64 --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishReadyToRun=true \
    -p:DebugType=none

if [ $? -ne 0 ]; then
    echo "❌ Publish failed!"
    exit 1
fi

EXE_PATH="src/Pick6.Loader/bin/Release/net8.0/win-x64/publish/pick6_loader.exe"

if [ -f "$EXE_PATH" ]; then
    SIZE=$(du -h "$EXE_PATH" | cut -f1)
    echo
    echo "✅ SUCCESS!"
    echo "📄 Single-file executable created:"
    echo "   Path: $EXE_PATH"
    echo "   Size: $SIZE"
    echo
    echo "🚀 Ready for distribution!"
    echo "   • No dependencies required"
    echo "   • Runs on Windows 10/11"
    echo "   • Self-contained .NET runtime"
    echo
    echo "💡 Usage:"
    echo "   pick6_loader.exe              # GUI mode (default)"
    echo "   pick6_loader.exe --console    # Console mode"
    echo "   pick6_loader.exe --help       # Show all options"
else
    echo "❌ Executable not found at expected location!"
    exit 1
fi