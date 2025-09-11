# Pick6 Auto-Update System

This document explains how to build, package, and deploy payload updates for the Pick6 loader's auto-update system.

## Overview

The Pick6 loader can dynamically download and load payload assemblies from a remote server. This allows for updates without redistributing the entire loader executable.

## Payload Structure

A payload consists of:
- One or more .NET assemblies (.dll files)
- A static entry method that serves as the application entry point
- A JSON manifest describing the payload

## Building a Payload

### Method 1: Using the Build Script (Recommended)

Use the provided PowerShell script to automate payload creation:

```powershell
.\Tools\Build-Payload.ps1 -Version "1.0.0" -OutputPath ".\dist"
```

This script will:
1. Build all payload projects in Release mode
2. Collect assemblies into a staging directory
3. Create a ZIP file with version suffix
4. Calculate SHA256 hash for integrity verification
5. Generate a manifest JSON file

### Method 2: Manual Build

1. **Build the payload projects**:
   ```bash
   dotnet build src/Pick6.Core -c Release
   dotnet build src/Pick6.Projection -c Release
   # Add other payload projects as needed
   ```

2. **Stage the assemblies**:
   Create a staging directory and copy the built assemblies:
   ```bash
   mkdir payload-staging
   cp src/Pick6.Core/bin/Release/net8.0/*.dll payload-staging/
   cp src/Pick6.Projection/bin/Release/net8.0/*.dll payload-staging/
   # Copy other required files
   ```

3. **Create the ZIP file**:
   ```bash
   cd payload-staging
   zip -r ../pick6-payload-1.0.0.zip *
   cd ..
   ```

4. **Calculate SHA256 hash**:
   ```bash
   # On Windows (PowerShell):
   Get-FileHash pick6-payload-1.0.0.zip -Algorithm SHA256
   
   # On Linux/macOS:
   sha256sum pick6-payload-1.0.0.zip
   ```

## Creating the Manifest

Create a JSON manifest file describing your payload:

```json
{
  "payloadVersion": "1.0.0",
  "payloadUrl": "https://your-server.com/releases/pick6-payload-1.0.0.zip",
  "sha256": "your-calculated-sha256-hash",
  "entryAssembly": "Pick6.Core.dll",
  "entryType": "Pick6.Core.PayloadEntry",
  "entryMethod": "Main"
}
```

### Manifest Fields

- **payloadVersion**: Version string for the payload (semantic versioning recommended)
- **payloadUrl**: Direct download URL for the payload ZIP file
- **sha256**: SHA256 hash of the ZIP file for integrity verification (lowercase)
- **entryAssembly**: Name of the main assembly containing the entry point
- **entryType**: Fully qualified type name containing the entry method
- **entryMethod**: Name of the static method to invoke (should accept string[] args or no parameters)

## Entry Point Requirements

The payload entry method must be:
- Static
- Public
- Accept either no parameters or `string[] args`
- Located in the specified assembly and type

Example entry point:
```csharp
namespace Pick6.Core
{
    public class PayloadEntry
    {
        public static void Main(string[] args)
        {
            // Your application logic here
            Console.WriteLine("Payload loaded successfully!");
        }
    }
}
```

## Deployment

1. **Upload the payload ZIP** to your web server at the URL specified in the manifest
2. **Upload the manifest JSON** to the URL configured in the loader (see `MANIFEST_URL` in `Program.cs`)
3. **Test the update process** by running the loader with dynamic payloads enabled

## Configuration

To enable dynamic payload loading in the loader:

1. Set `ENABLE_DYNAMIC_PAYLOAD = true` in `src/Pick6.Loader/Program.cs`
2. Update `MANIFEST_URL` to point to your manifest JSON file
3. Rebuild the loader

## Security Considerations

- Always use HTTPS for manifest and payload URLs
- Verify SHA256 hashes match exactly
- Keep your web server and payload files secure
- Consider code signing for additional security
- Test payloads thoroughly before deployment

## Troubleshooting

### Common Issues

1. **"Payload integrity verification failed"**
   - Ensure the SHA256 hash in the manifest matches the actual ZIP file
   - Check that the file wasn't corrupted during upload

2. **"Type 'X' not found in payload assembly"**
   - Verify the `entryType` in the manifest matches the actual type name
   - Ensure all required dependencies are included in the payload

3. **"Static method 'X' not found"**
   - Check that the entry method is public and static
   - Verify the method name matches the manifest

4. **Assembly loading errors**
   - Ensure all dependent assemblies are included in the payload
   - Check for version compatibility issues

### Debug Mode

Enable detailed logging by running the loader with console output visible to see update process details.