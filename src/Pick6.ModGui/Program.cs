using Pick6.Core;
using Pick6.Projection;
using System.Drawing;
using System.Windows.Forms;

namespace Pick6.ModGui;

/// <summary>
/// Main entry point for Pick6 OBS-style game capture UI
/// </summary>
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Handle CLI arguments that should skip GUI
        if (args.Any(arg => arg.ToLower() == "--check-updates-only" || arg.ToLower() == "--help"))
        {
            // Don't start GUI for these arguments - delegate to console handler
            Environment.Exit(0);
        }

        try
        {
#if WINDOWS
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            var app = new FiveMCaptureApplication();
            Application.Run(app);
#else
            Console.WriteLine("FiveM capture GUI is only available on Windows");
            Environment.Exit(1);
#endif
        }
        catch (Exception ex)
        {
            Log.Error($"FiveM capture GUI error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}

#if WINDOWS
/// <summary>
/// Simple OBS-style game capture application for FiveM
/// </summary>
public class FiveMCaptureApplication : Form
{
    // Core capture components
    private GameCaptureEngine? _captureEngine;
    private BorderlessProjectionWindow? _projectionWindow;
    private System.Windows.Forms.Timer? _statusTimer;
    
    // UI Controls
    private Button? _startButton;
    private Button? _stopButton;
    private Label? _statusLabel;
    private Label? _processStatusLabel;
    private ListBox? _logListBox;
    private NumericUpDown? _fpsControl;
    private CheckBox? _autoProjectionCheckbox;
    
    // Status tracking
    private bool _isRunning = false;
    private readonly Queue<string> _recentLogs = new();
    private const int MAX_LOG_ENTRIES = 50;

    public FiveMCaptureApplication()
    {
        InitializeForm();
        InitializeControls();
        SetupStatusTimer();
        
        // Setup logging to show in UI
        Log.AddSink(new UiLogSink(this));
        Log.Info("FiveM Game Capture started - OBS-style interface");
    }

    private void InitializeForm()
    {
        Text = "FiveM Game Capture - Pick6";
        Size = new Size(600, 450);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        BackColor = Color.FromArgb(25, 25, 25);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        
        // Handle form closing
        FormClosing += OnFormClosing;
    }

    private void InitializeControls()
    {
        SuspendLayout();
        
        // Title label
        var titleLabel = new Label
        {
            Text = "FiveM Game Capture",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 149, 237), // Cornflower blue
            Location = new Point(20, 15),
            Size = new Size(200, 25)
        };
        Controls.Add(titleLabel);

        // Status section
        var statusGroupBox = new GroupBox
        {
            Text = "Status",
            ForeColor = Color.White,
            Location = new Point(20, 50),
            Size = new Size(550, 80),
            Font = new Font("Segoe UI", 9F)
        };
        Controls.Add(statusGroupBox);

        _statusLabel = new Label
        {
            Text = "Ready - Waiting for FiveM",
            ForeColor = Color.FromArgb(100, 149, 237),
            Location = new Point(10, 25),
            Size = new Size(300, 20),
            Font = new Font("Segoe UI", 9F)
        };
        statusGroupBox.Controls.Add(_statusLabel);

        _processStatusLabel = new Label
        {
            Text = "No FiveM processes detected",
            ForeColor = Color.Gray,
            Location = new Point(10, 45),
            Size = new Size(300, 20),
            Font = new Font("Segoe UI", 8.25F)
        };
        statusGroupBox.Controls.Add(_processStatusLabel);

        // Controls section
        var controlsGroupBox = new GroupBox
        {
            Text = "Capture Controls",
            ForeColor = Color.White,
            Location = new Point(20, 140),
            Size = new Size(550, 80),
            Font = new Font("Segoe UI", 9F)
        };
        Controls.Add(controlsGroupBox);

        _startButton = new Button
        {
            Text = "Start Game Capture",
            BackColor = Color.FromArgb(0, 120, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(10, 25),
            Size = new Size(140, 35),
            Font = new Font("Segoe UI", 9F),
            Enabled = true
        };
        _startButton.FlatAppearance.BorderSize = 0;
        _startButton.Click += StartCapture_Click;
        controlsGroupBox.Controls.Add(_startButton);

        _stopButton = new Button
        {
            Text = "Stop Capture",
            BackColor = Color.FromArgb(120, 30, 30),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(160, 25),
            Size = new Size(120, 35),
            Font = new Font("Segoe UI", 9F),
            Enabled = false
        };
        _stopButton.FlatAppearance.BorderSize = 0;
        _stopButton.Click += StopCapture_Click;
        controlsGroupBox.Controls.Add(_stopButton);

        // Settings
        var fpsLabel = new Label
        {
            Text = "FPS:",
            ForeColor = Color.White,
            Location = new Point(300, 28),
            Size = new Size(35, 20),
            Font = new Font("Segoe UI", 9F)
        };
        controlsGroupBox.Controls.Add(fpsLabel);

        _fpsControl = new NumericUpDown
        {
            Minimum = 15,
            Maximum = 120,
            Value = 60,
            Location = new Point(340, 25),
            Size = new Size(60, 23),
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White
        };
        controlsGroupBox.Controls.Add(_fpsControl);

        _autoProjectionCheckbox = new CheckBox
        {
            Text = "Auto-start projection",
            ForeColor = Color.White,
            Location = new Point(420, 28),
            Size = new Size(120, 20),
            Font = new Font("Segoe UI", 8.25F),
            Checked = true
        };
        controlsGroupBox.Controls.Add(_autoProjectionCheckbox);

        // Log section
        var logLabel = new Label
        {
            Text = "Activity Log:",
            ForeColor = Color.White,
            Location = new Point(20, 230),
            Size = new Size(100, 20),
            Font = new Font("Segoe UI", 9F)
        };
        Controls.Add(logLabel);

        _logListBox = new ListBox
        {
            Location = new Point(20, 255),
            Size = new Size(550, 150),
            BackColor = Color.FromArgb(35, 35, 35),
            ForeColor = Color.LightGray,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 8.25F),
            SelectionMode = SelectionMode.None
        };
        Controls.Add(_logListBox);

        ResumeLayout(false);
    }

    private void SetupStatusTimer()
    {
        _statusTimer = new System.Windows.Forms.Timer { Interval = 2000 }; // Every 2 seconds
        _statusTimer.Tick += UpdateStatus;
        _statusTimer.Start();
    }

    private void UpdateStatus(object? sender, EventArgs e)
    {
        try
        {
            // Check for FiveM processes
            var fiveMProcesses = FiveMDetector.FindFiveMProcesses();
            var processCount = fiveMProcesses.Count;

            if (_processStatusLabel != null)
            {
                if (processCount > 0)
                {
                    _processStatusLabel.Text = $"FiveM detected: {processCount} process(es)";
                    _processStatusLabel.ForeColor = Color.FromArgb(144, 238, 144); // Light green
                }
                else
                {
                    _processStatusLabel.Text = "No FiveM processes detected";
                    _processStatusLabel.ForeColor = Color.Gray;
                }
            }

            // Update capture status
            if (_statusLabel != null)
            {
                if (_isRunning)
                {
                    var stats = _captureEngine?.Statistics;
                    if (stats != null && stats.TotalFrames > 0)
                    {
                        _statusLabel.Text = $"Capturing - {stats.AverageFPS:F1} FPS";
                        _statusLabel.ForeColor = Color.FromArgb(144, 238, 144); // Light green
                    }
                    else
                    {
                        _statusLabel.Text = "Starting capture...";
                        _statusLabel.ForeColor = Color.Orange;
                    }
                }
                else
                {
                    _statusLabel.Text = processCount > 0 ? "Ready - FiveM detected" : "Ready - Waiting for FiveM";
                    _statusLabel.ForeColor = Color.FromArgb(100, 149, 237);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Status update error: {ex.Message}");
        }
    }

    private async void StartCapture_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_startButton != null) _startButton.Enabled = false;
            if (_statusLabel != null) _statusLabel.Text = "Starting capture...";

            await Task.Run(() =>
            {
                // Find FiveM process
                var fiveMProcesses = FiveMDetector.FindFiveMProcesses();
                if (fiveMProcesses.Count == 0)
                {
                    throw new InvalidOperationException("No FiveM processes found. Please start FiveM first.");
                }

                var targetProcess = fiveMProcesses.First();
                Log.Info($"Starting capture for FiveM process: {targetProcess.ProcessName}");

                // Create capture engine
                _captureEngine = new GameCaptureEngine();
                _captureEngine.Settings.TargetFPS = (int)(_fpsControl?.Value ?? 60);
                _captureEngine.ErrorOccurred += (s, msg) => Log.Error($"Capture error: {msg}");
                
                // Start capture
                if (!_captureEngine.StartCapture(targetProcess.ProcessName))
                {
                    throw new InvalidOperationException("Failed to start game capture. Try running as administrator.");
                }

                // Start projection if requested
                if (_autoProjectionCheckbox?.Checked == true)
                {
                    _projectionWindow = new BorderlessProjectionWindow();
                    _captureEngine.FrameCaptured += (s, e) => _projectionWindow?.UpdateFrame(e.Frame);
                    _projectionWindow.Show();
                }

                _isRunning = true;
                Log.Info("Game capture started successfully");
            });

            if (_stopButton != null) _stopButton.Enabled = true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start capture: {ex.Message}");
            if (_startButton != null) _startButton.Enabled = true;
            if (_statusLabel != null) 
            {
                _statusLabel.Text = "Error - Check log";
                _statusLabel.ForeColor = Color.Red;
            }
        }
    }

    private void StopCapture_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_stopButton != null) _stopButton.Enabled = false;
            
            _captureEngine?.StopCapture();
            _projectionWindow?.Close();
            
            _captureEngine = null;
            _projectionWindow = null;
            _isRunning = false;
            
            if (_startButton != null) _startButton.Enabled = true;
            Log.Info("Game capture stopped");
        }
        catch (Exception ex)
        {
            Log.Error($"Error stopping capture: {ex.Message}");
        }
    }

    public void AddLogEntry(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(AddLogEntry), message);
            return;
        }

        if (_logListBox == null) return;

        _recentLogs.Enqueue($"{DateTime.Now:HH:mm:ss} {message}");
        if (_recentLogs.Count > MAX_LOG_ENTRIES)
        {
            _recentLogs.Dequeue();
        }

        _logListBox.Items.Clear();
        foreach (var log in _recentLogs)
        {
            _logListBox.Items.Add(log);
        }

        // Auto-scroll to bottom
        if (_logListBox.Items.Count > 0)
        {
            _logListBox.TopIndex = _logListBox.Items.Count - 1;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _statusTimer?.Stop();
        _captureEngine?.StopCapture();
        _projectionWindow?.Close();
    }

    /// <summary>
    /// Log sink that forwards messages to the UI
    /// </summary>
    private class UiLogSink : ILogSink
    {
        private readonly FiveMCaptureApplication _app;

        public UiLogSink(FiveMCaptureApplication app)
        {
            _app = app;
        }

        public void WriteLog(LogLevel level, string message)
        {
            var prefix = level switch
            {
                LogLevel.Error => "[ERROR]",
                LogLevel.Warning => "[WARN]",
                LogLevel.Info => "[INFO]",
                LogLevel.Debug => "[DEBUG]",
                _ => "[LOG]"
            };
            
            _app.AddLogEntry($"{prefix} {message}");
        }
    }
}
#endif
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