# Quick Fix Guide: "Core hook DLL not found" Error

## The Error

```
[Error] Core hook DLL not found: C:\Users\...\AppData\Local\Temp\.net\...
[Error] × All injection strategies failed: Core hook DLL not found: C:\Users\...
```

## What This Means

The Pick6 application cannot find the `Pick6VulkanHook.dll` file, which is required for capturing game frames from FiveM. This DLL must be located in the same directory as the `loader.exe` executable.

## Quick Fix (5 minutes)

### Step 1: Locate Your Pick6 Installation

The application is installed in one of these locations:
- `C:\Users\[YourUsername]\Downloads\Pick66\`
- The directory where you extracted Pick6
- The location shown in your Start menu shortcut

### Step 2: Check What's Missing

Open the installation directory and check for these files:

- ✅ `loader.exe` - Main executable (should be present)
- ❓ `Pick6VulkanHook.dll` - **REQUIRED** (likely missing if you see this error)
- ❓ `Pick6Native.dll` - Optional but recommended

### Step 3: Obtain the Missing DLL

#### Option A: Download from Release
1. Go to the [Pick6 Releases page](https://github.com/isogloss/pick66/releases)
2. Download the latest `Pick6-v{version}.zip` that includes all required DLLs
3. Extract to your installation directory
4. Run `loader.exe` again

#### Option B: Build from Source
If DLLs are not included in releases:

1. Clone the repository:
   ```bash
   git clone https://github.com/isogloss/pick66.git
   cd pick66
   ```

2. Run the build script:
   ```bash
   build.bat
   ```

3. Copy the DLLs to your installation:
   ```bash
   copy src\Pick6.Loader\Pick6VulkanHook.dll C:\Users\[YourUsername]\Downloads\Pick66\
   copy src\Pick6.Loader\Pick6Native.dll C:\Users\[YourUsername]\Downloads\Pick66\
   ```

#### Option C: Build Native Components Only
If you already have the loader but need the DLLs:

1. See [DLL_DEPLOYMENT_GUIDE.md](DLL_DEPLOYMENT_GUIDE.md) for detailed build instructions
2. Build the Vulkan hook DLL project
3. Copy `Pick6VulkanHook.dll` to your installation directory

### Step 4: Verify the Fix

After copying the DLLs, your installation directory should look like:

```
Pick66/
├── loader.exe           ✅
├── Pick6VulkanHook.dll  ✅ (REQUIRED - must be present)
├── Pick6Native.dll      ✅ (optional)
└── other files...
```

Run `loader.exe` again. The error should be gone!

## Why This Happens

The Pick6 application uses **single-file publishing** which bundles most files into a single `.exe`. However, DLLs used for injection into other processes (like FiveM) **cannot be bundled** and must be deployed separately.

### Previous Issue
Earlier versions used `AppDomain.CurrentDomain.BaseDirectory` to locate DLLs, which returns a temporary extraction directory when using single-file publishing. This meant DLLs couldn't be found even if they were in the same directory as the executable.

### Current Fix
The latest version uses `Environment.ProcessPath` to get the actual executable location, which correctly finds DLLs in the same directory. However, **the DLLs must still be present** in that directory.

## Still Having Issues?

### Issue: DLLs are present but error persists

**Possible causes:**
1. **Antivirus blocking** - Temporarily disable antivirus and try again
2. **File permissions** - Right-click the DLL → Properties → Unblock
3. **Wrong DLL version** - Ensure DLLs match your Pick6 version
4. **Corrupted DLL** - Re-download or rebuild the DLL

**Solution:**
```bash
# Check if DLL is blocked
Right-click Pick6VulkanHook.dll → Properties → Check "Unblock" if present → OK

# Verify DLL is readable
powershell -Command "Get-Item Pick6VulkanHook.dll | Select-Object *"
```

### Issue: Can't find Pick6VulkanHook.dll to download

This DLL is part of the native Vulkan hook component. If it's not included in releases:

1. Check the repository issues for build instructions
2. Ask in discussions for a pre-built version
3. Build it yourself (see [DLL_DEPLOYMENT_GUIDE.md](DLL_DEPLOYMENT_GUIDE.md))

### Issue: Application was working before

**Possible causes:**
1. DLLs were deleted (maybe by antivirus or cleanup tool)
2. Application was updated but DLLs weren't
3. Installation directory changed

**Solution:**
- Re-download the complete package
- Check antivirus quarantine
- Restore from backup if available

## Preventing This Issue

To avoid this error in the future:

1. **Always extract the complete ZIP** - Don't just copy the .exe
2. **Whitelist in antivirus** - Add the installation directory to exclusions
3. **Don't separate files** - Keep all DLLs with the executable
4. **Update properly** - When updating, replace all files, not just the .exe

## Need More Help?

- 📖 Read the [DLL_DEPLOYMENT_GUIDE.md](DLL_DEPLOYMENT_GUIDE.md)
- 💬 Open an issue on GitHub
- 🔍 Check existing issues for similar problems
- 📧 Contact the maintainers

## Technical Details (For Developers)

The error occurs in these files:
- `src/Pick6.Core/HybridInjector.cs` - Line ~50
- `src/Pick6.Core/EnhancedInjector.cs` - Line ~36

The application now uses `PathResolver.GetExecutableDirectory()` which correctly handles single-file publishing by using `Environment.ProcessPath` instead of `AppDomain.CurrentDomain.BaseDirectory`.

For the complete technical explanation, see:
- [DLL_DEPLOYMENT_GUIDE.md](DLL_DEPLOYMENT_GUIDE.md)
- [src/Pick6.Core/PathResolver.cs](src/Pick6.Core/PathResolver.cs)
