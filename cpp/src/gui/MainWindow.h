#pragma once

#include "core/GameCapture.h"
#include "gui/KeybindManager.h"
#include <memory>
#include <functional>

namespace Pick6 {

class MainWindow {
public:
    MainWindow();
    ~MainWindow();

    // Initialize and show the window
    bool Initialize();
    void Show();
    void Hide();
    
    // Window message loop
    int Run();
    
    // Callbacks for UI events
    using StartInjectionCallback = std::function<void()>;
    using StopInjectionCallback = std::function<void()>;
    using ShowProjectionCallback = std::function<void()>;
    using HideProjectionCallback = std::function<void()>;
    using SettingsCallback = std::function<void()>;
    
    void SetStartInjectionCallback(StartInjectionCallback callback);
    void SetStopInjectionCallback(StopInjectionCallback callback);
    void SetShowProjectionCallback(ShowProjectionCallback callback);
    void SetHideProjectionCallback(HideProjectionCallback callback);
    void SetSettingsCallback(SettingsCallback callback);
    
    // Update UI status
    void UpdateStatus(const std::string& status);
    void UpdateProcessStatus(const std::string& processInfo);
    void UpdateCaptureStatus(const std::string& captureInfo);
    
    // Settings
    void SetAutoProjection(bool enabled);
    bool GetAutoProjection() const;
    
    void SetTargetFPS(int fps);
    int GetTargetFPS() const;
    
    // Keybind management
    void SetKeybindManager(std::shared_ptr<KeybindManager> keybindManager);

private:
    class Impl;
    std::unique_ptr<Impl> pImpl;
};

} // namespace Pick6