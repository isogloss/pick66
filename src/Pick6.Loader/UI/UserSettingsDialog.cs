using System;
using System.Drawing;
using System.Windows.Forms;
using Pick6.Loader.Settings;

namespace Pick6.Loader.UI;

/// <summary>
/// Modal dialog for editing user settings
/// </summary>
public partial class UserSettingsDialog : Form
{
    private UserSettings _settings;
    private UserSettings _originalSettings;

    // UI Controls
    private CheckBox _autoStartProjectionCheckBox = null!;
    private CheckBox _verboseLoggingCheckBox = null!;
    private NumericUpDown _refreshIntervalNumeric = null!;
    private TextBox _toggleHotkeyTextBox = null!;
    private TextBox _stopHotkeyTextBox = null!;
    private TextBox _outputDirectoryTextBox = null!;
    private Button _browseButton = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public UserSettings Settings => _settings;

    public UserSettingsDialog(UserSettings settings)
    {
        _settings = new UserSettings
        {
            AutoStartProjection = settings.AutoStartProjection,
            VerboseLogging = settings.VerboseLogging,
            ProjectionRefreshIntervalMs = settings.ProjectionRefreshIntervalMs,
            HotkeyToggleProjection = settings.HotkeyToggleProjection,
            HotkeyStopAndRestore = settings.HotkeyStopAndRestore,
            OutputDirectory = settings.OutputDirectory
        };
        
        _originalSettings = settings;
        InitializeComponent();
        PopulateControls();
    }

    private void InitializeComponent()
    {
        Text = "Pick6 Settings";
        Size = new Size(450, 350);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        
        var font = new Font("Segoe UI", 9);

        // Auto-start projection checkbox
        var autoStartLabel = new Label
        {
            Text = "Auto-start projection:",
            Location = new Point(20, 20),
            Size = new Size(150, 23),
            Font = font
        };
        
        _autoStartProjectionCheckBox = new CheckBox
        {
            Location = new Point(180, 20),
            Size = new Size(200, 23),
            Font = font,
            Text = "Start projection automatically on app launch"
        };

        // Verbose logging checkbox
        var verboseLabel = new Label
        {
            Text = "Verbose logging:",
            Location = new Point(20, 50),
            Size = new Size(150, 23),
            Font = font
        };
        
        _verboseLoggingCheckBox = new CheckBox
        {
            Location = new Point(180, 50),
            Size = new Size(200, 23),
            Font = font,
            Text = "Enable detailed logging output"
        };

        // Refresh interval
        var refreshLabel = new Label
        {
            Text = "Refresh interval (ms):",
            Location = new Point(20, 80),
            Size = new Size(150, 23),
            Font = font
        };
        
        _refreshIntervalNumeric = new NumericUpDown
        {
            Location = new Point(180, 80),
            Size = new Size(80, 23),
            Font = font,
            Minimum = 50,
            Maximum = 10000,
            Increment = 50
        };

        // Toggle hotkey
        var toggleHotkeyLabel = new Label
        {
            Text = "Toggle hotkey:",
            Location = new Point(20, 110),
            Size = new Size(150, 23),
            Font = font
        };
        
        _toggleHotkeyTextBox = new TextBox
        {
            Location = new Point(180, 110),
            Size = new Size(200, 23),
            Font = font
        };

        // Stop and restore hotkey
        var stopHotkeyLabel = new Label
        {
            Text = "Stop & restore hotkey:",
            Location = new Point(20, 140),
            Size = new Size(150, 23),
            Font = font
        };
        
        _stopHotkeyTextBox = new TextBox
        {
            Location = new Point(180, 140),
            Size = new Size(200, 23),
            Font = font
        };

        // Output directory
        var outputDirLabel = new Label
        {
            Text = "Output directory:",
            Location = new Point(20, 170),
            Size = new Size(150, 23),
            Font = font
        };
        
        _outputDirectoryTextBox = new TextBox
        {
            Location = new Point(180, 170),
            Size = new Size(150, 23),
            Font = font
        };

        _browseButton = new Button
        {
            Text = "...",
            Location = new Point(340, 170),
            Size = new Size(30, 23),
            Font = font
        };
        _browseButton.Click += BrowseButton_Click;

        // Buttons
        _saveButton = new Button
        {
            Text = "Save",
            Location = new Point(180, 270),
            Size = new Size(75, 30),
            Font = font,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _saveButton.FlatAppearance.BorderSize = 0;
        _saveButton.Click += SaveButton_Click;

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(270, 270),
            Size = new Size(75, 30),
            Font = font,
            BackColor = Color.Gray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _cancelButton.FlatAppearance.BorderSize = 0;
        _cancelButton.Click += CancelButton_Click;

        // Add controls to form
        Controls.AddRange(new Control[]
        {
            autoStartLabel, _autoStartProjectionCheckBox,
            verboseLabel, _verboseLoggingCheckBox,
            refreshLabel, _refreshIntervalNumeric,
            toggleHotkeyLabel, _toggleHotkeyTextBox,
            stopHotkeyLabel, _stopHotkeyTextBox,
            outputDirLabel, _outputDirectoryTextBox, _browseButton,
            _saveButton, _cancelButton
        });
    }

    private void PopulateControls()
    {
        _autoStartProjectionCheckBox.Checked = _settings.AutoStartProjection;
        _verboseLoggingCheckBox.Checked = _settings.VerboseLogging;
        _refreshIntervalNumeric.Value = Math.Max((decimal)_settings.ProjectionRefreshIntervalMs, _refreshIntervalNumeric.Minimum);
        _toggleHotkeyTextBox.Text = _settings.HotkeyToggleProjection;
        _stopHotkeyTextBox.Text = _settings.HotkeyStopAndRestore;
        _outputDirectoryTextBox.Text = _settings.OutputDirectory;
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select output directory",
            SelectedPath = _outputDirectoryTextBox.Text
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _outputDirectoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        // Update settings from controls
        _settings.AutoStartProjection = _autoStartProjectionCheckBox.Checked;
        _settings.VerboseLogging = _verboseLoggingCheckBox.Checked;
        _settings.ProjectionRefreshIntervalMs = (int)_refreshIntervalNumeric.Value;
        _settings.HotkeyToggleProjection = _toggleHotkeyTextBox.Text;
        _settings.HotkeyStopAndRestore = _stopHotkeyTextBox.Text;
        _settings.OutputDirectory = _outputDirectoryTextBox.Text;

        // Validate settings
        _settings.Validate();

        DialogResult = DialogResult.OK;
        Close();
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}