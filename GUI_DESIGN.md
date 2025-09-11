# Pick6 GUI Interface - OBS-Style Design

## Main Window Layout (480x400px)

```
┌─────────────────────────── Pick6 - Game Capture ────────────────────────────┐
│                                                                              │
│  [Start Injection]  [Stop]                                                  │
│    (Blue button)    (Red)                                                   │
│                                                                              │
│  ┌─ Status ──────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  Ready to inject                                                       │  │
│  │  FiveM Status: Not detected                                            │  │
│  │  Capture Status: Inactive                                              │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌─ Settings ────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  ☑ Auto-start projection window                                       │  │
│  │                                                                        │  │
│  │  Target FPS: [60   ▼]                                                 │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Workflow States

### 1. Initial State
- Status: "Ready to inject" (blue text)
- Start Injection button: Enabled (blue)
- Stop button: Disabled (gray)

### 2. Monitoring State (after clicking "Start Injection")
- Status: "Monitoring for FiveM..." (orange text)
- Start Injection button: Disabled, text changes to "Monitoring..."
- Stop button: Enabled (red)
- FiveM Status: "Waiting for FiveM to start..." (orange)

### 3. FiveM Detected State
- FiveM Status: "Found 1 process(es)" (green text)
- Status: "Injecting into FiveM..." (orange)

### 4. Successfully Injected State
- Status: "Successfully injected - Vulkan injection" (green)
- Capture Status: "Active (Vulkan injection)" (green)
- Projection window automatically opens (if checkbox enabled)

### 5. Error State
- Status: "Injection failed - try running as administrator" (red)
- Capture Status: "Failed" (red)

## Key Features

1. **OBS-Style Workflow**: Single click to start, automatic handling
2. **Real-time Feedback**: Status updates show exactly what's happening
3. **Process Priority**: Vulkan injection prioritized over window capture
4. **Auto-Projection**: Optional automatic projection window startup
5. **Settings Panel**: FPS control and projection preferences
6. **Clean Design**: Minimal, functional interface like OBS Game Capture

This interface eliminates all manual steps - user clicks "Start Injection" and the system handles everything automatically, just like OBS Game Capture does.

## Implemented Features

### Enhanced GUI Menu (v2.1)

The GUI has been enhanced with a persistent menu system that provides comprehensive control over the projection/injection workflow:

**Main Window Features:**
- **480x400px resizable interface** with modern flat design
- **Start/Stop Controls**: Blue "Start Injection" and red "Stop" buttons with proper state management
- **Settings Management**: Dedicated Settings button that opens a modal configuration dialog
- **Status Display**: Real-time status showing Idle/Starting/Running/Stopping/Error states with color coding
- **Log Output Panel**: Scrollable log display showing the last 200 entries in a console-style format
- **Hide Button**: Minimizes window while maintaining global hotkey functionality

**Settings Dialog Features:**
- **Auto-start projection**: Checkbox to enable automatic projection startup
- **Verbose logging**: Toggle for detailed logging output
- **Refresh interval control**: Numeric input (50-10000ms) with validation
- **Hotkey customization**: Text fields for toggle and stop/restore hotkeys
- **Output directory**: Folder picker for capture and log storage
- **Save/Cancel**: Proper dialog handling with validation

**Persistence & Data Management:**
- **Settings stored in `%AppData%\Pick6\settings.json`** following existing VersionStore pattern
- **Automatic validation** with range clamping for numeric values
- **Graceful fallback** to defaults for missing or corrupted settings
- **Real-time validation** with user feedback via log messages

**Logging Integration:**
- **GuiLogSink** implementation feeds system logs to the GUI
- **Thread-safe log display** with automatic scrolling
- **Maintains existing console/file logging** without disruption
- **Color-coded status indicators** (green=success, red=error, orange=in progress)

**Controller Architecture:**
- **ProjectionController** abstraction wraps existing capture/projection logic
- **Thread-safe Start/Stop operations** with proper state management
- **Event-driven status updates** for real-time UI feedback
- **Seamless integration** with existing GameCaptureEngine and BorderlessProjectionWindow

**Auto-Start Functionality:**
- **Configurable auto-start** via settings checkbox
- **Thread-safe startup** after form initialization
- **Non-blocking UI** during startup operations
- **Error handling** with fallback to manual operation

This implementation maintains full backward compatibility with existing functionality while providing a modern, user-friendly interface for projection control and settings management.