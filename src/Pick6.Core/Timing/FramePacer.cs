using System;
using System.Diagnostics;
using System.Threading;

namespace Pick6.Core.Timing;

/// <summary>
/// Frame pacing modes for different precision requirements
/// </summary>
public enum PacingMode
{
    /// <summary>Use Thread.Sleep for basic timing</summary>
    SleepPrecision,
    /// <summary>Hybrid approach: coarse sleep + spin wait</summary>
    HybridSpin,
    /// <summary>No frame limiting - run as fast as possible</summary>
    Unlimited,
    /// <summary>VSync-based timing (placeholder for future implementation)</summary>
    VSync
}

/// <summary>
/// Elapsed frame timing statistics
/// </summary>
public struct FrameElapsed
{
    /// <summary>Actual elapsed time since last frame (ms)</summary>
    public double ElapsedMs { get; init; }
    /// <summary>Target frame interval (ms)</summary>
    public double TargetIntervalMs { get; init; }
    /// <summary>Whether this frame was late (exceeded 1.5x target interval)</summary>
    public bool IsLate { get; init; }
}

/// <summary>
/// High-precision frame pacer for consistent frame timing
/// </summary>
public class FramePacer
{
    private readonly Stopwatch _stopwatch = new();
    private double _targetIntervalMs = 16.67; // Default 60 FPS
    private double _nextFrameTime;
    private PacingMode _mode = PacingMode.HybridSpin;
    private const double SafetyMarginMs = 1.0; // Safety margin for hybrid spin

    /// <summary>
    /// Current target FPS
    /// </summary>
    public double TargetFPS => 1000.0 / _targetIntervalMs;

    /// <summary>
    /// Current pacing mode
    /// </summary>
    public PacingMode Mode => _mode;

    /// <summary>
    /// Reset the pacer with new target FPS and pacing mode
    /// </summary>
    /// <param name="targetFps">Target frames per second</param>
    /// <param name="mode">Pacing mode to use</param>
    public void Reset(double targetFps, PacingMode mode = PacingMode.HybridSpin)
    {
        if (targetFps <= 0) throw new ArgumentException("Target FPS must be positive", nameof(targetFps));
        
        _targetIntervalMs = 1000.0 / targetFps;
        _mode = mode;
        _stopwatch.Restart();
        _nextFrameTime = 0.0;
    }

    /// <summary>
    /// Wait for the next frame and return elapsed timing information
    /// </summary>
    /// <returns>Frame timing statistics</returns>
    public FrameElapsed WaitNextFrame()
    {
        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();
            _nextFrameTime = _targetIntervalMs;
            return new FrameElapsed 
            { 
                ElapsedMs = 0, 
                TargetIntervalMs = _targetIntervalMs, 
                IsLate = false 
            };
        }

        var currentTime = _stopwatch.Elapsed.TotalMilliseconds;
        var elapsedSinceLastFrame = currentTime - (_nextFrameTime - _targetIntervalMs);

        switch (_mode)
        {
            case PacingMode.SleepPrecision:
                WaitWithSleep(currentTime);
                break;
            case PacingMode.HybridSpin:
                WaitWithHybridSpin(currentTime);
                break;
            case PacingMode.VSync:
                // TODO: Implement VSync timing when display sync is available
                WaitWithHybridSpin(currentTime);
                break;
            case PacingMode.Unlimited:
                // No waiting - run as fast as possible
                break;
        }

        // Update next frame time
        var actualCurrentTime = _stopwatch.Elapsed.TotalMilliseconds;
        
        if (actualCurrentTime >= _nextFrameTime)
        {
            _nextFrameTime += _targetIntervalMs;
            
            // Prevent drift by resetting if we're too far behind
            if (_nextFrameTime < actualCurrentTime - _targetIntervalMs)
            {
                _nextFrameTime = actualCurrentTime + _targetIntervalMs;
            }
        }

        return new FrameElapsed
        {
            ElapsedMs = elapsedSinceLastFrame,
            TargetIntervalMs = _targetIntervalMs,
            IsLate = elapsedSinceLastFrame > _targetIntervalMs * 1.5
        };
    }

    /// <summary>
    /// Wait using Thread.Sleep only
    /// </summary>
    private void WaitWithSleep(double currentTime)
    {
        var sleepTime = _nextFrameTime - currentTime;
        if (sleepTime > 1.0)
        {
            Thread.Sleep((int)sleepTime);
        }
    }

    /// <summary>
    /// Wait using hybrid approach: coarse sleep + spin wait
    /// </summary>
    private void WaitWithHybridSpin(double currentTime)
    {
        var sleepTime = _nextFrameTime - currentTime;
        
        // Coarse sleep for most of the wait time
        if (sleepTime > SafetyMarginMs + 1.0)
        {
            Thread.Sleep((int)(sleepTime - SafetyMarginMs));
        }
        
        // Spin wait for high precision on the remaining time
        while (_stopwatch.Elapsed.TotalMilliseconds < _nextFrameTime)
        {
            Thread.SpinWait(10);
        }
    }
}