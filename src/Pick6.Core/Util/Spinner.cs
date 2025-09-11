using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pick6.Core.Util;

/// <summary>
/// Provides animated spinner functionality for console output during long-running operations
/// </summary>
public class Spinner : IDisposable
{
    private readonly string _message;
    private readonly int _intervalMs;
    private readonly bool _supportsTTY;
    private readonly bool _cursorWasVisible;
    private volatile bool _isRunning;
    private Task? _animationTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _currentFrame;

    /// <summary>
    /// Initialize a new spinner with the specified message and animation interval
    /// </summary>
    /// <param name="message">Message to display alongside the spinner</param>
    /// <param name="intervalMs">Animation interval in milliseconds</param>
    public Spinner(string message, int intervalMs = 90)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
        _intervalMs = intervalMs;
        _supportsTTY = TextGlyphs.SupportsTTY;
        
        // Store initial cursor visibility state
        try
        {
            _cursorWasVisible = Console.CursorVisible;
        }
        catch
        {
            _cursorWasVisible = true;
        }
    }

    /// <summary>
    /// Start the spinner animation
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();

        if (_supportsTTY)
        {
            try
            {
                // Hide cursor
                Console.CursorVisible = false;
            }
            catch { /* Ignore cursor control failures */ }
        }

        _animationTask = Task.Run(AnimationLoop, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Stop the spinner and show success message
    /// </summary>
    /// <param name="finalMessage">Optional final message to show. If null, uses success glyph with original message.</param>
    public void Success(string? finalMessage = null)
    {
        Stop();
        var message = finalMessage ?? $"{TextGlyphs.Success} {_message}";
        Console.WriteLine(message);
    }

    /// <summary>
    /// Stop the spinner and show failure message
    /// </summary>
    /// <param name="finalMessage">Optional final message to show. If null, uses failure glyph with original message.</param>
    public void Fail(string? finalMessage = null)
    {
        Stop();
        var message = finalMessage ?? $"{TextGlyphs.Fail} {_message}";
        Console.WriteLine(message);
    }

    /// <summary>
    /// Stop the spinner without showing any final message
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        
        try
        {
            _animationTask?.Wait(TimeSpan.FromMilliseconds(_intervalMs * 2));
        }
        catch (AggregateException) { /* Ignore cancellation exceptions */ }

        // Clear the current line if TTY is supported
        if (_supportsTTY)
        {
            try
            {
                Console.Write('\r');
                Console.Write(new string(' ', _message.Length + 10)); // Clear with padding
                Console.Write('\r');
                
                // Restore cursor visibility
                Console.CursorVisible = _cursorWasVisible;
            }
            catch { /* Ignore console control failures */ }
        }
    }

    /// <summary>
    /// Animation loop for the spinner
    /// </summary>
    private async Task AnimationLoop()
    {
        try
        {
            while (_isRunning && !_cancellationTokenSource!.Token.IsCancellationRequested)
            {
                if (_supportsTTY)
                {
                    // TTY mode: animate in place
                    var frame = TextGlyphs.SpinnerFrames[_currentFrame % TextGlyphs.SpinnerFrames.Length];
                    Console.Write($"\r{frame} {_message}");
                    _currentFrame++;
                }
                else
                {
                    // Non-TTY mode: simple fallback, no animation
                    if (_currentFrame == 0)
                    {
                        Console.Write($"[-] {_message}");
                    }
                    _currentFrame++;
                }

                await Task.Delay(_intervalMs, _cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException) { /* Expected when stopping */ }
        catch (Exception) { /* Ignore other exceptions to prevent crashes */ }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        Stop();
        _cancellationTokenSource?.Dispose();
        _animationTask?.Dispose();
        GC.SuppressFinalize(this);
    }
}