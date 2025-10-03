using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Pick6.Core;

namespace Pick6.Loader.UI;

/// <summary>
/// Utility class to load custom fonts from embedded resources
/// </summary>
public static class FontLoader
{
    private static PrivateFontCollection? _fontCollection;
    private static Font? _minecraftFont;
    private static readonly object _lock = new object();

    /// <summary>
    /// Gets the Minecraft-style font, loading it if necessary
    /// </summary>
    public static Font GetMinecraftFont(float size, FontStyle style = FontStyle.Regular)
    {
        lock (_lock)
        {
            if (_fontCollection == null)
            {
                LoadMinecraftFont();
            }

            if (_minecraftFont == null || Math.Abs(_minecraftFont.Size - size) > 0.1f || _minecraftFont.Style != style)
            {
                if (_fontCollection != null && _fontCollection.Families.Length > 0)
                {
                    _minecraftFont = new Font(_fontCollection.Families[0], size, style);
                }
                else
                {
                    // Fallback to Courier New if custom font fails to load
                    Log.Warn("Minecraft font not available, using Courier New fallback");
                    _minecraftFont = new Font("Courier New", size, style);
                }
            }

            return _minecraftFont;
        }
    }

    private static void LoadMinecraftFont()
    {
        try
        {
            _fontCollection = new PrivateFontCollection();

            // Load font from embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Pick6.Loader.Resources.Monocraft.ttf";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Log.Warn($"Could not find embedded font resource: {resourceName}");
                    return;
                }

                // Read font data into byte array
                var fontData = new byte[stream.Length];
                stream.Read(fontData, 0, fontData.Length);

                // Allocate memory and copy font data
                var fontPtr = Marshal.AllocCoTaskMem(fontData.Length);
                Marshal.Copy(fontData, 0, fontPtr, fontData.Length);

                // Add font to private collection
                _fontCollection.AddMemoryFont(fontPtr, fontData.Length);

                // Free allocated memory
                Marshal.FreeCoTaskMem(fontPtr);

                Log.Info("Minecraft font loaded successfully");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load Minecraft font: {ex.Message}");
            _fontCollection = null;
        }
    }

    /// <summary>
    /// Dispose of font resources
    /// </summary>
    public static void Dispose()
    {
        lock (_lock)
        {
            _minecraftFont?.Dispose();
            _minecraftFont = null;
            _fontCollection?.Dispose();
            _fontCollection = null;
        }
    }
}
