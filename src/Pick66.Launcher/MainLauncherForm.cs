#if WINDOWS
using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace Pick66.Launcher;

/// <summary>
/// Main launcher form for Pick66 - handles proxy DLL installation and game directory selection
/// </summary>
public partial class MainLauncherForm : Form
{
    private Button btnBrowseDirectory;
    private Button btnInstallProxy;
    private Button btnUninstallProxy;
    private Button btnLaunchGame;
    private TextBox txtTargetDirectory;
    private TextBox txtExecutablePath;
    private Label lblStatus;
    private Label lblProxyStatus;
    private ComboBox cmbProxyType;
    private CheckBox chkEnableOverlay;
    private CheckBox chkAutoBackup;
    private GroupBox grpTargetSelection;
    private GroupBox grpProxyOptions;
    private GroupBox grpStatus;
    private Button btnBrowseExecutable;

    private readonly ProxyManager _proxyManager;

    public MainLauncherForm()
    {
        _proxyManager = new ProxyManager();
        InitializeComponent();
        InitializeForm();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form properties
        this.Text = "Pick66 Launcher - D3D11/DXGI Proxy Injection";
        this.Size = new Size(600, 480);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = true;

        // Target Selection Group
        grpTargetSelection = new GroupBox()
        {
            Text = "Target Game Directory",
            Location = new Point(12, 12),
            Size = new Size(560, 120)
        };

        var lblDirectory = new Label()
        {
            Text = "Game Directory:",
            Location = new Point(12, 25),
            Size = new Size(100, 23)
        };

        txtTargetDirectory = new TextBox()
        {
            Location = new Point(12, 48),
            Size = new Size(420, 23),
            PlaceholderText = "Select FiveM or GTA V installation directory..."
        };

        btnBrowseDirectory = new Button()
        {
            Text = "Browse...",
            Location = new Point(438, 47),
            Size = new Size(100, 25)
        };

        var lblExecutable = new Label()
        {
            Text = "Executable:",
            Location = new Point(12, 75),
            Size = new Size(100, 23)
        };

        txtExecutablePath = new TextBox()
        {
            Location = new Point(120, 75),
            Size = new Size(312, 23),
            PlaceholderText = "Game executable (auto-detected)..."
        };

        btnBrowseExecutable = new Button()
        {
            Text = "Browse...",
            Location = new Point(438, 74),
            Size = new Size(100, 25)
        };

        grpTargetSelection.Controls.AddRange(new Control[] {
            lblDirectory, txtTargetDirectory, btnBrowseDirectory,
            lblExecutable, txtExecutablePath, btnBrowseExecutable
        });

        // Proxy Options Group
        grpProxyOptions = new GroupBox()
        {
            Text = "Proxy Configuration",
            Location = new Point(12, 142),
            Size = new Size(560, 120)
        };

        var lblProxyType = new Label()
        {
            Text = "Proxy DLL:",
            Location = new Point(12, 25),
            Size = new Size(80, 23)
        };

        cmbProxyType = new ComboBox()
        {
            Location = new Point(98, 25),
            Size = new Size(150, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbProxyType.Items.AddRange(new[] { "Auto-detect", "dxgi.dll", "d3d11.dll" });
        cmbProxyType.SelectedIndex = 0;

        chkEnableOverlay = new CheckBox()
        {
            Text = "Enable ImGui overlay (Alt+F12 to toggle)",
            Location = new Point(12, 55),
            Size = new Size(280, 23),
            Checked = true
        };

        chkAutoBackup = new CheckBox()
        {
            Text = "Automatically backup existing DLLs",
            Location = new Point(12, 80),
            Size = new Size(250, 23),
            Checked = true
        };

        btnInstallProxy = new Button()
        {
            Text = "Install Proxy Hook",
            Location = new Point(350, 25),
            Size = new Size(120, 30),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        btnUninstallProxy = new Button()
        {
            Text = "Uninstall Proxy",
            Location = new Point(350, 60),
            Size = new Size(120, 30),
            BackColor = Color.FromArgb(196, 43, 28),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        btnLaunchGame = new Button()
        {
            Text = "Launch Game",
            Location = new Point(480, 25),
            Size = new Size(70, 65),
            BackColor = Color.FromArgb(16, 124, 16),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };

        grpProxyOptions.Controls.AddRange(new Control[] {
            lblProxyType, cmbProxyType, chkEnableOverlay, chkAutoBackup,
            btnInstallProxy, btnUninstallProxy, btnLaunchGame
        });

        // Status Group
        grpStatus = new GroupBox()
        {
            Text = "Status",
            Location = new Point(12, 272),
            Size = new Size(560, 160)
        };

        lblStatus = new Label()
        {
            Text = "Ready - Select target directory to begin",
            Location = new Point(12, 25),
            Size = new Size(536, 23),
            ForeColor = Color.Blue
        };

        lblProxyStatus = new Label()
        {
            Text = "Proxy Status: Not installed",
            Location = new Point(12, 50),
            Size = new Size(536, 100),
            ForeColor = Color.Gray,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(248, 248, 248)
        };

        grpStatus.Controls.AddRange(new Control[] { lblStatus, lblProxyStatus });

        // Add all groups to form
        this.Controls.AddRange(new Control[] { grpTargetSelection, grpProxyOptions, grpStatus });

        this.ResumeLayout(false);
    }

    private void InitializeForm()
    {
        // Wire up event handlers
        btnBrowseDirectory.Click += BtnBrowseDirectory_Click;
        btnBrowseExecutable.Click += BtnBrowseExecutable_Click;
        btnInstallProxy.Click += BtnInstallProxy_Click;
        btnUninstallProxy.Click += BtnUninstallProxy_Click;
        btnLaunchGame.Click += BtnLaunchGame_Click;
        txtTargetDirectory.TextChanged += TxtTargetDirectory_TextChanged;
        cmbProxyType.SelectedIndexChanged += CmbProxyType_SelectedIndexChanged;

        UpdateUI();
    }

    private void BtnBrowseDirectory_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog()
        {
            Description = "Select FiveM or GTA V installation directory",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtTargetDirectory.Text = dialog.SelectedPath;
            DetectGameExecutable(dialog.SelectedPath);
        }
    }

    private void BtnBrowseExecutable_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog()
        {
            Title = "Select Game Executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            InitialDirectory = txtTargetDirectory.Text
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtExecutablePath.Text = dialog.FileName;
        }
    }

    private void DetectGameExecutable(string gameDirectory)
    {
        var commonExecutables = new[]
        {
            "FiveM.exe",
            "FiveM_b2060.exe",
            "FiveM_b2189.exe", 
            "FiveM_b2372.exe",
            "FiveM_b2545.exe",
            "FiveM_b2612.exe",
            "FiveM_b2699.exe",
            "FiveM_b2802.exe",
            "FiveM_b2944.exe",
            "GTA5.exe",
            "PlayGTAV.exe"
        };

        foreach (var exe in commonExecutables)
        {
            var fullPath = Path.Combine(gameDirectory, exe);
            if (File.Exists(fullPath))
            {
                txtExecutablePath.Text = fullPath;
                break;
            }
        }
    }

    private void TxtTargetDirectory_TextChanged(object? sender, EventArgs e)
    {
        UpdateUI();
    }

    private void CmbProxyType_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateUI();
    }

    private void BtnInstallProxy_Click(object? sender, EventArgs e)
    {
        try
        {
            var targetDir = txtTargetDirectory.Text;
            var proxyType = GetSelectedProxyType();
            var enableOverlay = chkEnableOverlay.Checked;
            var autoBackup = chkAutoBackup.Checked;

            lblStatus.Text = "Installing proxy DLL...";
            lblStatus.ForeColor = Color.Orange;
            Application.DoEvents();

            var result = _proxyManager.InstallProxy(targetDir, proxyType, enableOverlay, autoBackup);
            
            if (result.Success)
            {
                lblStatus.Text = "Proxy installed successfully!";
                lblStatus.ForeColor = Color.Green;
                btnLaunchGame.Enabled = true;
            }
            else
            {
                lblStatus.Text = $"Installation failed: {result.ErrorMessage}";
                lblStatus.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Installation error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
        }
        
        UpdateProxyStatus();
    }

    private void BtnUninstallProxy_Click(object? sender, EventArgs e)
    {
        try
        {
            var targetDir = txtTargetDirectory.Text;
            
            lblStatus.Text = "Uninstalling proxy DLL...";
            lblStatus.ForeColor = Color.Orange;
            Application.DoEvents();

            var result = _proxyManager.UninstallProxy(targetDir);
            
            if (result.Success)
            {
                lblStatus.Text = "Proxy uninstalled successfully!";
                lblStatus.ForeColor = Color.Green;
                btnLaunchGame.Enabled = false;
            }
            else
            {
                lblStatus.Text = $"Uninstallation failed: {result.ErrorMessage}";
                lblStatus.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Uninstallation error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
        }
        
        UpdateProxyStatus();
    }

    private void BtnLaunchGame_Click(object? sender, EventArgs e)
    {
        try
        {
            var executablePath = txtExecutablePath.Text;
            if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
            {
                MessageBox.Show("Please select a valid game executable.", "Launch Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblStatus.Text = "Launching game...";
            lblStatus.ForeColor = Color.Blue;
            
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = true
            });

            lblStatus.Text = "Game launched successfully!";
            lblStatus.ForeColor = Color.Green;
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Launch error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
        }
    }

    private ProxyType GetSelectedProxyType()
    {
        return cmbProxyType.SelectedIndex switch
        {
            1 => ProxyType.DXGI,
            2 => ProxyType.D3D11,
            _ => ProxyType.AutoDetect
        };
    }

    private void UpdateUI()
    {
        var hasValidDirectory = !string.IsNullOrEmpty(txtTargetDirectory.Text) && 
                               Directory.Exists(txtTargetDirectory.Text);

        btnInstallProxy.Enabled = hasValidDirectory;
        btnUninstallProxy.Enabled = hasValidDirectory;

        if (hasValidDirectory)
        {
            UpdateProxyStatus();
        }
        else
        {
            lblProxyStatus.Text = "Proxy Status: Select a valid directory";
            lblProxyStatus.ForeColor = Color.Gray;
        }
    }

    private void UpdateProxyStatus()
    {
        var targetDir = txtTargetDirectory.Text;
        if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
            return;

        var status = _proxyManager.GetProxyStatus(targetDir);
        
        var statusText = status.IsInstalled ? 
            $"Proxy Status: Installed ({status.InstalledType})\n" +
            $"DLL Path: {status.ProxyDllPath}\n" +
            $"Overlay: {(status.OverlayEnabled ? "Enabled" : "Disabled")}\n" +
            $"Backup: {(status.HasBackup ? $"Yes ({status.BackupPath})" : "No")}"
            :
            "Proxy Status: Not installed";

        lblProxyStatus.Text = statusText;
        lblProxyStatus.ForeColor = status.IsInstalled ? Color.Green : Color.Gray;
        
        btnLaunchGame.Enabled = status.IsInstalled && !string.IsNullOrEmpty(txtExecutablePath.Text);
    }
}

/// <summary>
/// Proxy installation result
/// </summary>
public class ProxyInstallResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Proxy status information
/// </summary>
public class ProxyStatus
{
    public bool IsInstalled { get; set; }
    public ProxyType InstalledType { get; set; }
    public string? ProxyDllPath { get; set; }
    public bool OverlayEnabled { get; set; }
    public bool HasBackup { get; set; }
    public string? BackupPath { get; set; }
}

/// <summary>
/// Proxy DLL type
/// </summary>
public enum ProxyType
{
    AutoDetect,
    DXGI,
    D3D11
}
#endif