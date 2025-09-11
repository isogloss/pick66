#pragma once

#include "core/GameCapture.h"
#include <memory>

namespace Pick6 {

class ProjectionWindow {
public:
    ProjectionWindow();
    ~ProjectionWindow();

    // Initialize projection window
    bool Initialize();
    
    // Show/hide projection
    void Show();
    void Hide();
    bool IsVisible() const;
    
    // Update the frame being displayed
    void UpdateFrame(const FrameData& frame);
    
    // Projection settings
    void SetMonitor(int monitorIndex);
    void SetBorderless(bool borderless);
    void SetTopmost(bool topmost);
    
    // Stealth features - hide from task manager and Alt+Tab
    void EnableStealth(bool enabled);
    bool IsStealthEnabled() const;
    
    // Fullscreen control
    void SetFullscreen(bool fullscreen);
    bool IsFullscreen() const;

private:
    class Impl;
    std::unique_ptr<Impl> pImpl;
};

} // namespace Pick6