using Pick6.Core;
using Pick6.Core.Util;
using Pick6.Projection;
using System.ComponentModel;

namespace Pick6.GUI;

/// <summary>
/// Main GUI form for Pick6 - OBS-style game capture interface
/// </summary>
public partial class MainForm : Form
{
    private GameCaptureEngine? _captureEngine;
    private BorderlessProjectionWindow? _projectionWindow;
    private System.Timers.Timer? _processMonitorTimer;
    private bool _isInjectionPrepped = false;
    private bool _isMonitoring = false;
    private bool _isCapturing = false;

    // Spinner for GUI animation during monitoring
    private System.Windows.Forms.Timer? _spinnerTimer;
    private int _spinnerFrame = 0;

    // UI Controls
    private Button _injectButton = null!;
    private Button _stopButton = null!;
    private Label _statusLabel = null!;
    private Label _processStatusLabel = null!;
    private Label _captureStatusLabel = null!;
    private CheckBox _autoProjectCheckbox = null!;
    private NumericUpDown _fpsNumeric = null!;
    private Panel _settingsPanel = null!;
    private GroupBox _statusGroup = null!;
    private GroupBox _settingsGroup = null!;

    public MainForm()
    {
        InitializeComponent();
        InitializeEngines();
        SetupEventHandlers();
        UpdateUI();
    }

    private void InitializeComponent()
    {
        Text = "pick6";
        Size = new Size(480, 400);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Application;

        // Main inject button - prominent like OBS
        _injectButton = new Button
        {
            Text = "Start Injection",
            Size = new Size(120, 40),
            Location = new Point(20, 20),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 215), // Windows blue
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _injectButton.FlatAppearance.BorderSize = 0;
        _injectButton.Click += InjectButton_Click;

        // Stop button
        _stopButton = new Button
        {
            Text = "Stop",
            Size = new Size(80, 40),
            Location = new Point(150, 20),
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(196, 43, 28), // Red
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        _stopButton.FlatAppearance.BorderSize = 0;
        _stopButton.Click += StopButton_Click;

        // Status display group
        _statusGroup = new GroupBox
        {
            Text = "Status",
            Location = new Point(20, 80),
            Size = new Size(430, 120),
            Font = new Font("Segoe UI", 9)
        };

        _statusLabel = new Label
        {
            Text = "Ready to inject",
            Location = new Point(10, 25),
            Size = new Size(400, 20),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215)
        };

        _processStatusLabel = new Label
        {
            Text = "FiveM Status: Not detected",
            Location = new Point(10, 50),
            Size = new Size(400, 20),
            Font = new Font("Segoe UI", 9)
        };

        _captureStatusLabel = new Label
        {
            Text = "Capture Status: Inactive",
            Location = new Point(10, 75),
            Size = new Size(400, 20),
            Font = new Font("Segoe UI", 9)
        };

        // Settings group
        _settingsGroup = new GroupBox
        {
            Text = "Settings",
            Location = new Point(20, 220),
            Size = new Size(430, 120),
            Font = new Font("Segoe UI", 9)
        };

        _autoProjectCheckbox = new CheckBox
        {
            Text = "Auto-start projection window",
            Location = new Point(15, 25),
            Size = new Size(200, 20),
            Checked = true,
            Font = new Font("Segoe UI", 9)
        };

        var fpsLabel = new Label
        {
            Text = "Target FPS:",
            Location = new Point(15, 55),
            Size = new Size(80, 20),
            Font = new Font("Segoe UI", 9)
        };

        _fpsNumeric = new NumericUpDown
        {
            Location = new Point(100, 53),
            Size = new Size(60, 20),
            Minimum = 15,
            Maximum = 120,
            Value = 60,
            Font = new Font("Segoe UI", 9)
        };

        // Add controls to groups
        _statusGroup.Controls.AddRange(new Control[] { _statusLabel, _processStatusLabel, _captureStatusLabel });
        _settingsGroup.Controls.AddRange(new Control[] { _autoProjectCheckbox, fpsLabel, _fpsNumeric });

        // Add all controls to form
        Controls.AddRange(new Control[] { _injectButton, _stopButton, _statusGroup, _settingsGroup });
    }

    private void InitializeEngines()
    {
        _captureEngine = new GameCaptureEngine();
        _projectionWindow = new BorderlessProjectionWindow();
    }

    private void SetupEventHandlers()
    {
        if (_captureEngine == null || _projectionWindow == null) return;

        // Forward captured frames to projection window
        _captureEngine.FrameCaptured += (s, e) =>
        {
            if (_autoProjectCheckbox.Checked)
            {
                _projectionWindow.UpdateFrame(e.Frame);
            }
        };

        // Handle capture errors
        _captureEngine.ErrorOccurred += (s, errorMessage) =>
        {
            BeginInvoke(() =>
            {
                _statusLabel.Text = $"Error: {errorMessage}";
                _statusLabel.ForeColor = Color.Red;
            });
        };

        // Handle projection events
        _projectionWindow.ProjectionStarted += (s, e) =>
        {
            BeginInvoke(() =>
            {
                _statusLabel.Text = "Capturing and projecting";
            });
        };

        _projectionWindow.ProjectionStopped += (s, e) =>
        {
            BeginInvoke(() =>
            {
                UpdateUI();
            });
        };

        // Settings change handlers
        _fpsNumeric.ValueChanged += (s, e) =>
        {
            if (_captureEngine != null)
                _captureEngine.Settings.TargetFPS = (int)_fpsNumeric.Value;
        };
    }

    private void InjectButton_Click(object? sender, EventArgs e)
    {
        if (!_isInjectionPrepped)
        {
            StartInjectionPrep();
        }
    }

    private void StopButton_Click(object? sender, EventArgs e)
    {
        StopInjection();
    }

    private void StartInjectionPrep()
    {
        _isInjectionPrepped = true;
        _isMonitoring = true;

        // Update UI
        _injectButton.Enabled = false;
        _stopButton.Enabled = true;
        _statusLabel.Text = "Monitoring for FiveM...";
        _statusLabel.ForeColor = Color.Orange;

        // Start spinner animation
        StartSpinner();

        // Start monitoring for FiveM processes
        _processMonitorTimer = new System.Timers.Timer(1000); // Check every second
        _processMonitorTimer.Elapsed += ProcessMonitorTimer_Elapsed;
        _processMonitorTimer.Start();

        // Check immediately if FiveM is already running
        CheckForFiveMAndInject();
    }

    private void ProcessMonitorTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!_isMonitoring) return;

        CheckForFiveMAndInject();
    }

    private void CheckForFiveMAndInject()
    {
        if (_isCapturing) return; // Already capturing

        var summary = FiveMDetector.GetProcessSummary();
        
        BeginInvoke(() =>
        {
            if (summary.TotalProcessCount > 0)
            {
                _processStatusLabel.Text = $"FiveM Status: Found {summary.TotalProcessCount} process(es)";
                _processStatusLabel.ForeColor = Color.Green;

                if (!_isCapturing)
                {
                    AttemptInjection(summary);
                }
            }
            else
            {
                _processStatusLabel.Text = "FiveM Status: Waiting for FiveM to start...";
                _processStatusLabel.ForeColor = Color.Orange;
            }
        });
    }

    private void AttemptInjection(FiveMProcessSummary summary)
    {
        if (_captureEngine == null) return;

        ProcessInfo? targetProcess = null;
        string method = "";

        // Prioritize Vulkan processes like OBS would
        if (summary.VulkanProcesses.Any())
        {
            var vulkanProcess = summary.VulkanProcesses.First();
            targetProcess = new ProcessInfo
            {
                ProcessId = vulkanProcess.ProcessId,
                ProcessName = vulkanProcess.ProcessName,
                WindowTitle = vulkanProcess.WindowTitle,
                WindowHandle = vulkanProcess.WindowHandle
            };
            method = "Vulkan injection";
        }
        else if (summary.TraditionalProcesses.Any())
        {
            targetProcess = summary.TraditionalProcesses.First();
            method = "Window capture";
        }

        if (targetProcess == null) return;

        _statusLabel.Text = $"Injecting into {targetProcess.ProcessName}...";
        _statusLabel.ForeColor = Color.Orange;

        // Apply current settings
        _captureEngine.Settings.TargetFPS = (int)_fpsNumeric.Value;

        if (_captureEngine.StartCapture(targetProcess.ProcessName))
        {
            _isCapturing = true;
            StopSpinner();
            _statusLabel.Text = $"{TextGlyphs.Success} Successfully injected - {method}";
            _statusLabel.ForeColor = Color.Green;
            _captureStatusLabel.Text = $"Capture Status: Active ({method})";
            _captureStatusLabel.ForeColor = Color.Green;

            // Auto-start projection if enabled
            if (_autoProjectCheckbox.Checked)
            {
                _projectionWindow?.StartProjection();
            }
        }
        else
        {
            StopSpinner();
            _statusLabel.Text = $"{TextGlyphs.Fail} Injection failed - try running as administrator";
            _statusLabel.ForeColor = Color.Red;
            _captureStatusLabel.Text = "Capture Status: Failed";
            _captureStatusLabel.ForeColor = Color.Red;
        }
    }

    private void StopInjection()
    {
        _isInjectionPrepped = false;
        _isMonitoring = false;
        _isCapturing = false;

        _processMonitorTimer?.Stop();
        _processMonitorTimer?.Dispose();
        _processMonitorTimer = null;

        // Stop spinner animation
        StopSpinner();

        _captureEngine?.StopCapture();
        _projectionWindow?.StopProjection();

        UpdateUI();
    }

    private void UpdateUI()
    {
        _injectButton.Enabled = !_isInjectionPrepped;
        _injectButton.Text = _isInjectionPrepped ? "Monitoring..." : "Start Injection";
        _stopButton.Enabled = _isInjectionPrepped;

        if (!_isInjectionPrepped)
        {
            _statusLabel.Text = "Ready to inject";
            _statusLabel.ForeColor = Color.FromArgb(0, 120, 215);
            _processStatusLabel.Text = "FiveM Status: Not monitored";
            _processStatusLabel.ForeColor = Color.Gray;
            _captureStatusLabel.Text = "Capture Status: Inactive";
            _captureStatusLabel.ForeColor = Color.Gray;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopInjection();
        base.OnFormClosing(e);
    }

    /// <summary>
    /// Start the spinner animation for GUI monitoring
    /// </summary>
    private void StartSpinner()
    {
        if (_spinnerTimer != null) return;

        _spinnerFrame = 0;
        _spinnerTimer = new System.Windows.Forms.Timer { Interval = 90 }; // 90ms interval
        _spinnerTimer.Tick += SpinnerTimer_Tick;
        _spinnerTimer.Start();
    }

    /// <summary>
    /// Stop the spinner animation
    /// </summary>
    private void StopSpinner()
    {
        if (_spinnerTimer == null) return;

        _spinnerTimer.Stop();
        _spinnerTimer.Dispose();
        _spinnerTimer = null;
    }

    /// <summary>
    /// Handle spinner timer tick to animate the monitoring message
    /// </summary>
    private void SpinnerTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isMonitoring || _isCapturing)
        {
            StopSpinner();
            return;
        }

        var frame = TextGlyphs.SpinnerFrames[_spinnerFrame % TextGlyphs.SpinnerFrames.Length];
        _statusLabel.Text = $"{frame} Monitoring for FiveM...";
        _spinnerFrame++;
    }
}