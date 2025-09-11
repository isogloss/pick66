#pragma once

#include <windows.h>
#include <d3d11.h>
#include <dxgi.h>
#include <memory>
#include <string>

// Pick66 proxy configuration
#define PICK66_CONFIG_FILE L"pick66_config.txt"
#define PICK66_LOG_FILE L"pick66_proxy.log"

// Version information
#define PICK66_VERSION_MAJOR 1
#define PICK66_VERSION_MINOR 0
#define PICK66_VERSION_PATCH 0
#define PICK66_VERSION_STRING "1.0.0"

// ImGui overlay toggle key (Alt+F12)
#define OVERLAY_TOGGLE_KEY VK_F12
#define OVERLAY_MODIFIER_KEY VK_MENU

namespace Pick66
{
    /// <summary>
    /// Configuration loaded from pick66_config.txt
    /// </summary>
    struct ProxyConfig
    {
        bool OverlayEnabled = true;
        bool LoggingEnabled = false;
        int OverlayToggleKey = VK_F12;
        int OverlayModifierKey = VK_MENU;
    };

    /// <summary>
    /// Shared proxy functionality
    /// </summary>
    class ProxyCommon
    {
    public:
        static ProxyConfig LoadConfig();
        static void Log(const std::wstring& message);
        static void LogError(const std::wstring& message);
        static std::wstring GetModuleDirectory();
        static bool IsOverlayTogglePressed();
        
    private:
        static ProxyConfig s_config;
        static bool s_configLoaded;
        static HANDLE s_logFile;
    };

    /// <summary>
    /// ImGui overlay manager
    /// </summary>
    class OverlayManager
    {
    public:
        static OverlayManager& Instance();
        
        bool Initialize(ID3D11Device* device, ID3D11DeviceContext* context);
        void Shutdown();
        void NewFrame();
        void Render();
        void Present();
        bool IsVisible() const { return m_visible; }
        void SetVisible(bool visible) { m_visible = visible; }
        void Toggle() { m_visible = !m_visible; }

        // Window procedure for input handling
        LRESULT WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
        
    private:
        OverlayManager() = default;
        ~OverlayManager() = default;
        
        void RenderUI();
        void UpdateStats();
        
        bool m_initialized = false;
        bool m_visible = false;
        ID3D11Device* m_device = nullptr;
        ID3D11DeviceContext* m_context = nullptr;
        
        // Statistics
        float m_frameTime = 0.0f;
        float m_fps = 0.0f;
        size_t m_frameCount = 0;
        LARGE_INTEGER m_lastFrameTime;
        LARGE_INTEGER m_frequency;
    };

    /// <summary>
    /// D3D11 hook manager for frame capture and overlay
    /// </summary>
    class D3D11HookManager
    {
    public:
        static D3D11HookManager& Instance();
        
        bool Initialize();
        void Shutdown();
        
        // Hook points
        void OnDeviceCreated(ID3D11Device* device, ID3D11DeviceContext* context);
        void OnSwapChainPresent(IDXGISwapChain* swapChain);
        void OnBeforePresent(IDXGISwapChain* swapChain);
        void OnAfterPresent(IDXGISwapChain* swapChain);
        
    private:
        D3D11HookManager() = default;
        ~D3D11HookManager() = default;
        
        bool m_initialized = false;
        bool m_overlayInitialized = false;
        ID3D11Device* m_device = nullptr;
        ID3D11DeviceContext* m_context = nullptr;
        OverlayManager* m_overlayManager = nullptr;
        
        // Input handling
        WNDPROC m_originalWndProc = nullptr;
        HWND m_targetWindow = nullptr;
        
        static LRESULT CALLBACK WndProcHook(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
        void InstallInputHook(HWND hwnd);
        void RemoveInputHook();
    };
}

// Helper macros for COM interface forwarding
#define FORWARD_INTERFACE_METHOD(method, ...) \
    return m_original->method(__VA_ARGS__)

#define FORWARD_INTERFACE_METHOD_VOID(method, ...) \
    m_original->method(__VA_ARGS__)

// Logging macros
#define PICK66_LOG(msg) Pick66::ProxyCommon::Log(msg)
#define PICK66_LOG_ERROR(msg) Pick66::ProxyCommon::LogError(msg)

#ifdef _DEBUG
#define PICK66_DEBUG_LOG(msg) PICK66_LOG(L"[DEBUG] " msg)
#else
#define PICK66_DEBUG_LOG(msg) ((void)0)
#endif