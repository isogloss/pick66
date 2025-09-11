using Pick6.Core;
using Pick6.Projection;
using ImGuiNET;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Pick6.ModGui;

/// <summary>
/// Main entry point for Pick6 ImGui mod menu
/// </summary>
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Handle CLI arguments that should skip GUI
        if (args.Any(arg => arg.ToLower() == "--check-updates-only" || arg.ToLower() == "--help"))
        {
            // Don't start GUI for these arguments
            Environment.Exit(0);
        }

        try
        {
#if WINDOWS
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            var app = new ModMenuApplication();
            Application.Run(app);
#else
            Console.WriteLine("ImGui mod menu is only available on Windows");
            Environment.Exit(1);
#endif
        }
        catch (Exception ex)
        {
            Log.Error($"ImGui mod menu error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}

#if WINDOWS
/// <summary>
/// Windows Forms host for ImGui-style mod menu with tabs
/// </summary>
public class ModMenuApplication : Form
{
    // Core references
    private GameCaptureEngine? _captureEngine;
    private BorderlessProjectionWindow? _projectionWindow;
    private System.Windows.Forms.Timer? _updateTimer;
    
    // UI Controls
    private TabControl? _tabControl;
    private Panel? _loaderPanel;
    private Panel? _settingsPanel;
    private ListBox? _logListBox;
    private Label? _statusLabel;
    private Label? _fpsLabel;
    
    // Loader tab controls
    private Button? _startCaptureButton;
    private Button? _stopCaptureButton;
    private Button? _startProjectionButton;
    private Button? _stopProjectionButton;
    
    // Settings tab controls
    private NumericUpDown? _fpsNumeric;
    private NumericUpDown? _widthNumeric;
    private NumericUpDown? _heightNumeric;
    private CheckBox? _hwAccelCheckbox;
    private CheckBox? _autoStartCheckbox;
    private TrackBar? _uiScaleTracker;
    private NumericUpDown? _monitorNumeric;
    private Button? _applyButton;
    private Button? _saveButton;

    public ModMenuApplication()
    {
        InitializeForm();
        InitializeControls();
        InitializeCoreServices();
        SetupUpdateTimer();
        LoadSettings();
        
        // Setup log sink
        Log.AddSink(new ImGuiLogSink());
        Log.Info("Pick6 Mod Menu started");
    }

    private void InitializeForm()
    {
        Text = "Pick6 Mod Menu";
        Size = new Size(800, 600);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(600, 400);
        BackColor = Color.FromArgb(40, 40, 40);
        ForeColor = Color.White;
    }

    private void InitializeControls()
    {
        // Main TabControl
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White
        };
        Controls.Add(_tabControl);

        // Loader Tab
        var loaderTab = new TabPage("Loader")
        {
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White
        };
        _loaderPanel = new Panel { Dock = DockStyle.Fill };
        loaderTab.Controls.Add(_loaderPanel);
        _tabControl.TabPages.Add(loaderTab);

        InitializeLoaderTab();

        // Settings Tab
        var settingsTab = new TabPage("Settings")
        {
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White
        };
        _settingsPanel = new Panel { Dock = DockStyle.Fill };
        settingsTab.Controls.Add(_settingsPanel);
        _tabControl.TabPages.Add(settingsTab);

        InitializeSettingsTab();
    }

    private void InitializeLoaderTab()
    {
        if (_loaderPanel == null) return;

        int y = 20;
        const int rowHeight = 35;
        const int buttonWidth = 120;
        const int buttonHeight = 30;

        // Status section
        _statusLabel = new Label
        {
            Text = "Status: Idle",
            Location = new Point(20, y),
            Size = new Size(300, 20),
            ForeColor = Color.White
        };
        _loaderPanel.Controls.Add(_statusLabel);
        y += 25;

        _fpsLabel = new Label
        {
            Text = "FPS: 0.0 | Dropped: 0",
            Location = new Point(20, y),
            Size = new Size(300, 20),
            ForeColor = Color.LightGray
        };
        _loaderPanel.Controls.Add(_fpsLabel);
        y += rowHeight;

        // Control buttons
        _startCaptureButton = new Button
        {
            Text = "Start Capture",
            Location = new Point(20, y),
            Size = new Size(buttonWidth, buttonHeight),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _startCaptureButton.Click += (s, e) => StartCapture();
        _loaderPanel.Controls.Add(_startCaptureButton);

        _stopCaptureButton = new Button
        {
            Text = "Stop Capture",
            Location = new Point(150, y),
            Size = new Size(buttonWidth, buttonHeight),
            BackColor = Color.Red,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _stopCaptureButton.Click += (s, e) => StopCapture();
        _loaderPanel.Controls.Add(_stopCaptureButton);
        y += rowHeight + 10;

        _startProjectionButton = new Button
        {
            Text = "Start Projection",
            Location = new Point(20, y),
            Size = new Size(buttonWidth, buttonHeight),
            BackColor = Color.Blue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _startProjectionButton.Click += (s, e) => StartProjection();
        _loaderPanel.Controls.Add(_startProjectionButton);

        _stopProjectionButton = new Button
        {
            Text = "Stop Projection",
            Location = new Point(150, y),
            Size = new Size(buttonWidth, buttonHeight),
            BackColor = Color.DarkBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _stopProjectionButton.Click += (s, e) => StopProjection();
        _loaderPanel.Controls.Add(_stopProjectionButton);
        y += rowHeight + 20;

        // Log console
        var logLabel = new Label
        {
            Text = "Console Log:",
            Location = new Point(20, y),
            Size = new Size(100, 20),
            ForeColor = Color.White
        };
        _loaderPanel.Controls.Add(logLabel);
        y += 25;

        _logListBox = new ListBox
        {
            Location = new Point(20, y),
            Size = new Size(740, 200),
            BackColor = Color.Black,
            ForeColor = Color.LightGray,
            Font = new Font("Consolas", 9)
        };
        _loaderPanel.Controls.Add(_logListBox);

        // Subscribe to log updates
        GuiState.Instance.LogUpdated += OnLogUpdated;
    }

    private void InitializeSettingsTab()
    {
        if (_settingsPanel == null) return;

        int y = 20;
        const int rowHeight = 35;
        const int labelWidth = 150;
        const int controlWidth = 100;

        // FPS setting
        var fpsLabel = new Label
        {
            Text = "Target FPS:",
            Location = new Point(20, y),
            Size = new Size(labelWidth, 20),
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(fpsLabel);

        _fpsNumeric = new NumericUpDown
        {
            Location = new Point(180, y),
            Size = new Size(controlWidth, 20),
            Minimum = 1,
            Maximum = 120,
            Value = 60,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(_fpsNumeric);
        y += rowHeight;

        // Width setting
        var widthLabel = new Label
        {
            Text = "Width (0=auto):",
            Location = new Point(20, y),
            Size = new Size(labelWidth, 20),
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(widthLabel);

        _widthNumeric = new NumericUpDown
        {
            Location = new Point(180, y),
            Size = new Size(controlWidth, 20),
            Maximum = 4000,
            Value = 0,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(_widthNumeric);
        y += rowHeight;

        // Height setting
        var heightLabel = new Label
        {
            Text = "Height (0=auto):",
            Location = new Point(20, y),
            Size = new Size(labelWidth, 20),
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(heightLabel);

        _heightNumeric = new NumericUpDown
        {
            Location = new Point(180, y),
            Size = new Size(controlWidth, 20),
            Maximum = 4000,
            Value = 0,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(_heightNumeric);
        y += rowHeight;

        // Hardware acceleration
        _hwAccelCheckbox = new CheckBox
        {
            Text = "Hardware Acceleration",
            Location = new Point(20, y),
            Size = new Size(200, 20),
            Checked = true,
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(_hwAccelCheckbox);
        y += rowHeight;

        // Auto-start
        _autoStartCheckbox = new CheckBox
        {
            Text = "Auto-start Projection",
            Location = new Point(20, y),
            Size = new Size(200, 20),
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(_autoStartCheckbox);
        y += rowHeight;

        // UI Scale
        var scaleLabel = new Label
        {
            Text = "UI Scale:",
            Location = new Point(20, y),
            Size = new Size(labelWidth, 20),
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(scaleLabel);

        _uiScaleTracker = new TrackBar
        {
            Location = new Point(180, y),
            Size = new Size(200, 45),
            Minimum = 5,
            Maximum = 30,
            Value = 10,
            TickFrequency = 5,
            BackColor = Color.FromArgb(60, 60, 60)
        };
        _settingsPanel.Controls.Add(_uiScaleTracker);
        y += 50;

        // Monitor
        var monitorLabel = new Label
        {
            Text = "Monitor Index:",
            Location = new Point(20, y),
            Size = new Size(labelWidth, 20),
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(monitorLabel);

        _monitorNumeric = new NumericUpDown
        {
            Location = new Point(180, y),
            Size = new Size(controlWidth, 20),
            Maximum = 10,
            Value = 0,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        _settingsPanel.Controls.Add(_monitorNumeric);
        y += rowHeight + 20;

        // Buttons
        _applyButton = new Button
        {
            Text = "Apply Settings",
            Size = new Size(120, 30),
            Location = new Point(20, y),
            BackColor = Color.Orange,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _applyButton.Click += (s, e) => ApplySettings();
        _settingsPanel.Controls.Add(_applyButton);

        _saveButton = new Button
        {
            Text = "Save Settings",
            Size = new Size(120, 30),
            Location = new Point(150, y),
            BackColor = Color.Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _saveButton.Click += (s, e) => SaveSettings();
        _settingsPanel.Controls.Add(_saveButton);
    }

    private void InitializeCoreServices()
    {
        _captureEngine = new GameCaptureEngine();
        _projectionWindow = new BorderlessProjectionWindow();

        // Store references in GuiState
        var guiState = GuiState.Instance;
        guiState.CaptureEngine = _captureEngine;
        guiState.ProjectionWindow = _projectionWindow;

        // Setup event handlers for status updates
        _captureEngine.FrameCaptured += (s, e) =>
        {
            _projectionWindow.UpdateFrame(e.Frame);
        };

        _captureEngine.ErrorOccurred += (s, errorMessage) =>
        {
            Log.Error($"Capture error: {errorMessage}");
            guiState.CurrentStatus = "Error";
        };

        // Update projection window events to update GUI state
        _projectionWindow.ProjectionStarted += (s, e) =>
        {
            Log.Info("Projection started");
            guiState.IsProjecting = true;
            guiState.CurrentStatus = "Projecting";
        };

        _projectionWindow.ProjectionStopped += (s, e) =>
        {
            Log.Info("Projection stopped");
            guiState.IsProjecting = false;
            if (guiState.IsCapturing)
                guiState.CurrentStatus = "Capturing";
            else
                guiState.CurrentStatus = "Idle";
        };
    }

    private void SetupUpdateTimer()
    {
        _updateTimer = new System.Windows.Forms.Timer();
        _updateTimer.Interval = 16; // ~60fps
        _updateTimer.Tick += (s, e) => UpdatePerformanceMetrics();
        _updateTimer.Start();
    }

    private void UpdatePerformanceMetrics()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdatePerformanceMetrics));
            return;
        }
        
        var guiState = GuiState.Instance;
        
        // Update performance metrics
        if (_captureEngine?.Statistics != null)
        {
            guiState.CurrentFPS = (float)_captureEngine.Statistics.FramesPerSecond;
            guiState.DroppedFrames = (int)_captureEngine.Statistics.DroppedFrames;
        }
        
        // Update UI labels
        if (_statusLabel != null)
        {
            _statusLabel.Text = $"Status: {guiState.CurrentStatus}";
            _statusLabel.ForeColor = GetStatusColor(guiState.CurrentStatus);
        }
        
        if (_fpsLabel != null)
        {
            _fpsLabel.Text = $"FPS: {guiState.CurrentFPS:F1} | Dropped: {guiState.DroppedFrames}";
        }
    }

    private void LoadSettings()
    {
        var settings = GuiState.Instance.CurrentSettings;
        
        if (_fpsNumeric != null) _fpsNumeric.Value = settings.TargetFPS;
        if (_widthNumeric != null) _widthNumeric.Value = settings.ResolutionWidth;
        if (_heightNumeric != null) _heightNumeric.Value = settings.ResolutionHeight;
        if (_hwAccelCheckbox != null) _hwAccelCheckbox.Checked = settings.HardwareAcceleration;
        if (_autoStartCheckbox != null) _autoStartCheckbox.Checked = settings.AutoStartProjection;
        if (_uiScaleTracker != null) _uiScaleTracker.Value = (int)(settings.UiScale * 10);
        if (_monitorNumeric != null) _monitorNumeric.Value = settings.MonitorIndex;
    }

    private void SaveSettings()
    {
        var settings = GuiState.Instance.CurrentSettings;
        
        if (_fpsNumeric != null) settings.TargetFPS = (int)_fpsNumeric.Value;
        if (_widthNumeric != null) settings.ResolutionWidth = (int)_widthNumeric.Value;
        if (_heightNumeric != null) settings.ResolutionHeight = (int)_heightNumeric.Value;
        if (_hwAccelCheckbox != null) settings.HardwareAcceleration = _hwAccelCheckbox.Checked;
        if (_autoStartCheckbox != null) settings.AutoStartProjection = _autoStartCheckbox.Checked;
        if (_uiScaleTracker != null) settings.UiScale = _uiScaleTracker.Value / 10.0f;
        if (_monitorNumeric != null) settings.MonitorIndex = (int)_monitorNumeric.Value;
        
        settings.Validate();
        GuiState.Instance.SaveSettings();
        
        Log.Info("Settings saved");
        ApplySettings(); // Also apply them
    }

    private void OnLogUpdated(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new EventHandler(OnLogUpdated), sender, e);
            return;
        }
        
        if (_logListBox == null) return;
        
        // Add recent log entries to the list
        var logs = GuiState.Instance.GetLogEntries().TakeLast(100);
        _logListBox.Items.Clear();
        
        foreach (var log in logs)
        {
            var logText = $"[{log.Timestamp:HH:mm:ss}] [{log.Level}] {log.Message}";
            _logListBox.Items.Add(logText);
        }
        
        // Auto-scroll to bottom
        if (_logListBox.Items.Count > 0)
        {
            _logListBox.TopIndex = _logListBox.Items.Count - 1;
        }
    }

    private Color GetStatusColor(string status)
    {
        return status.ToLower() switch
        {
            "idle" => Color.Gray,
            "capturing" => Color.Green,
            "projecting" => Color.Blue,
            "error" => Color.Red,
            _ => Color.Orange
        };
    }

    private void StartCapture()
    {
        if (_captureEngine == null) return;
        
        var guiState = GuiState.Instance;
        
        // Apply current settings to capture engine
        _captureEngine.Settings.TargetFPS = guiState.CurrentSettings.TargetFPS;
        _captureEngine.Settings.ScaleWidth = guiState.CurrentSettings.ResolutionWidth;
        _captureEngine.Settings.ScaleHeight = guiState.CurrentSettings.ResolutionHeight;
        _captureEngine.Settings.UseHardwareAcceleration = guiState.CurrentSettings.HardwareAcceleration;

        // Find FiveM process
        var summary = FiveMDetector.GetProcessSummary();
        if (summary.TotalProcessCount == 0)
        {
            Log.Error("No FiveM processes found. Please start FiveM first.");
            return;
        }

        // Try to start capture
        string processName = "";
        if (summary.VulkanProcesses.Any())
        {
            processName = summary.VulkanProcesses.First().ProcessName;
            Log.Info($"Attempting Vulkan capture on {processName}");
        }
        else if (summary.TraditionalProcesses.Any())
        {
            processName = summary.TraditionalProcesses.First().ProcessName;
            Log.Info($"Attempting GDI capture on {processName}");
        }

        if (_captureEngine.StartCapture(processName))
        {
            Log.Info("Capture started successfully");
            guiState.IsCapturing = true;
            guiState.CurrentStatus = "Capturing";
            
            // Auto-start projection if enabled
            if (guiState.CurrentSettings.AutoStartProjection && !guiState.IsProjecting)
            {
                StartProjection();
            }
        }
        else
        {
            Log.Error("Failed to start capture");
            guiState.CurrentStatus = "Error";
        }
    }

    private void StopCapture()
    {
        if (_captureEngine == null) return;
        
        _captureEngine.StopCapture();
        
        var guiState = GuiState.Instance;
        guiState.IsCapturing = false;
        guiState.CurrentStatus = guiState.IsProjecting ? "Projecting" : "Idle";
        
        Log.Info("Capture stopped");
    }

    private void StartProjection()
    {
        if (_projectionWindow == null) return;
        
        var guiState = GuiState.Instance;
        _projectionWindow.StartProjection(guiState.CurrentSettings.MonitorIndex);
        
        Log.Info($"Starting projection on monitor {guiState.CurrentSettings.MonitorIndex}");
    }

    private void StopProjection()
    {
        if (_projectionWindow == null) return;
        
        _projectionWindow.StopProjection();
        Log.Info("Projection stopped");
    }

    private void ApplySettings()
    {
        var guiState = GuiState.Instance;
        var settings = guiState.CurrentSettings;
        
        // Apply settings to capture engine if running
        if (_captureEngine != null)
        {
            _captureEngine.Settings.TargetFPS = settings.TargetFPS;
            _captureEngine.Settings.ScaleWidth = settings.ResolutionWidth;
            _captureEngine.Settings.ScaleHeight = settings.ResolutionHeight;
            _captureEngine.Settings.UseHardwareAcceleration = settings.HardwareAcceleration;
        }
        
        // Apply to projection window
        if (_projectionWindow != null)
        {
            _projectionWindow.SetTargetFPS(settings.TargetFPS);
        }
        
        Log.Info("Settings applied");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        
        _captureEngine?.StopCapture();
        _projectionWindow?.StopProjection();
        
        // Unsubscribe from events
        GuiState.Instance.LogUpdated -= OnLogUpdated;
        
        Log.Info("Pick6 Mod Menu shutting down");
        GuiState.Instance.SaveSettings();
        
        base.OnFormClosed(e);
    }
}
#endif