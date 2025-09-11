#if WINDOWS
using System;
using System.IO;
using System.Diagnostics;

namespace Pick66.Launcher;

/// <summary>
/// Manages proxy DLL installation, uninstallation, and status checking
/// </summary>
public class ProxyManager
{
    private const string PROXY_DXGI_DLL = "dxgi.dll";
    private const string PROXY_D3D11_DLL = "d3d11.dll";
    private const string BACKUP_SUFFIX = ".pick66_backup";
    private const string CONFIG_FILE = "pick66_config.txt";

    /// <summary>
    /// Install proxy DLL in target directory
    /// </summary>
    public ProxyInstallResult InstallProxy(string targetDirectory, ProxyType proxyType, bool enableOverlay, bool autoBackup)
    {
        try
        {
            if (!Directory.Exists(targetDirectory))
            {
                return new ProxyInstallResult 
                { 
                    Success = false, 
                    ErrorMessage = "Target directory does not exist" 
                };
            }

            // Determine which proxy DLL to use
            var selectedProxyType = proxyType;
            if (proxyType == ProxyType.AutoDetect)
            {
                selectedProxyType = DetectBestProxyType(targetDirectory);
            }

            var proxyDllName = selectedProxyType == ProxyType.DXGI ? PROXY_DXGI_DLL : PROXY_D3D11_DLL;
            var targetDllPath = Path.Combine(targetDirectory, proxyDllName);

            // Check if target DLL already exists and backup if needed
            if (File.Exists(targetDllPath) && autoBackup)
            {
                var backupPath = targetDllPath + BACKUP_SUFFIX;
                if (!File.Exists(backupPath))
                {
                    File.Copy(targetDllPath, backupPath, false);
                }
            }

            // Copy our proxy DLL to target location
            var sourceDllPath = GetProxyDllSourcePath(selectedProxyType);
            if (!File.Exists(sourceDllPath))
            {
                return new ProxyInstallResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Proxy DLL not found: {sourceDllPath}" 
                };
            }

            File.Copy(sourceDllPath, targetDllPath, true);

            // Create configuration file
            CreateConfigFile(targetDirectory, selectedProxyType, enableOverlay);

            return new ProxyInstallResult { Success = true };
        }
        catch (Exception ex)
        {
            return new ProxyInstallResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    /// <summary>
    /// Uninstall proxy DLL from target directory
    /// </summary>
    public ProxyInstallResult UninstallProxy(string targetDirectory)
    {
        try
        {
            if (!Directory.Exists(targetDirectory))
            {
                return new ProxyInstallResult 
                { 
                    Success = false, 
                    ErrorMessage = "Target directory does not exist" 
                };
            }

            var configPath = Path.Combine(targetDirectory, CONFIG_FILE);
            var config = LoadConfig(configPath);

            if (config != null)
            {
                var proxyDllName = config.ProxyType == ProxyType.DXGI ? PROXY_DXGI_DLL : PROXY_D3D11_DLL;
                var targetDllPath = Path.Combine(targetDirectory, proxyDllName);
                var backupPath = targetDllPath + BACKUP_SUFFIX;

                // Remove proxy DLL
                if (File.Exists(targetDllPath))
                {
                    File.Delete(targetDllPath);
                }

                // Restore backup if it exists
                if (File.Exists(backupPath))
                {
                    File.Move(backupPath, targetDllPath);
                }

                // Remove config file
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }
            }
            else
            {
                // Fallback: try to remove both possible proxy DLLs
                RemoveProxyDllIfExists(targetDirectory, PROXY_DXGI_DLL);
                RemoveProxyDllIfExists(targetDirectory, PROXY_D3D11_DLL);
            }

            return new ProxyInstallResult { Success = true };
        }
        catch (Exception ex)
        {
            return new ProxyInstallResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    /// <summary>
    /// Get current proxy installation status
    /// </summary>
    public ProxyStatus GetProxyStatus(string targetDirectory)
    {
        var status = new ProxyStatus();

        if (!Directory.Exists(targetDirectory))
            return status;

        var configPath = Path.Combine(targetDirectory, CONFIG_FILE);
        var config = LoadConfig(configPath);

        if (config != null)
        {
            var proxyDllName = config.ProxyType == ProxyType.DXGI ? PROXY_DXGI_DLL : PROXY_D3D11_DLL;
            var targetDllPath = Path.Combine(targetDirectory, proxyDllName);
            var backupPath = targetDllPath + BACKUP_SUFFIX;

            status.IsInstalled = File.Exists(targetDllPath) && IsOurProxyDll(targetDllPath);
            status.InstalledType = config.ProxyType;
            status.ProxyDllPath = targetDllPath;
            status.OverlayEnabled = config.OverlayEnabled;
            status.HasBackup = File.Exists(backupPath);
            status.BackupPath = status.HasBackup ? backupPath : null;
        }
        else
        {
            // Check for presence of our proxy DLLs without config
            var dxgiPath = Path.Combine(targetDirectory, PROXY_DXGI_DLL);
            var d3d11Path = Path.Combine(targetDirectory, PROXY_D3D11_DLL);

            if (File.Exists(dxgiPath) && IsOurProxyDll(dxgiPath))
            {
                status.IsInstalled = true;
                status.InstalledType = ProxyType.DXGI;
                status.ProxyDllPath = dxgiPath;
            }
            else if (File.Exists(d3d11Path) && IsOurProxyDll(d3d11Path))
            {
                status.IsInstalled = true;
                status.InstalledType = ProxyType.D3D11;
                status.ProxyDllPath = d3d11Path;
            }
        }

        return status;
    }

    private ProxyType DetectBestProxyType(string targetDirectory)
    {
        // Check if existing system DLLs are present
        var dxgiPath = Path.Combine(targetDirectory, PROXY_DXGI_DLL);
        var d3d11Path = Path.Combine(targetDirectory, PROXY_D3D11_DLL);

        // Prefer DXGI as it's typically loaded earlier in the graphics pipeline
        if (File.Exists(dxgiPath))
        {
            return ProxyType.DXGI;
        }
        else if (File.Exists(d3d11Path))
        {
            return ProxyType.D3D11;
        }

        // Default to DXGI for modern DirectX applications
        return ProxyType.DXGI;
    }

    private string GetProxyDllSourcePath(ProxyType proxyType)
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var dllsDirectory = Path.Combine(appDirectory, "ProxyDlls");
        
        var dllName = proxyType == ProxyType.DXGI ? PROXY_DXGI_DLL : PROXY_D3D11_DLL;
        return Path.Combine(dllsDirectory, dllName);
    }

    private void CreateConfigFile(string targetDirectory, ProxyType proxyType, bool enableOverlay)
    {
        var configPath = Path.Combine(targetDirectory, CONFIG_FILE);
        var configContent = $"ProxyType={proxyType}\n" +
                           $"OverlayEnabled={enableOverlay}\n" +
                           $"InstallDate={DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                           $"Version=1.0.0\n";

        File.WriteAllText(configPath, configContent);
    }

    private ProxyConfig? LoadConfig(string configPath)
    {
        if (!File.Exists(configPath))
            return null;

        try
        {
            var lines = File.ReadAllLines(configPath);
            var config = new ProxyConfig();

            foreach (var line in lines)
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "ProxyType":
                        if (Enum.TryParse<ProxyType>(value, out var proxyType))
                            config.ProxyType = proxyType;
                        break;
                    case "OverlayEnabled":
                        if (bool.TryParse(value, out var overlayEnabled))
                            config.OverlayEnabled = overlayEnabled;
                        break;
                }
            }

            return config;
        }
        catch
        {
            return null;
        }
    }

    private void RemoveProxyDllIfExists(string targetDirectory, string dllName)
    {
        var dllPath = Path.Combine(targetDirectory, dllName);
        var backupPath = dllPath + BACKUP_SUFFIX;

        if (File.Exists(dllPath) && IsOurProxyDll(dllPath))
        {
            File.Delete(dllPath);

            // Restore backup if it exists
            if (File.Exists(backupPath))
            {
                File.Move(backupPath, dllPath);
            }
        }
    }

    private bool IsOurProxyDll(string dllPath)
    {
        try
        {
            // Check if this is our proxy DLL by examining the file version or a signature
            var versionInfo = FileVersionInfo.GetVersionInfo(dllPath);
            return versionInfo.CompanyName?.Contains("Pick66") == true ||
                   versionInfo.ProductName?.Contains("Pick66") == true ||
                   versionInfo.FileDescription?.Contains("Pick66 Proxy") == true;
        }
        catch
        {
            // If we can't read version info, assume it's not ours
            return false;
        }
    }

    private class ProxyConfig
    {
        public ProxyType ProxyType { get; set; } = ProxyType.AutoDetect;
        public bool OverlayEnabled { get; set; } = true;
    }
}
#endif