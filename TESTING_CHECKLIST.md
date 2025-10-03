# Testing Checklist for Single Executable Build

This checklist helps verify that the single executable build implementation works correctly.

## Pre-Testing Setup

- [ ] Ensure you have access to GitHub Actions in the repository
- [ ] Have a Windows 10/11 x64 machine for runtime testing
- [ ] Install FiveM for integration testing (optional)

## Build Testing (GitHub Actions)

### Trigger the Workflow

- [ ] Go to GitHub Actions tab
- [ ] Select "Build Pick6 Single Executable" workflow
- [ ] Click "Run workflow" on main branch
- [ ] Wait for workflow to complete (5-10 minutes expected)

### Verify Build Steps

- [ ] **Setup Environment** completes successfully
  - .NET 8 SDK installed
  - CMake installed
  - MSVC/Visual Studio setup
  
- [ ] **Build Pick6Native.dll** completes successfully
  - CMake configures without errors
  - C++ compilation succeeds
  - DLL is created in build/Release/
  - Size verification passes

- [ ] **Create Placeholder Pick6VulkanHook.dll** completes successfully
  - Stub DLL is created
  - Has required exports (minimal)
  
- [ ] **Copy DLLs to Loader project** completes successfully
  - Pick6Native.dll copied
  - Pick6VulkanHook.dll copied
  - Files listed in output

- [ ] **Restore dependencies** completes successfully
  - NuGet packages restored
  - Costura.Fody package present

- [ ] **Build single-file executable** completes successfully
  - dotnet publish succeeds
  - No build errors or warnings
  - Output created in dist/

- [ ] **Verify pick6.exe** completes successfully
  - pick6.exe exists
  - File size is reasonable (50-100 MB expected)
  - **No separate DLL files in dist/** (important!)

- [ ] **Upload artifacts** completes successfully
  - Artifact uploaded to GitHub
  - Contains pick6.exe and versioned copy

### Download and Inspect Artifact

- [ ] Download the build artifact from GitHub Actions
- [ ] Extract the artifact ZIP
- [ ] Verify contents:
  - [ ] pick6.exe present
  - [ ] Pick6-v{version}.exe present
  - [ ] **No .dll files** (all should be embedded)
- [ ] Check file size (should be 50-100 MB typically)

## Runtime Testing (Windows)

### Basic Execution

- [ ] Download pick6.exe to a test directory
- [ ] Run pick6.exe (double-click or command line)
- [ ] Application launches without errors
- [ ] No "DLL not found" errors appear
- [ ] Application shows GUI or console interface

### DLL Extraction Verification

- [ ] Open File Explorer
- [ ] Navigate to `%TEMP%\Pick6\Native\`
- [ ] Verify extracted DLLs:
  - [ ] Pick6VulkanHook.dll exists
  - [ ] Pick6Native.dll exists
  - [ ] Files have reasonable size (> 0 bytes)
  - [ ] Files are dated from current run

### Administrator Privileges

- [ ] Close pick6.exe
- [ ] Delete `%TEMP%\Pick6\Native\` directory
- [ ] Right-click pick6.exe → Run as Administrator
- [ ] Verify DLLs are extracted again
- [ ] Application runs without errors

### Extraction Error Handling

- [ ] Create `%TEMP%\Pick6\Native\` directory
- [ ] Set directory to read-only (Properties → Attributes)
- [ ] Run pick6.exe
- [ ] Verify appropriate error message appears
- [ ] Verify application handles error gracefully
- [ ] Remove read-only attribute

## Integration Testing (Optional - Requires FiveM)

### With FiveM Running

- [ ] Start FiveM
- [ ] Connect to a server
- [ ] Run pick6.exe
- [ ] Select injection method (if prompted)
- [ ] Verify injection succeeds
- [ ] Check for frame capture
- [ ] Verify projection window appears
- [ ] Test real-time frame display

### Injection Strategies

- [ ] Test with Pick6Native.dll injection
- [ ] Test with managed C# injection (if fallback occurs)
- [ ] Test with proxy DLL injection (if configured)
- [ ] Verify fallback works if primary method fails

## Compatibility Testing

### Different Windows Versions

- [ ] Test on Windows 10 (if available)
- [ ] Test on Windows 11 (if available)
- [ ] Test on fresh Windows install (no dev tools)

### Different Scenarios

- [ ] Test with antivirus enabled
- [ ] Test with Windows Defender enabled
- [ ] Test with limited user account (non-admin)
- [ ] Test with admin account

### Network and Storage

- [ ] Test with network drive (copy exe to network location)
- [ ] Test with USB drive (run from external storage)
- [ ] Test with different temp directory locations

## Documentation Verification

### User Documentation

- [ ] Read readme.md installation instructions
- [ ] Follow installation steps as a new user would
- [ ] Verify instructions are accurate and complete

### Developer Documentation

- [ ] Read SINGLE_EXECUTABLE_BUILD.md
- [ ] Verify technical details are accurate
- [ ] Check code examples and commands

### Workflow Documentation

- [ ] Read .github/workflows/README.md
- [ ] Verify workflow descriptions match actual behavior
- [ ] Check troubleshooting section

## Regression Testing

### Existing Functionality

- [ ] All original features still work
- [ ] No functionality removed or broken
- [ ] Performance is similar to previous version

### Error Messages

- [ ] Error messages are clear and helpful
- [ ] No confusing or outdated error text
- [ ] Troubleshooting information provided

## Performance Testing

### Startup Time

- [ ] Record startup time (first run)
- [ ] Record startup time (subsequent runs)
- [ ] Compare to previous version if available
- [ ] Verify < 5 second startup (typical)

### DLL Extraction Time

- [ ] Time the DLL extraction process
- [ ] Verify < 100ms for extraction
- [ ] Check that extraction only happens once

### Memory Usage

- [ ] Check memory usage at startup
- [ ] Check memory usage during operation
- [ ] Verify no memory leaks over time

## Edge Cases and Error Conditions

### Corrupted Installation

- [ ] Delete Pick6VulkanHook.dll from temp before embedding
- [ ] Build and test (should fail build)
- [ ] Restore DLL and rebuild

### Disk Space

- [ ] Test with very limited disk space in %TEMP%
- [ ] Verify appropriate error message
- [ ] Verify graceful failure

### File Permissions

- [ ] Test with read-only %TEMP% directory
- [ ] Test with no write permissions
- [ ] Verify error handling

## Clean Up Testing

### Uninstallation

- [ ] Delete pick6.exe
- [ ] Delete `%TEMP%\Pick6\` directory
- [ ] Verify no other files left behind
- [ ] Check registry (should be empty)

### Update Testing

- [ ] Run version 1.0 (or previous version)
- [ ] Extract DLLs to temp
- [ ] Replace with version 2.0 (new build)
- [ ] Run new version
- [ ] Verify old DLLs are overwritten
- [ ] Verify no conflicts

## Security Testing

### Antivirus Scanning

- [ ] Scan pick6.exe with Windows Defender
- [ ] Scan pick6.exe with third-party antivirus
- [ ] Check for false positives
- [ ] Document any flagged behavior

### Digital Signature (Future)

- [ ] Check if executable is signed (currently not)
- [ ] Verify signature if present
- [ ] Note: Add signing in future enhancement

## Release Validation

### GitHub Release

- [ ] Trigger release by creating git tag
- [ ] Verify release is created automatically
- [ ] Check release notes
- [ ] Download asset from release
- [ ] Verify asset matches build artifact

### Version Information

- [ ] Check pick6.exe properties
- [ ] Verify version number matches tag
- [ ] Check file description

## Issue Reporting

If any test fails, report an issue with:

- [ ] Test case that failed
- [ ] Expected behavior
- [ ] Actual behavior
- [ ] Error messages (if any)
- [ ] Screenshots (if applicable)
- [ ] System information (Windows version, etc)
- [ ] Steps to reproduce

## Test Results Summary

**Test Date**: _______________  
**Tester**: _______________  
**Build Version**: _______________  
**Windows Version**: _______________  

**Overall Result**: ☐ Pass  ☐ Fail  ☐ Partial

**Notes**:
```
[Add any additional notes, observations, or issues found during testing]
```

## Sign-Off

- [ ] All critical tests passed
- [ ] All blockers resolved
- [ ] Documentation reviewed and accurate
- [ ] Ready for production deployment

**Tested By**: _______________  
**Date**: _______________  
**Signature**: _______________
