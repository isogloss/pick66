#pragma once

#ifdef _WIN32
#include <windows.h>
#endif

namespace Pick6 {

class StealthManager {
public:
    StealthManager();
    ~StealthManager();

    // Hide window from Alt+Tab list
    static bool HideFromAltTab(void* windowHandle);
    
    // Show window in Alt+Tab list
    static bool ShowInAltTab(void* windowHandle);
    
    // Hide window from taskbar
    static bool HideFromTaskbar(void* windowHandle);
    
    // Show window in taskbar
    static bool ShowInTaskbar(void* windowHandle);
    
    // Make window completely invisible to window enumeration
    static bool EnableInvisibilityMode(void* windowHandle);
    static bool DisableInvisibilityMode(void* windowHandle);
    
    // Set window as a tool window (reduces visibility in system UI)
    static bool SetAsToolWindow(void* windowHandle, bool enable);

#ifdef _WIN32
    // Windows-specific stealth features
    static bool HideFromTaskManager(DWORD processId);
    static bool SetWindowExStyle(HWND hwnd, DWORD exStyle, bool add);
#endif

private:
    // Track stealth state
    static void* stealthWindow;
    static bool isStealthActive;
};

} // namespace Pick6