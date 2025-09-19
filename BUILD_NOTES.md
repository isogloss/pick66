# Build Environment Notes

## Linux Build Issues (Development Only)

This repository is designed for Windows development and deployment. The Linux build environment has limitations:

### Issues on Linux:
1. **WindowsDesktop SDK**: Linux .NET SDK doesn't include Windows Desktop workload
2. **Platform Versions**: Windows-specific platform version settings cause errors
3. **Windows Forms**: Not available on Linux .NET SDK

### For Windows Development:
The project files are correctly configured for Windows and should build without issues when:
- Using Windows with .NET 8 SDK
- Windows Desktop workload is installed
- All required Windows SDKs are present

### CI/CD Note:
For automated builds, use Windows-based agents or containers with Windows Desktop workload installed.

## Testing the Installer Logic

Since we can't build Windows executables on Linux, the installer scripts include fallback logic:
1. Look for pre-built executable first
2. Try to build from source if .NET SDK available
3. Download from GitHub releases as fallback
4. Provide clear error messages if all methods fail

This ensures users can install Pick66 even if the build environment varies.