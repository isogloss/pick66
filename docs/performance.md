# Pick6 Performance Guide

This guide covers the enhanced performance features introduced in Pick6 v2.0, including frame pacing technology, diagnostics, and optimization strategies.

## Frame Pacing Architecture

### FramePacer

The `FramePacer` class provides high-precision timing control for consistent frame delivery:

```csharp
// Initialize with target FPS and pacing mode
_framePacer.Reset(60, PacingMode.HybridSpin);

// In capture/render loop
var frameElapsed = _framePacer.WaitNextFrame();
_statistics.RecordFrame(frameElapsed.ElapsedMs, frameElapsed.TargetIntervalMs);
```

**Available Modes:**
- `HybridSpin`: Uses Thread.Sleep for coarse timing, then spin-waits for precision (default)
- `SleepPrecision`: Uses only Thread.Sleep for timing (lower CPU usage)
- `Unlimited`: No frame limiting - runs as fast as possible
- `VSync`: Display synchronization (placeholder for future implementation)

### Frame Statistics

The `FrameStatistics` class maintains a ring buffer of frame timing data:

```csharp
// Access performance metrics
var stats = captureEngine.Statistics;
Console.WriteLine($"FPS: {stats.InstantFps:F1} (avg: {stats.AverageFps:F1})");
Console.WriteLine($"P95 Frame Time: {stats.P95FrameTimeMs:F1}ms");
Console.WriteLine($"Drop Rate: {stats.DropRate:F1}%");
```

**Metrics Provided:**
- **Instant FPS**: Frame rate based on most recent frame interval
- **Average FPS**: Moving average over measurement window (up to 240 frames)
- **95th Percentile Frame Time**: Time exceeded by only 5% of frames (smoothness indicator)
- **Dropped Frames**: Frames that exceeded 1.5x the target interval
- **Drop Rate**: Percentage of total frames that were dropped

## Performance Monitoring

### Console Interface

Access the enhanced console menu:

```bash
pick6.exe --interactive
```

**Key Menu Options:**
- **Option 4**: Configure FPS with presets (30/60/120/144)
- **Option 13**: Live statistics monitoring
- **Option 14**: Performance analysis and warnings
- **Option 15**: Toggle diagnostic logging
- **Option 16**: Export comprehensive diagnostics

### Environment Variables

**Enable Detailed Logging:**
```bash
# Windows Command Prompt
set PICK6_DIAG=1
pick6.exe

# Windows PowerShell  
$env:PICK6_DIAG=1
pick6.exe

# Linux/macOS
export PICK6_DIAG=1
./pick6
```

When enabled, the application logs frame timing information every second:
```
[GDI Capture] FPS: 59.8 (avg: 59.2) | P95: 16.9ms | Dropped: 2/3580 (0.1%)
[Projection] FPS: 60.1 (avg: 59.7) | P95: 16.2ms | Dropped: 0/3598 (0.0%)
```

### Live Statistics Display

The live monitoring mode (Option 13) provides real-time updates:

```
Capture:    FPS: 59.8 (avg: 59.2) | P95: 16.9ms | Dropped: 2/3580 (0.1%)
Projection: Active (stats not available via current interface)
Uptime:     00:02:45
Memory:     89.2 MB
```

Updates 10 times per second until any key is pressed.

## Performance Analysis

### Automated Warnings

The system automatically detects common performance issues:

**Low Frame Rate Warning:**
```
⚠️  Capture FPS Warning: Average 42.3 FPS is significantly below target 60 FPS
   Possible causes: CPU overload, insufficient memory, game blocking capture
```

**Frame Time Consistency Warning:**
```
⚠️  Frame Time Warning: 95th percentile frame time is 35.2ms (target: 16.7ms)
   This indicates inconsistent frame delivery
```

**High Drop Rate Warning:**
```
⚠️  Drop Rate Warning: 8.3% of frames are dropped
   Consider reducing FPS or resolution
```

**Memory Usage Warning:**
```
⚠️  Memory Usage Warning: 512.4 MB allocated
   High memory usage may indicate a memory leak or excessive frame buffering
```

### Diagnostic Export

The diagnostic export (Option 16) creates a comprehensive system report:

```
pick6_diagnostics_2024-01-15_14-30-45.txt
```

**Contents Include:**
- Application version and build information
- Capture engine configuration and statistics
- System information (OS, CLR version, processor count)
- Memory usage (working set, GC memory)
- FiveM process detection results
- Environment variables

## Optimization Strategies

### Target FPS Selection

**30 FPS**: Lowest CPU usage, suitable for:
- Battery-powered devices
- Systems with limited processing power
- Background capture scenarios

**60 FPS**: Balanced performance, recommended for:
- Most gaming scenarios
- Standard monitors (60Hz refresh rate)
- General-purpose capture

**120+ FPS**: High performance, ideal for:
- Gaming monitors (120Hz, 144Hz, 240Hz)
- Competitive gaming scenarios
- Systems with abundant CPU/GPU resources

### Resolution Optimization

**Original Resolution**: 
- No scaling overhead
- Best quality preservation
- Highest resource usage

**1080p (1920x1080)**:
- Good balance of quality and performance
- Compatible with most displays
- Moderate resource usage

**720p (1280x720)**:
- Lower resource usage
- Suitable for streaming/recording
- May show scaling artifacts

### Hardware Optimization

**Run as Administrator:**
```bash
# Improves injection success rate
# Enables more efficient process access
```

**Vulkan Injection vs Window Capture:**
- Vulkan injection provides better performance when available
- Falls back to GDI window capture automatically
- Check "Show last injection method" (Option 23) to verify method used

**Hardware Acceleration:**
```bash
# Toggle via Option 6 in console menu
# Uses GPU resources for capture processing
# May improve performance on systems with dedicated graphics
```

### System-Level Optimizations

**Close Unnecessary Applications:**
- Free up CPU and memory resources
- Reduce system context switching overhead
- Improve overall stability

**Paging Mode Selection:**
- `HybridSpin`: Best precision, higher CPU usage
- `SleepPrecision`: Lower CPU usage, reduced precision
- Switch via code modification (future menu option)

**Monitor Resource Usage:**
- Use Task Manager or system monitor
- Watch for CPU spikes during capture
- Monitor memory growth over time

## Troubleshooting Common Issues

### Half-Rate Performance (30 FPS when targeting 60)

**Symptoms:**
- Consistent frame rate exactly half of target
- P95 frame times approximately 2x target interval

**Causes:**
- Frame duplication in projection path
- VSync interference
- Display driver issues

**Solutions:**
1. Disable VSync in game/driver settings
2. Check for frame skipping logic in projection
3. Verify monitor refresh rate matches target FPS

### Inconsistent Frame Delivery

**Symptoms:**
- High P95 frame times relative to average
- Variable frame intervals
- Occasional frame drops

**Causes:**
- CPU scheduling interference
- Background processes
- Insufficient system resources

**Solutions:**
1. Lower target FPS or resolution
2. Close background applications
3. Increase process priority (administrator mode)
4. Switch to SleepPrecision pacing mode

### Memory Leaks

**Symptoms:**
- Steadily increasing memory usage over time
- System becomes slower after extended use
- Eventually leads to out-of-memory errors

**Causes:**
- Frame buffers not properly disposed
- Event handler memory leaks
- Accumulating diagnostic data

**Solutions:**
1. Restart application periodically
2. Monitor memory usage via diagnostics
3. Report issues with diagnostic export data

### Low Sustained Performance

**Symptoms:**
- Average FPS significantly below target
- High CPU usage
- System becomes unresponsive

**Causes:**
- System resource exhaustion
- Inefficient capture method
- Background interference

**Solutions:**
1. Reduce target FPS (try 30 FPS)
2. Lower capture resolution
3. Disable hardware acceleration
4. Ensure FiveM is not CPU-bound
5. Check for malware/background processes

## Future Enhancements

### Planned Features

**VSync Integration:**
- True display synchronization
- Eliminates tearing
- Matches display refresh rate automatically

**Adaptive Frame Pacing:**
- Automatic FPS adjustment based on system performance
- Dynamic quality scaling
- Load balancing between capture and projection

**Extended Statistics:**
- GPU usage monitoring
- Network performance metrics
- Game-specific performance indicators

**Export Formats:**
- CSV export for analysis tools
- JSON format for programmatic processing
- Real-time streaming to external monitors

### Contributing Performance Improvements

1. Profile with diagnostic logging enabled
2. Identify bottlenecks using exported data
3. Test optimizations with consistent workloads
4. Document performance impact of changes
5. Submit improvements with before/after metrics