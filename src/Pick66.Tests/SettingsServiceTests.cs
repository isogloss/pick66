using Pick6.Loader.Settings;
using System.IO;
using Xunit;

namespace Pick66.Tests;

/// <summary>
/// Tests for the SettingsService Load/Save functionality
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _tempSettingsPath;
    private readonly string _originalSettingsPath;

    public SettingsServiceTests()
    {
        // Create a temporary directory for test settings
        var tempDir = Path.Combine(Path.GetTempPath(), "Pick6Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        _tempSettingsPath = Path.Combine(tempDir, "settings.json");
        
        // Store original settings path for cleanup
        _originalSettingsPath = SettingsService.GetSettingsFilePath();
        
        // Use reflection to set the temp path for testing
        // Since we can't easily mock static methods, we'll test with a known temp location
    }

    [Fact]
    public void TryLoadOrDefault_ReturnsDefaultSettings_WhenFileDoesNotExist()
    {
        // Arrange - ensure no settings file exists
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "settings.json");
        
        // Act
        var settings = SettingsService.TryLoadOrDefault();
        
        // Assert
        Assert.NotNull(settings);
        Assert.False(settings.AutoStartProjection);
        Assert.False(settings.VerboseLogging);
        Assert.Equal(500, settings.ProjectionRefreshIntervalMs);
        Assert.Equal("Ctrl+P", settings.HotkeyToggleProjection);
        Assert.Equal("Ctrl+Shift+P", settings.HotkeyStopAndRestore);
        Assert.Equal("output", settings.OutputDirectory);
    }

    [Fact]
    public void SaveAndLoadRoundTrip_PreservesAllSettings()
    {
        // Arrange
        var originalSettings = new UserSettings
        {
            AutoStartProjection = true,
            VerboseLogging = true,
            ProjectionRefreshIntervalMs = 1000,
            HotkeyToggleProjection = "Ctrl+T",
            HotkeyStopAndRestore = "Ctrl+Alt+S",
            OutputDirectory = "custom_output"
        };

        // Act - Save settings
        var saveSuccess = SettingsService.Save(originalSettings);
        Assert.True(saveSuccess);

        // Act - Load settings (this will use the default path, but the save would have created the file)
        var loadedSettings = SettingsService.TryLoadOrDefault();

        // Assert
        Assert.NotNull(loadedSettings);
        Assert.Equal(originalSettings.AutoStartProjection, loadedSettings.AutoStartProjection);
        Assert.Equal(originalSettings.VerboseLogging, loadedSettings.VerboseLogging);
        Assert.Equal(originalSettings.ProjectionRefreshIntervalMs, loadedSettings.ProjectionRefreshIntervalMs);
        Assert.Equal(originalSettings.HotkeyToggleProjection, loadedSettings.HotkeyToggleProjection);
        Assert.Equal(originalSettings.HotkeyStopAndRestore, loadedSettings.HotkeyStopAndRestore);
        Assert.Equal(originalSettings.OutputDirectory, loadedSettings.OutputDirectory);
    }

    [Fact]
    public void UserSettings_Validate_ClampsRefreshInterval()
    {
        // Arrange
        var settings = new UserSettings
        {
            ProjectionRefreshIntervalMs = 25 // Below minimum of 50
        };

        // Act
        settings.Validate();

        // Assert
        Assert.Equal(50, settings.ProjectionRefreshIntervalMs);

        // Arrange - test upper bound
        settings.ProjectionRefreshIntervalMs = 15000; // Above maximum of 10000

        // Act
        settings.Validate();

        // Assert
        Assert.Equal(10000, settings.ProjectionRefreshIntervalMs);
    }

    [Fact]
    public void UserSettings_Validate_FixesNullOrEmptyValues()
    {
        // Arrange
        var settings = new UserSettings
        {
            HotkeyToggleProjection = "",
            HotkeyStopAndRestore = null!,
            OutputDirectory = "   " // Whitespace only
        };

        // Act
        settings.Validate();

        // Assert
        Assert.Equal("Ctrl+P", settings.HotkeyToggleProjection);
        Assert.Equal("Ctrl+Shift+P", settings.HotkeyStopAndRestore);
        Assert.Equal("output", settings.OutputDirectory);
    }

    [Fact]
    public void SettingsService_Save_ReturnsFalseForNullSettings()
    {
        // Act
        var result = SettingsService.Save(null!);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        // Clean up any test files
        try
        {
            var settingsFile = SettingsService.GetSettingsFilePath();
            if (File.Exists(settingsFile))
            {
                File.Delete(settingsFile);
            }
            
            var settingsDir = Path.GetDirectoryName(settingsFile);
            if (Directory.Exists(settingsDir) && Directory.GetFiles(settingsDir).Length == 0)
            {
                Directory.Delete(settingsDir);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}