using Pick6.Core;
using Pick6.Core.Util;
using Pick6.Projection;
using Pick6.Loader.Settings;
using Pick6.Loader.Controllers;
using Pick6.Loader.Logging;
using Pick6.Loader.UI;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Pick6.Loader;

/// <summary>
/// Enhanced GUI form for Pick6 - displays controls for Start/Stop projection/inject and user settings persistence
/// </summary>
public partial class MainForm : Form
{
    private ProjectionController? _projectionController;
    private GlobalKeybindManager? _keybindManager;
    private UserSettings _userSettings;
    private GuiLogSink? _guiLogSink;
    private bool _loaderVisible = true;

    // UI Controls - existing
    private Label _headerLabel = null!;
    private Label _statusLabel = null!;
    private Button _hideButton = null!;

    // UI Controls - new
    private Button _startButton = null!;
    private Button _stopButton = null!;
    private Button _settingsButton = null!;
    private ListBox _logListBox = null!;
    private Panel _controlPanel = null!;
    private Panel _logPanel = null!;

    public MainForm()
    {
        // Load settings first
        _userSettings = SettingsService.TryLoadOrDefault();
        
        InitializeComponent();
        InitializeProjectionController();
        SetupEventHandlers();
        SetupLogging();
        UpdateButtonStates();

        // Auto-start if enabled in settings (after form is shown to be thread-safe)
        if (_userSettings.AutoStartProjection)
        {
            this.Shown += MainForm_Shown;
        }
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        // Auto-start projection after form is shown
        BeginInvoke(() => StartProjection());
    }

    private void InitializeComponent()
    {
        // Form setup - enhanced for new menu functionality
        Text = "Pick6 - Game Capture Menu";
        Size = new Size(480, 400);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(240, 240, 240);
        ForeColor = Color.Black;
        Icon = SystemIcons.Application;

        var headerFont = new Font("Segoe UI", 14, FontStyle.Bold);
        var buttonFont = new Font("Segoe UI", 9);
        var labelFont = new Font("Segoe UI", 9);

        // Header label
        _headerLabel = new Label
        {
            Text = "Pick6 - Game Capture",
            Font = headerFont,
            ForeColor = Color.FromArgb(0, 122, 204),
            BackColor = Color.Transparent,
            Location = new Point(20, 15),
            Size = new Size(300, 25),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Status label
        _statusLabel = new Label
        {
            Text = "Idle",
            Font = labelFont,
            ForeColor = Color.Black,
            BackColor = Color.Transparent,
            Location = new Point(20, 50),
            Size = new Size(200, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Control panel for buttons
        _controlPanel = new Panel
        {
            Location = new Point(20, 80),
            Size = new Size(430, 50),
            BackColor = Color.Transparent
        };

        // Start button
        _startButton = new Button
        {
            Text = "Start Injection",
            Font = buttonFont,
            Location = new Point(0, 10),
            Size = new Size(100, 30),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        _startButton.FlatAppearance.BorderSize = 0;
        _startButton.Click += StartButton_Click;

        // Stop button
        _stopButton = new Button
        {
            Text = "Stop",
            Font = buttonFont,
            Location = new Point(110, 10),
            Size = new Size(80, 30),
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Enabled = false
        };
        _stopButton.FlatAppearance.BorderSize = 0;
        _stopButton.Click += StopButton_Click;

        // Settings button
        _settingsButton = new Button
        {
            Text = "Settings",
            Font = buttonFont,
            Location = new Point(200, 10),
            Size = new Size(80, 30),
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        _settingsButton.FlatAppearance.BorderSize = 0;
        _settingsButton.Click += SettingsButton_Click;

        // Hide button (kept for compatibility)
        _hideButton = new Button
        {
            Text = "Hide",
            Font = new Font("Segoe UI", 8),
            Location = new Point(350, 10),
            Size = new Size(50, 30),
            BackColor = Color.Gray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        _hideButton.FlatAppearance.BorderSize = 0;
        _hideButton.Click += HideButton_Click;

        _controlPanel.Controls.AddRange(new Control[] { _startButton, _stopButton, _settingsButton, _hideButton });

        // Log panel
        _logPanel = new Panel
        {
            Location = new Point(20, 150),
            Size = new Size(430, 200),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var logLabel = new Label
        {
            Text = "Log Output:",
            Font = labelFont,
            Location = new Point(5, 5),
            Size = new Size(100, 15),
            BackColor = Color.Transparent
        };

        _logListBox = new ListBox
        {
            Location = new Point(5, 25),
            Size = new Size(415, 165),
            Font = new Font("Consolas", 8),
            BackColor = Color.Black,
            ForeColor = Color.LimeGreen,
            BorderStyle = BorderStyle.None,
            ScrollAlwaysVisible = false
        };

        _logPanel.Controls.AddRange(new Control[] { logLabel, _logListBox });

        // Add controls to form
        Controls.AddRange(new Control[] { _headerLabel, _statusLabel, _controlPanel, _logPanel });
    }

    private void InitializeProjectionController()
    {
        _projectionController = new ProjectionController();
        
        // Initialize global keybind manager for hotkeys
        _keybindManager = new GlobalKeybindManager();
        SetupGlobalKeybinds();
    }

    private void SetupLogging()
    {
        // Create GUI log sink and register it with the Log system
        _guiLogSink = new GuiLogSink();
        _guiLogSink.LogReceived += GuiLogSink_LogReceived;
        Log.AddSink(_guiLogSink);
        
        AddLogMessage("Info", "Application started");
    }

    private void GuiLogSink_LogReceived(object? sender, Pick6.Loader.Logging.LogEventArgs e)
    {
        BeginInvoke(() => AddLogMessage(e.Level, e.Message));
    }

    private void SetupGlobalKeybinds()
    {
        if (_keybindManager == null) return;

        DefaultKeybinds.RegisterDefaultKeybinds(_keybindManager,
            toggleLoader: () => BeginInvoke(() => ToggleLoaderVisibility()),
            toggleProjection: () => BeginInvoke(() => ToggleProjection()),
            closeProjection: () => BeginInvoke(() => StopProjection()),
            stopProjectionAndRestore: () => BeginInvoke(() => StopProjectionAndRestore()),
            closeProjectionAndToggleLoader: () => BeginInvoke(() => ToggleLoaderVisibility())
        );

        _keybindManager.StartMonitoring();
    }

    private void SetupEventHandlers()
    {
        if (_projectionController == null) return;

        // Handle projection controller events
        _projectionController.StatusChanged += ProjectionController_StatusChanged;
        _projectionController.Log += ProjectionController_Log;
    }

    private void ProjectionController_StatusChanged(object? sender, StatusChangedEventArgs e)
    {
        BeginInvoke(() =>
        {
            UpdateStatus(e.Status, e.Message);
            UpdateButtonStates();
        });
    }

    private void ProjectionController_Log(object? sender, Pick6.Loader.Controllers.LogEventArgs e)
    {
        BeginInvoke(() => AddLogMessage(e.Level, e.Message));
    }

    private void StartButton_Click(object? sender, EventArgs e)
    {
        StartProjection();
    }

    private void StopButton_Click(object? sender, EventArgs e)
    {
        StopProjection();
    }

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        ShowSettingsDialog();
    }

    private void StartProjection()
    {
        if (_projectionController == null) return;

        var success = _projectionController.Start(_userSettings);
        if (!success)
        {
            AddLogMessage("Error", "Failed to start projection");
        }
    }

    private void StopProjection()
    {
        if (_projectionController == null) return;
        _projectionController.Stop();
    }

    private void ShowSettingsDialog()
    {
        using var dialog = new UserSettingsDialog(_userSettings);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _userSettings = dialog.Settings;
            SettingsService.Save(_userSettings);
            AddLogMessage("Info", "Settings saved");
        }
    }

    private void UpdateStatus(ProjectionStatus status, string? message = null)
    {
        string statusText = status switch
        {
            ProjectionStatus.Idle => "Idle",
            ProjectionStatus.Starting => "Starting...",
            ProjectionStatus.Running => "Running",
            ProjectionStatus.Stopping => "Stopping...",
            ProjectionStatus.Error => "Error",
            _ => "Unknown"
        };

        if (!string.IsNullOrEmpty(message))
        {
            statusText = $"{statusText} - {message}";
        }

        _statusLabel.Text = statusText;
        _statusLabel.ForeColor = status switch
        {
            ProjectionStatus.Running => Color.Green,
            ProjectionStatus.Error => Color.Red,
            ProjectionStatus.Starting or ProjectionStatus.Stopping => Color.Orange,
            _ => Color.Black
        };
    }

    private void UpdateButtonStates()
    {
        bool isRunning = _projectionController?.IsRunning ?? false;
        _startButton.Enabled = !isRunning;
        _stopButton.Enabled = isRunning;
    }

    private void AddLogMessage(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] [{level}] {message}";
        
        _logListBox.Items.Add(logEntry);
        
        // Keep only last 200 entries
        while (_logListBox.Items.Count > 200)
        {
            _logListBox.Items.RemoveAt(0);
        }
        
        // Auto-scroll to bottom
        _logListBox.TopIndex = Math.Max(0, _logListBox.Items.Count - 1);
    }

    private void HideButton_Click(object? sender, EventArgs e)
    {
        ToggleLoaderVisibility();
    }

    private void ToggleLoaderVisibility()
    {
        if (_loaderVisible)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            _loaderVisible = false;
        }
        else
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
            _loaderVisible = true;
        }
    }

    private void ToggleProjection()
    {
        if (_projectionController == null) return;
        
        if (_projectionController.IsRunning)
        {
            StopProjection();
        }
        else
        {
            StartProjection();
        }
    }

    private void StopProjectionAndRestore()
    {
        StopProjection();
        
        if (!_loaderVisible)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.BringToFront();
            this.Activate();
            _loaderVisible = true;
        }
        else
        {
            this.BringToFront();
            this.Activate();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Clean up resources
        _projectionController?.Stop();
        _projectionController?.Dispose();
        _keybindManager?.Dispose();
        
        // Remove GUI log sink
        if (_guiLogSink != null)
        {
            Log.RemoveSink(_guiLogSink);
        }
        
        base.OnFormClosing(e);
    }

    #region Win32 API for Window Management
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;
    #endregion
}
}