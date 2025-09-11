using System;
using System.Text;

namespace Pick6.Core.Util;

/// <summary>
/// Provides text glyphs with Unicode fallback support for console and GUI display
/// </summary>
public static class TextGlyphs
{
    /// <summary>
    /// Spinner frame characters (extended sequence)
    /// </summary>
    public static readonly string[] SpinnerFrames = { "-", "/", "|", "\\", "-", "/", "-", "\\", "|" };

    /// <summary>
    /// Success glyph with fallback
    /// </summary>
    public static string Success => SupportsUnicode ? "✓" : "[OK]";

    /// <summary>
    /// Failure glyph with fallback
    /// </summary>
    public static string Fail => SupportsUnicode ? "✗" : "[FAIL]";

    /// <summary>
    /// Warning glyph with fallback
    /// </summary>
    public static string Warning => SupportsUnicode ? "⚠" : "[WARN]";

    /// <summary>
    /// Info glyph with fallback
    /// </summary>
    public static string Info => SupportsUnicode ? "ⓘ" : "[INFO]";

    /// <summary>
    /// Check if the current environment supports Unicode characters
    /// </summary>
    public static bool SupportsUnicode
    {
        get
        {
            try
            {
                // Check console encoding
                if (Console.OutputEncoding.EncodingName.Contains("UTF", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Check environment locale
                var locale = Environment.GetEnvironmentVariable("LANG") ?? 
                           Environment.GetEnvironmentVariable("LC_ALL") ?? "";
                
                return locale.Contains("UTF", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Check if console supports TTY features (cursor control, etc.)
    /// </summary>
    public static bool SupportsTTY
    {
        get
        {
            try
            {
                // Check if output is redirected
                if (Console.IsOutputRedirected)
                    return false;

                // Basic TTY feature check
                return !Console.IsOutputRedirected && Environment.UserInteractive;
            }
            catch
            {
                return false;
            }
        }
    }
}