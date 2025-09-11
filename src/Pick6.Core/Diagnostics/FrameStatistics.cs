using System;
using System.Linq;

namespace Pick6.Core.Diagnostics;

/// <summary>
/// Collects and analyzes frame timing statistics using a ring buffer
/// </summary>
public class FrameStatistics
{
    private readonly double[] _frameIntervals;
    private readonly double[] _targetIntervals;
    private readonly int _bufferSize;
    private int _writeIndex;
    private int _totalFrames;
    private int _droppedFrames;
    private readonly object _lock = new();

    /// <summary>
    /// Initialize frame statistics with specified buffer size
    /// </summary>
    /// <param name="bufferSize">Number of frame intervals to keep in history (default: 240 frames)</param>
    public FrameStatistics(int bufferSize = 240)
    {
        if (bufferSize <= 0) throw new ArgumentException("Buffer size must be positive", nameof(bufferSize));
        
        _bufferSize = bufferSize;
        _frameIntervals = new double[bufferSize];
        _targetIntervals = new double[bufferSize];
        _writeIndex = 0;
        _totalFrames = 0;
        _droppedFrames = 0;
    }

    /// <summary>
    /// Record a frame timing measurement
    /// </summary>
    /// <param name="deltaMs">Actual frame interval in milliseconds</param>
    /// <param name="targetIntervalMs">Target frame interval in milliseconds</param>
    public void RecordFrame(double deltaMs, double targetIntervalMs)
    {
        lock (_lock)
        {
            _frameIntervals[_writeIndex] = deltaMs;
            _targetIntervals[_writeIndex] = targetIntervalMs;
            
            _writeIndex = (_writeIndex + 1) % _bufferSize;
            _totalFrames++;

            // Count as dropped if frame took more than 1.5x the target interval
            if (deltaMs > targetIntervalMs * 1.5)
            {
                _droppedFrames++;
            }
        }
    }

    /// <summary>
    /// Current instantaneous FPS based on most recent frame
    /// </summary>
    public double InstantFps
    {
        get
        {
            lock (_lock)
            {
                if (_totalFrames == 0) return 0.0;
                
                var lastIndex = (_writeIndex - 1 + _bufferSize) % _bufferSize;
                var lastInterval = _frameIntervals[lastIndex];
                
                return lastInterval > 0 ? 1000.0 / lastInterval : 0.0;
            }
        }
    }

    /// <summary>
    /// Average FPS over the measurement window
    /// </summary>
    public double AverageFps
    {
        get
        {
            lock (_lock)
            {
                if (_totalFrames == 0) return 0.0;
                
                var validFrames = Math.Min(_totalFrames, _bufferSize);
                if (validFrames == 0) return 0.0;
                
                var sumIntervals = _frameIntervals.Take(validFrames).Sum();
                var avgInterval = sumIntervals / validFrames;
                
                return avgInterval > 0 ? 1000.0 / avgInterval : 0.0;
            }
        }
    }

    /// <summary>
    /// 95th percentile frame time in milliseconds (higher values indicate worse performance)
    /// </summary>
    public double P95FrameTimeMs
    {
        get
        {
            lock (_lock)
            {
                if (_totalFrames == 0) return 0.0;
                
                var validFrames = Math.Min(_totalFrames, _bufferSize);
                if (validFrames == 0) return 0.0;
                
                var sortedIntervals = _frameIntervals
                    .Take(validFrames)
                    .OrderBy(x => x)
                    .ToArray();
                
                var p95Index = (int)Math.Ceiling(0.95 * validFrames) - 1;
                p95Index = Math.Max(0, Math.Min(p95Index, sortedIntervals.Length - 1));
                
                return sortedIntervals[p95Index];
            }
        }
    }

    /// <summary>
    /// Total number of dropped frames (frames that exceeded 1.5x target interval)
    /// </summary>
    public int DroppedFrames
    {
        get
        {
            lock (_lock)
            {
                return _droppedFrames;
            }
        }
    }

    /// <summary>
    /// Total number of frames recorded
    /// </summary>
    public int TotalFrames
    {
        get
        {
            lock (_lock)
            {
                return _totalFrames;
            }
        }
    }

    /// <summary>
    /// Drop rate as a percentage (0.0 to 100.0)
    /// </summary>
    public double DropRate
    {
        get
        {
            lock (_lock)
            {
                return _totalFrames > 0 ? (double)_droppedFrames / _totalFrames * 100.0 : 0.0;
            }
        }
    }

    /// <summary>
    /// Reset all statistics
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            Array.Clear(_frameIntervals, 0, _frameIntervals.Length);
            Array.Clear(_targetIntervals, 0, _targetIntervals.Length);
            _writeIndex = 0;
            _totalFrames = 0;
            _droppedFrames = 0;
        }
    }

    /// <summary>
    /// Get a formatted summary string of current statistics
    /// </summary>
    /// <returns>Formatted statistics summary</returns>
    public string GetSummary()
    {
        lock (_lock)
        {
            if (_totalFrames == 0)
                return "No frame data available";

            return $"FPS: {InstantFps:F1} (avg: {AverageFps:F1}) | " +
                   $"P95: {P95FrameTimeMs:F1}ms | " +
                   $"Dropped: {_droppedFrames}/{_totalFrames} ({DropRate:F1}%)";
        }
    }
}