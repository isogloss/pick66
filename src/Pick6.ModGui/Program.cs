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
/// Windows Forms host for ImGui mod menu
/// </summary>
public class ModMenuApplication : Form
{
    // Core references
    private GameCaptureEngine? _captureEngine;
    private BorderlessProjectionWindow? _projectionWindow;
    private System.Windows.Forms.Timer? _updateTimer;

    public ModMenuApplication()
    {
        InitializeForm();
        InitializeCoreServices();
        SetupUpdateTimer();
        
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
        var guiState = GuiState.Instance;
        
        // Update performance metrics
        if (_captureEngine?.Statistics != null)
        {
            guiState.CurrentFPS = (float)_captureEngine.Statistics.FramesPerSecond;
            guiState.DroppedFrames = (int)_captureEngine.Statistics.DroppedFrames;
        }
        
        // Force refresh
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        RenderInterface(e.Graphics);
    }

    private void RenderInterface(Graphics g)
    {
        var guiState = GuiState.Instance;
        
        // Use simple Windows Forms controls drawn with Graphics for now
        // This is a simplified approach that will work with single-file deployment
        
        using var brush = new SolidBrush(Color.Black);
        g.FillRectangle(brush, ClientRectangle);
        
        using var textBrush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI", 10);
        
        int y = 10;
        const int lineHeight = 25;
        
        // Title
        using var titleFont = new Font("Segoe UI", 14, FontStyle.Bold);
        g.DrawString("Pick6 Mod Menu", titleFont, textBrush, 10, y);
        y += 40;
        
        // Status
        var statusColor = GetStatusColor(guiState.CurrentStatus);
        using var statusBrush = new SolidBrush(statusColor);
        g.DrawString($"Status: {guiState.CurrentStatus}", font, statusBrush, 10, y);
        y += lineHeight;
        
        // Performance
        g.DrawString($"FPS: {guiState.CurrentFPS:F1} | Dropped: {guiState.DroppedFrames}", font, textBrush, 10, y);
        y += lineHeight;
        
        // Settings preview
        y += 10;
        g.DrawString("Settings:", font, textBrush, 10, y);
        y += lineHeight;
        g.DrawString($"Target FPS: {guiState.CurrentSettings.TargetFPS}", font, textBrush, 20, y);
        y += lineHeight;
        g.DrawString($"Resolution: {(guiState.CurrentSettings.ResolutionWidth > 0 ? $"{guiState.CurrentSettings.ResolutionWidth}x{guiState.CurrentSettings.ResolutionHeight}" : "Auto")}", font, textBrush, 20, y);
        y += lineHeight;
        g.DrawString($"Hardware Acceleration: {(guiState.CurrentSettings.HardwareAcceleration ? "On" : "Off")}", font, textBrush, 20, y);
        y += lineHeight;
        g.DrawString($"Auto-start: {(guiState.CurrentSettings.AutoStartProjection ? "On" : "Off")}", font, textBrush, 20, y);
        y += lineHeight + 20;
        
        // Instructions
        g.DrawString("Right-click for menu options", font, textBrush, 10, y);
        y += lineHeight;
        
        // Recent logs
        y += 10;
        g.DrawString("Recent Logs:", font, textBrush, 10, y);
        y += lineHeight;
        
        var logs = guiState.GetLogEntries().TakeLast(10);
        foreach (var log in logs)
        {
            var logColor = GetLogColorForGraphics(log.Level);
            using var logBrush = new SolidBrush(logColor);
            var logText = $"[{log.Timestamp:HH:mm:ss}] [{log.Level}] {log.Message}";
            if (logText.Length > 80) logText = logText.Substring(0, 77) + "...";
            g.DrawString(logText, font, logBrush, 20, y);
            y += lineHeight;
            
            if (y > ClientSize.Height - 50) break;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            ShowContextMenu(e.Location);
        }
        base.OnMouseUp(e);
    }

    private void ShowContextMenu(Point location)
    {
        var menu = new ContextMenuStrip();
        
        menu.Items.Add("Start Capture", null, (s, e) => StartCapture());
        menu.Items.Add("Stop Capture", null, (s, e) => StopCapture());
        menu.Items.Add("-");
        menu.Items.Add("Start Projection", null, (s, e) => StartProjection());
        menu.Items.Add("Stop Projection", null, (s, e) => StopProjection());
        menu.Items.Add("-");
        menu.Items.Add("Settings...", null, (s, e) => ShowSettingsDialog());
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (s, e) => Close());
        
        menu.Show(this, location);
    }

    private void ShowSettingsDialog()
    {
        var settingsForm = new SettingsForm();
        if (settingsForm.ShowDialog(this) == DialogResult.OK)
        {
            ApplySettings();
            Invalidate();
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

    private Color GetLogColorForGraphics(string level)
    {
        return level.ToLower() switch
        {
            "error" => Color.Red,
            "warn" => Color.Orange,
            "info" => Color.LightGray,
            "debug" => Color.Gray,
            _ => Color.White
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
        
        Log.Info("Pick6 Mod Menu shutting down");
        GuiState.Instance.SaveSettings();
        
        base.OnFormClosed(e);
    }
}

/// <summary>
/// Settings dialog form
/// </summary>
public class SettingsForm : Form
{
    private NumericUpDown _fpsNumeric = null!;
    private NumericUpDown _widthNumeric = null!;
    private NumericUpDown _heightNumeric = null!;
    private CheckBox _hwAccelCheckbox = null!;
    private CheckBox _autoStartCheckbox = null!;
    private TrackBar _uiScaleTracker = null!;
    private NumericUpDown _monitorNumeric = null!;

    public SettingsForm()
    {
        InitializeForm();
        LoadCurrentSettings();
    }

    private void InitializeForm()
    {
        Text = "Settings";
        Size = new Size(400, 350);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var y = 20;
        const int rowHeight = 35;

        // FPS setting
        Controls.Add(new Label { Text = "Target FPS:", Location = new Point(20, y), Size = new Size(120, 20) });
        _fpsNumeric = new NumericUpDown { Location = new Point(150, y), Size = new Size(80, 20), Minimum = 1, Maximum = 120, Value = 60 };
        Controls.Add(_fpsNumeric);
        y += rowHeight;

        // Width setting
        Controls.Add(new Label { Text = "Width (0=auto):", Location = new Point(20, y), Size = new Size(120, 20) });
        _widthNumeric = new NumericUpDown { Location = new Point(150, y), Size = new Size(80, 20), Maximum = 4000, Value = 0 };
        Controls.Add(_widthNumeric);
        y += rowHeight;

        // Height setting
        Controls.Add(new Label { Text = "Height (0=auto):", Location = new Point(20, y), Size = new Size(120, 20) });
        _heightNumeric = new NumericUpDown { Location = new Point(150, y), Size = new Size(80, 20), Maximum = 4000, Value = 0 };
        Controls.Add(_heightNumeric);
        y += rowHeight;

        // Hardware acceleration
        _hwAccelCheckbox = new CheckBox { Text = "Hardware Acceleration", Location = new Point(20, y), Size = new Size(200, 20), Checked = true };
        Controls.Add(_hwAccelCheckbox);
        y += rowHeight;

        // Auto-start
        _autoStartCheckbox = new CheckBox { Text = "Auto-start Projection", Location = new Point(20, y), Size = new Size(200, 20) };
        Controls.Add(_autoStartCheckbox);
        y += rowHeight;

        // UI Scale
        Controls.Add(new Label { Text = "UI Scale:", Location = new Point(20, y), Size = new Size(120, 20) });
        _uiScaleTracker = new TrackBar { Location = new Point(150, y), Size = new Size(150, 45), Minimum = 5, Maximum = 20, Value = 10, TickFrequency = 5 };
        Controls.Add(_uiScaleTracker);
        y += 50;

        // Monitor
        Controls.Add(new Label { Text = "Monitor Index:", Location = new Point(20, y), Size = new Size(120, 20) });
        _monitorNumeric = new NumericUpDown { Location = new Point(150, y), Size = new Size(80, 20), Maximum = 10, Value = 0 };
        Controls.Add(_monitorNumeric);
        y += rowHeight + 20;

        // Buttons
        var okButton = new Button { Text = "OK", Size = new Size(80, 30), Location = new Point(220, y), DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancel", Size = new Size(80, 30), Location = new Point(310, y), DialogResult = DialogResult.Cancel };
        
        Controls.Add(okButton);
        Controls.Add(cancelButton);
        
        AcceptButton = okButton;
        CancelButton = cancelButton;
        
        okButton.Click += (s, e) => SaveSettings();
    }

    private void LoadCurrentSettings()
    {
        var settings = GuiState.Instance.CurrentSettings;
        
        _fpsNumeric.Value = settings.TargetFPS;
        _widthNumeric.Value = settings.ResolutionWidth;
        _heightNumeric.Value = settings.ResolutionHeight;
        _hwAccelCheckbox.Checked = settings.HardwareAcceleration;
        _autoStartCheckbox.Checked = settings.AutoStartProjection;
        _uiScaleTracker.Value = (int)(settings.UiScale * 10);
        _monitorNumeric.Value = settings.MonitorIndex;
    }

    private void SaveSettings()
    {
        var settings = GuiState.Instance.CurrentSettings;
        
        settings.TargetFPS = (int)_fpsNumeric.Value;
        settings.ResolutionWidth = (int)_widthNumeric.Value;
        settings.ResolutionHeight = (int)_heightNumeric.Value;
        settings.HardwareAcceleration = _hwAccelCheckbox.Checked;
        settings.AutoStartProjection = _autoStartCheckbox.Checked;
        settings.UiScale = _uiScaleTracker.Value / 10.0f;
        settings.MonitorIndex = (int)_monitorNumeric.Value;
        
        settings.Validate();
        GuiState.Instance.SaveSettings();
        
        Log.Info("Settings saved");
    }
}
#endif