using Pick6.Core;
using Pick6.Core.Util;
using Pick6.Projection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Pick6.Loader;

/// <summary>
/// Minimal black & white GUI form for Pick6 - displays header and spinner during injection
/// </summary>
public partial class MainForm : Form
{
    private GameCaptureEngine? _captureEngine;
    private BorderlessProjectionWindow? _projectionWindow;
    private GlobalKeybindManager? _keybindManager;
    private System.Timers.Timer? _processMonitorTimer;
    private bool _isInjectionActive = false;
    private bool _isMonitoring = false;
    private bool _isCapturing = false;
    private bool _loaderVisible = true;

    // Spinner animation
    private System.Windows.Forms.Timer? _spinnerTimer;
    private int _spinnerFrame = 0;

    // UI Controls
    private Label _headerLabel = null!;
    private Label _statusLabel = null!;
    private Button _hideButton = null!;

    // Braille spinner frames as specified in requirements
    private static readonly string[] BrailleSpinnerFrames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };

    public MainForm()
    {
        InitializeComponent();
        InitializeEngines();
        SetupEventHandlers();
        StartInjectionProcess();
    }

    private void InitializeComponent()
    {
        // Form setup - minimal black & white design as specified
        Text = "pick6";
        Size = new Size(300, 120);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;
        ForeColor = Color.White;
        Icon = SystemIcons.Application;

        // Large "pick6" header label
        _headerLabel = new Label
        {
            Text = "pick6",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Black,
            Location = new Point(10, 10),
            Size = new Size(280, 30),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Status label for spinner and messages
        _statusLabel = new Label
        {
            Text = "⠋ Waiting for injection",
            Font = new Font("Consolas", 10, FontStyle.Regular), // Monospace for spinner alignment
            ForeColor = Color.White,
            BackColor = Color.Black,
            Location = new Point(10, 45),
            Size = new Size(280, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Small hide button in corner (flat white border as specified)
        _hideButton = new Button
        {
            Text = "Hide",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.White,
            BackColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(225, 75),
            Size = new Size(50, 20)
        };
        _hideButton.FlatAppearance.BorderColor = Color.White;
        _hideButton.FlatAppearance.BorderSize = 1;
        _hideButton.Click += HideButton_Click;

        // Add controls to form
        Controls.AddRange(new Control[] { _headerLabel, _statusLabel, _hideButton });
    }

    private void InitializeEngines()
    {
        _captureEngine = new GameCaptureEngine();
        _projectionWindow = new BorderlessProjectionWindow();
        
        // Initialize global keybind manager for hotkeys
        _keybindManager = new GlobalKeybindManager();
        SetupGlobalKeybinds();
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

    private void StartInjectionProcess()
    {
        _isInjectionActive = true;
        _isMonitoring = true;
        
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
            if (summary.TotalProcessCount > 0 && !_isCapturing)
            {
                AttemptInjection(summary);
            }
        });
    }

    private void AttemptInjection(FiveMProcessSummary summary)
    {
        if (_captureEngine == null) return;

        ProcessInfo? targetProcess = null;
        string method = "";

        // Prioritize Vulkan processes
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

        if (_captureEngine.StartCapture(targetProcess.ProcessName))
        {
            _isCapturing = true;
            StopSpinner();
            _statusLabel.Text = "✓ Successfully injected";
            _statusLabel.ForeColor = Color.LimeGreen;

            // Auto-start projection
            _projectionWindow?.StartProjection(0); // Use primary monitor
        }
        else
        {
            StopSpinner();
            _statusLabel.Text = "✗ Injection failed";
            _statusLabel.ForeColor = Color.Red;
        }
    }

    private void SetupEventHandlers()
    {
        if (_captureEngine == null || _projectionWindow == null) return;

        // Forward captured frames to projection window
        _captureEngine.FrameCaptured += (s, e) =>
        {
            _projectionWindow.UpdateFrame(e.Frame);
        };

        // Handle capture errors
        _captureEngine.ErrorOccurred += (s, errorMessage) =>
        {
            BeginInvoke(() =>
            {
                StopSpinner();
                _statusLabel.Text = "✗ Injection failed";
                _statusLabel.ForeColor = Color.Red;
            });
        };

        // Handle projection events
        _projectionWindow.ProjectionStarted += (s, e) =>
        {
            BeginInvoke(() =>
            {
                Log.Info("Projection started");
            });
        };

        _projectionWindow.ProjectionStopped += (s, e) =>
        {
            BeginInvoke(() =>
            {
                Log.Info("Projection stopped");
            });
        };
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
        if (_projectionWindow != null && _isCapturing)
        {
            if (_projectionWindow.IsProjecting)
            {
                _projectionWindow.StopProjection();
            }
            else
            {
                _projectionWindow.StartProjection(0);
            }
        }
    }

    private void StopProjection()
    {
        _projectionWindow?.StopProjection();
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
        _isInjectionActive = false;
        _isMonitoring = false;
        _isCapturing = false;

        _processMonitorTimer?.Stop();
        _processMonitorTimer?.Dispose();
        _processMonitorTimer = null;

        StopSpinner();
        _captureEngine?.StopCapture();
        _projectionWindow?.StopProjection();
        _keybindManager?.Dispose();
        
        base.OnFormClosing(e);
    }

    /// <summary>
    /// Start the Braille spinner animation as specified in requirements
    /// </summary>
    private void StartSpinner()
    {
        if (_spinnerTimer != null) return;

        _spinnerFrame = 0;
        _spinnerTimer = new System.Windows.Forms.Timer { Interval = 100 }; // 100ms as specified
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
    /// Handle spinner timer tick - cycles through Braille spinner frames every 100ms
    /// </summary>
    private void SpinnerTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isMonitoring || _isCapturing)
        {
            StopSpinner();
            return;
        }

        var frame = BrailleSpinnerFrames[_spinnerFrame % BrailleSpinnerFrames.Length];
        _statusLabel.Text = $"{frame} Waiting for injection";
        _spinnerFrame++;
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