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