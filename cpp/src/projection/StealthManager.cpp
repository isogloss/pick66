#include "StealthManager.h"
#include <iostream>

#ifdef _WIN32
#include <windows.h>
#include <shellapi.h>
#include <dwmapi.h>

#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "dwmapi.lib")
#endif

namespace Pick6 {

void* StealthManager::stealthWindow = nullptr;
bool StealthManager::isStealthActive = false;

StealthManager::StealthManager() = default;
StealthManager::~StealthManager() = default;

bool StealthManager::HideFromAltTab(void* windowHandle) {
#ifdef _WIN32
    HWND hwnd = static_cast<HWND>(windowHandle);
    if (!hwnd) return false;

    // Method 1: Set WS_EX_TOOLWINDOW extended style
    LONG_PTR exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
    exStyle |= WS_EX_TOOLWINDOW;
    exStyle &= ~WS_EX_APPWINDOW;
    SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);

    // Method 2: Use DWM to exclude from Alt+Tab
    BOOL exclude = TRUE;
    DwmSetWindowAttribute(hwnd, DWMWA_EXCLUDED_FROM_PEEK, &exclude, sizeof(exclude));

    return true;
#else
    // Linux implementation would use X11 window properties
    (void)windowHandle; // Suppress unused parameter warning
    std::cout << "HideFromAltTab called (Linux stub)" << std::endl;
    return true;
#endif
}

bool StealthManager::ShowInAltTab(void* windowHandle) {
#ifdef _WIN32
    HWND hwnd = static_cast<HWND>(windowHandle);
    if (!hwnd) return false;

    // Restore normal window styles
    LONG_PTR exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
    exStyle &= ~WS_EX_TOOLWINDOW;
    exStyle |= WS_EX_APPWINDOW;
    SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);

    // Include in Alt+Tab
    BOOL exclude = FALSE;
    DwmSetWindowAttribute(hwnd, DWMWA_EXCLUDED_FROM_PEEK, &exclude, sizeof(exclude));

    return true;
#else
    (void)windowHandle; // Suppress unused parameter warning
    std::cout << "ShowInAltTab called (Linux stub)" << std::endl;
    return true;
#endif
}

bool StealthManager::HideFromTaskbar(void* windowHandle) {
#ifdef _WIN32
    HWND hwnd = static_cast<HWND>(windowHandle);
    if (!hwnd) return false;

    // Hide the window from the taskbar by removing it from the shell
    LONG_PTR exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
    exStyle |= WS_EX_TOOLWINDOW;
    SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);

    // Force taskbar to update
    ShowWindow(hwnd, SW_HIDE);
    ShowWindow(hwnd, SW_SHOW);

    return true;
#else
    (void)windowHandle; // Suppress unused parameter warning
    std::cout << "HideFromTaskbar called (Linux stub)" << std::endl;
    return true;
#endif
}

bool StealthManager::ShowInTaskbar(void* windowHandle) {
#ifdef _WIN32
    HWND hwnd = static_cast<HWND>(windowHandle);
    if (!hwnd) return false;

    // Show the window in the taskbar
    LONG_PTR exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
    exStyle &= ~WS_EX_TOOLWINDOW;
    exStyle |= WS_EX_APPWINDOW;
    SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);

    // Force taskbar to update
    ShowWindow(hwnd, SW_HIDE);
    ShowWindow(hwnd, SW_SHOW);

    return true;
#else
    (void)windowHandle; // Suppress unused parameter warning
    std::cout << "ShowInTaskbar called (Linux stub)" << std::endl;
    return true;
#endif
}

bool StealthManager::EnableInvisibilityMode(void* windowHandle) {
#ifdef _WIN32
    HWND hwnd = static_cast<HWND>(windowHandle);
    if (!hwnd) return false;

    // Make window completely invisible to most system tools
    // This is more aggressive than just hiding from Alt+Tab
    
    // 1. Set multiple stealth flags
    SetWindowExStyle(hwnd, WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE, true);
    SetWindowExStyle(hwnd, WS_EX_APPWINDOW, false);
    
    // 2. Hide from DWM peek and thumbnails
    BOOL exclude = TRUE;
    DwmSetWindowAttribute(hwnd, DWMWA_EXCLUDED_FROM_PEEK, &exclude, sizeof(exclude));
    DwmSetWindowAttribute(hwnd, DWMWA_DISALLOW_PEEK, &exclude, sizeof(exclude));
    
    // 3. Set window to not appear in various system enumerations
    // Note: This doesn't hide from task manager completely, but reduces visibility
    
    stealthWindow = windowHandle;
    isStealthActive = true;
    
    return true;
#else
    std::cout << "EnableInvisibilityMode called (Linux stub)" << std::endl;
    stealthWindow = windowHandle;
    isStealthActive = true;
    return true;
#endif
}

bool StealthManager::DisableInvisibilityMode(void* windowHandle) {
#ifdef _WIN32
    HWND hwnd = static_cast<HWND>(windowHandle);
    if (!hwnd) return false;

    // Restore normal visibility
    SetWindowExStyle(hwnd, WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE, false);
    SetWindowExStyle(hwnd, WS_EX_APPWINDOW, true);
    
    BOOL exclude = FALSE;
    DwmSetWindowAttribute(hwnd, DWMWA_EXCLUDED_FROM_PEEK, &exclude, sizeof(exclude));
    DwmSetWindowAttribute(hwnd, DWMWA_DISALLOW_PEEK, &exclude, sizeof(exclude));
    
    stealthWindow = nullptr;
    isStealthActive = false;
    
    return true;
#else
    (void)windowHandle; // Suppress unused parameter warning
    std::cout << "DisableInvisibilityMode called (Linux stub)" << std::endl;
    stealthWindow = nullptr;
    isStealthActive = false;
    return true;
#endif
}

bool StealthManager::SetAsToolWindow(void* windowHandle, bool enable) {
#ifdef _WIN32
    HWND hwnd = static_cast<HWND>(windowHandle);
    if (!hwnd) return false;

    return SetWindowExStyle(hwnd, WS_EX_TOOLWINDOW, enable);
#else
    (void)windowHandle; (void)enable; // Suppress unused parameter warnings
    std::cout << "SetAsToolWindow called (Linux stub): " << (enable ? "enable" : "disable") << std::endl;
    return true;
#endif
}

#ifdef _WIN32
bool StealthManager::HideFromTaskManager(DWORD processId) {
    // Note: Completely hiding from Task Manager requires kernel-level techniques
    // which are not recommended for legitimate applications.
    // This function is a placeholder for educational purposes.
    
    // In a real-world scenario, you might:
    // 1. Use legitimate techniques like running as a service
    // 2. Minimize system resource usage to be less noticeable
    // 3. Use descriptive process names that look like system processes
    
    std::cout << "HideFromTaskManager called for PID: " << processId << std::endl;
    std::cout << "Note: Complete task manager hiding requires elevated privileges" << std::endl;
    
    return false; // Not implemented for security reasons
}

bool StealthManager::SetWindowExStyle(HWND hwnd, DWORD exStyle, bool add) {
    if (!hwnd) return false;

    LONG_PTR currentStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
    LONG_PTR newStyle;
    
    if (add) {
        newStyle = currentStyle | exStyle;
    } else {
        newStyle = currentStyle & ~exStyle;
    }
    
    SetWindowLongPtr(hwnd, GWL_EXSTYLE, newStyle);
    
    // Force window to update its appearance
    SetWindowPos(hwnd, nullptr, 0, 0, 0, 0, 
                 SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    
    return true;
}
#endif

} // namespace Pick6