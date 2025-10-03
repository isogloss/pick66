# Build Process Architecture

## Overview

This document provides visual diagrams of the Pick6 build process and runtime architecture.

## Build Process Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                     GitHub Actions Workflow                          │
│                     (.github/workflows/build.yml)                    │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────┐
        │           1. Setup Environment                │
        │  - .NET 8 SDK                                │
        │  - CMake                                     │
        │  - Visual Studio 2022 (MSVC)                │
        └───────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────┐
        │     2. Build Pick6Native.dll (C++)           │
        │                                               │
        │  src/Pick6.Native/                           │
        │    ├── CMakeLists.txt                        │
        │    ├── build.bat (no pause)                  │
        │    └── C++ source files                      │
        │           │                                   │
        │           ▼                                   │
        │     build/Release/Pick6Native.dll            │
        └───────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────┐
        │  3. Create Pick6VulkanHook.dll (Placeholder) │
        │                                               │
        │  - Compile stub C++ code                     │
        │  - Creates minimal DLL with exports          │
        │  - For CI only (replace for production)      │
        │           │                                   │
        │           ▼                                   │
        │     Pick6VulkanHook.dll (stub)               │
        └───────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────┐
        │         4. Copy DLLs to Loader               │
        │                                               │
        │  copy Pick6Native.dll    → src/Pick6.Loader/ │
        │  copy Pick6VulkanHook.dll → src/Pick6.Loader/│
        └───────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────┐
        │     5. Build C# Project with Embedded DLLs   │
        │                                               │
        │  dotnet publish Pick6.Loader.csproj          │
        │    - PublishSingleFile=true                  │
        │    - EmbeddedResource for native DLLs        │
        │    - Costura.Fody for managed deps           │
        │           │                                   │
        │           ▼                                   │
        │     dist/pick6.exe (single file)             │
        └───────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────┐
        │           6. Create Release                   │
        │                                               │
        │  - Version from git tag                      │
        │  - Upload to GitHub Releases                 │
        │  - Artifact: Pick6-v{version}.exe            │
        └───────────────────────────────────────────────┘
```

## Runtime Architecture

### Application Startup

```
┌─────────────────────────────────────────────────────────────────────┐
│                         User runs pick6.exe                          │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────┐
        │         .NET Runtime Initializes              │
        │  - Extract managed assemblies to temp         │
        │  - Load Costura.Fody embedded dependencies   │
        │  - Start Pick6.Loader application            │
        └───────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────┐
        │      Application Needs Native DLL             │
        │  (for injection into FiveM)                   │
        └───────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────┐
        │         PathResolver.FindDll()                │
        │                                               │
        │  1. Check executable directory                │
        │     └─> Not found (embedded)                 │
        │                                               │
        │  2. Call ResourceExtractor.TryExtractDll()   │
        │     ├─> Find embedded resource               │
        │     ├─> Extract to %TEMP%/Pick6/Native/      │
        │     └─> Return extracted path                │
        │                                               │
        │  3. Return DLL path for use                  │
        └───────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────┐
        │         Injection Process                     │
        │                                               │
        │  Use extracted DLL for injection:             │
        │    - LoadLibrary into target process         │
        │    - Hook Vulkan/DX APIs                     │
        │    - Capture frames                          │
        └───────────────────────────────────────────────┘
```

## File Structure

### Before Build (Source)

```
pick66/
├── .github/
│   └── workflows/
│       └── build.yml                    ← New automated workflow
├── src/
│   ├── Pick6.Native/
│   │   ├── CMakeLists.txt
│   │   ├── build.bat                   ← No pause commands
│   │   └── *.cpp, *.h
│   ├── Pick6.Core/
│   │   ├── ResourceExtractor.cs        ← New: DLL extraction
│   │   ├── PathResolver.cs             ← Updated: Check extracted DLLs
│   │   └── ...
│   └── Pick6.Loader/
│       ├── Pick6.Loader.csproj         ← Updated: EmbeddedResource
│       ├── Pick6Native.dll             ← Copied before build
│       ├── Pick6VulkanHook.dll         ← Copied before build
│       └── ...
├── build.bat                            ← No pause commands
└── ...
```

### After Build (Distribution)

```
dist/
└── pick6.exe                            ← Single file!
    ├── [Embedded: .NET Runtime]
    ├── [Embedded: Pick6.Core.dll]       (via Costura)
    ├── [Embedded: Pick6.Projection.dll] (via Costura)
    ├── [Embedded: Pick6Native.dll]      (as resource)
    └── [Embedded: Pick6VulkanHook.dll]  (as resource)
```

### At Runtime (Extracted)

```
User's System:
├── C:\path\to\pick6.exe                 ← Single file downloaded
└── %TEMP%\Pick6\Native\                 ← Created at runtime
    ├── Pick6Native.dll                  ← Extracted from resources
    └── Pick6VulkanHook.dll              ← Extracted from resources
```

## Comparison: Old vs New

### Old Architecture (ExcludeFromSingleFile)

```
Distribution:
  pick6/
  ├── loader.exe           ← Main executable
  ├── Pick6VulkanHook.dll  ← Separate file
  ├── Pick6Native.dll      ← Separate file
  └── ...

Issues:
  ❌ Multiple files to distribute
  ❌ DLLs can be separated from exe
  ❌ More complex deployment
```

### New Architecture (EmbeddedResource)

```
Distribution:
  pick6/
  └── pick6.exe            ← Single file only!

Benefits:
  ✅ Single file distribution
  ✅ DLLs always available (embedded)
  ✅ Simpler deployment
  ✅ Maintains injection functionality
```

## Data Flow

```
┌──────────────┐
│  pick6.exe   │  Single file
│              │
│ ┌──────────┐ │
│ │ .NET     │ │  Embedded runtime
│ │ Runtime  │ │
│ └──────────┘ │
│              │
│ ┌──────────┐ │
│ │ Managed  │ │  Via Costura.Fody
│ │ DLLs     │ │  (Pick6.Core, etc)
│ └──────────┘ │
│              │
│ ┌──────────┐ │
│ │ Native   │ │  As EmbeddedResource
│ │ DLLs     │ │  (Pick6Native.dll, etc)
│ └──────────┘ │
└──────────────┘
       │
       │ Runtime
       ▼
┌──────────────┐
│ Temp Dir     │  %TEMP%/Pick6/Native/
│              │
│ ┌──────────┐ │
│ │ Extracted│ │  Physical files for
│ │ Native   │ │  injection into other
│ │ DLLs     │ │  processes
│ └──────────┘ │
└──────────────┘
       │
       │ Injection
       ▼
┌──────────────┐
│   FiveM      │  Target process
│   Process    │
│              │
│ ┌──────────┐ │
│ │ Injected │ │  LoadLibrary()
│ │ DLL      │ │
│ └──────────┘ │
└──────────────┘
```

## Key Components

### ResourceExtractor.cs
```
Purpose: Extract embedded DLLs to temp directory
Location: src/Pick6.Core/ResourceExtractor.cs
Key Methods:
  - ExtractDll(resourceName)
  - TryExtractDll(dllFileName)
  - GetExtractedDllsDirectory()
```

### PathResolver.cs
```
Purpose: Find DLLs in multiple locations
Location: src/Pick6.Core/PathResolver.cs
Search Order:
  1. Executable directory
  2. Extracted temp directory (NEW)
  3. Additional search paths
  4. Base directory fallback
```

### Pick6.Loader.csproj
```
Purpose: Build configuration
Key Settings:
  - AssemblyName: pick6 (was "loader")
  - PublishSingleFile: true
  - EmbeddedResource: Native DLLs (was ExcludeFromSingleFile)
  - Costura.Fody: Managed dependencies
```

## Build Triggers

The GitHub Actions workflow is triggered by:

1. **Push to main/develop**: Automatic build
2. **Pull Request**: Automatic build for testing
3. **Git Tags (v*)**: Build + GitHub Release
4. **Manual Dispatch**: On-demand builds

## Versioning

```
Version Source Priority:
1. Git tag (e.g., v1.2.3)
2. Workflow manual input
3. Auto-generated (dev-YYYYMMDD-HHMMSS)

Output Filename:
- pick6.exe (standard name)
- Pick6-v{version}.exe (versioned copy)
```

## Success Criteria

✅ Single file output (pick6.exe)  
✅ No separate DLL files  
✅ Native DLLs embedded as resources  
✅ Automatic extraction at runtime  
✅ Injection works correctly  
✅ Automated CI/CD pipeline  
✅ GitHub Releases integration  

## Troubleshooting Flow

```
User reports: "DLL not found"
        │
        ▼
Check: Does pick6.exe exist?
        ├─ No  → Download from Releases
        └─ Yes → Continue
                │
                ▼
Check: Run as Administrator?
        ├─ No  → Run as Admin
        └─ Yes → Continue
                │
                ▼
Check: Temp directory writable?
        ├─ No  → Check permissions
        └─ Yes → Continue
                │
                ▼
Check: Antivirus blocking?
        ├─ Yes → Add exception
        └─ No  → Contact support
```
