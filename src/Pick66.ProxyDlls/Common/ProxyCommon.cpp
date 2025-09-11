#include "ProxyCommon.h"
#include <fstream>
#include <sstream>
#include <filesystem>
#include <chrono>
#include <iomanip>

// Include ImGui
#include "imgui.h"
#include "imgui_impl_win32.h"
#include "imgui_impl_dx11.h"

#pragma comment(lib, "d3d11.lib")
#pragma comment(lib, "dxgi.lib")

namespace Pick66
{
    // Static member definitions
    ProxyConfig ProxyCommon::s_config;
    bool ProxyCommon::s_configLoaded = false;
    HANDLE ProxyCommon::s_logFile = INVALID_HANDLE_VALUE;

    ProxyConfig ProxyCommon::LoadConfig()
    {
        if (s_configLoaded)
            return s_config;

        s_config = ProxyConfig(); // Default values
        s_configLoaded = true;

        try
        {
            auto moduleDir = GetModuleDirectory();
            auto configPath = moduleDir + L"\\" + PICK66_CONFIG_FILE;

            std::wifstream file(configPath);
            if (!file.is_open())
            {
                Log(L"Config file not found, using defaults");
                return s_config;
            }

            std::wstring line;
            while (std::getline(file, line))
            {
                auto pos = line.find(L'=');
                if (pos == std::wstring::npos)
                    continue;

                auto key = line.substr(0, pos);
                auto value = line.substr(pos + 1);

                if (key == L"OverlayEnabled")
                {
                    s_config.OverlayEnabled = (value == L"true" || value == L"True");
                }
                else if (key == L"LoggingEnabled")
                {
                    s_config.LoggingEnabled = (value == L"true" || value == L"True");
                }
            }

            Log(L"Configuration loaded successfully");
        }
        catch (const std::exception&)
        {
            Log(L"Error loading configuration, using defaults");
        }

        return s_config;
    }

    void ProxyCommon::Log(const std::wstring& message)
    {
        auto config = LoadConfig();
        if (!config.LoggingEnabled)
            return;

        try
        {
            if (s_logFile == INVALID_HANDLE_VALUE)
            {
                auto moduleDir = GetModuleDirectory();
                auto logPath = moduleDir + L"\\" + PICK66_LOG_FILE;
                
                s_logFile = CreateFileW(logPath.c_str(), GENERIC_WRITE, FILE_SHARE_READ,
                    nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
            }

            if (s_logFile != INVALID_HANDLE_VALUE)
            {
                auto now = std::chrono::system_clock::now();
                auto time_t = std::chrono::system_clock::to_time_t(now);
                
                std::wstringstream ss;
                ss << std::put_time(std::localtime(&time_t), L"[%Y-%m-%d %H:%M:%S] ");
                ss << message << L"\r\n";
                
                auto logLine = ss.str();
                DWORD bytesWritten;
                WriteFile(s_logFile, logLine.c_str(), static_cast<DWORD>(logLine.length() * sizeof(wchar_t)),
                    &bytesWritten, nullptr);
                FlushFileBuffers(s_logFile);
            }
        }
        catch (...)
        {
            // Ignore logging errors
        }
    }

    void ProxyCommon::LogError(const std::wstring& message)
    {
        Log(L"[ERROR] " + message);
    }

    std::wstring ProxyCommon::GetModuleDirectory()
    {
        wchar_t path[MAX_PATH];
        HMODULE hModule = nullptr;
        
        if (GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |
                              GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                              (LPCWSTR)&GetModuleDirectory, &hModule))
        {
            GetModuleFileNameW(hModule, path, MAX_PATH);
            std::filesystem::path p(path);
            return p.parent_path().wstring();
        }
        
        return L".";
    }

    bool ProxyCommon::IsOverlayTogglePressed()
    {
        static bool wasPressed = false;
        bool isPressed = (GetAsyncKeyState(OVERLAY_MODIFIER_KEY) & 0x8000) &&
                        (GetAsyncKeyState(OVERLAY_TOGGLE_KEY) & 0x8000);
        
        if (isPressed && !wasPressed)
        {
            wasPressed = true;
            return true;
        }
        
        if (!isPressed)
            wasPressed = false;
            
        return false;
    }

    // OverlayManager implementation
    OverlayManager& OverlayManager::Instance()
    {
        static OverlayManager instance;
        return instance;
    }

    bool OverlayManager::Initialize(ID3D11Device* device, ID3D11DeviceContext* context)
    {
        if (m_initialized)
            return true;

        if (!device || !context)
            return false;

        m_device = device;
        m_context = context;

        // Setup ImGui
        IMGUI_CHECKVERSION();
        ImGui::CreateContext();
        ImGuiIO& io = ImGui::GetIO();
        io.ConfigFlags |= ImGuiConfigFlags_NoMouseCursorChange;
        
        // Setup style
        ImGui::StyleColorsDark();
        
        // Get swap chain from device to get window handle
        IDXGIDevice* dxgiDevice = nullptr;
        IDXGIAdapter* dxgiAdapter = nullptr;
        IDXGIFactory* dxgiFactory = nullptr;
        HWND hwnd = nullptr;
        
        if (SUCCEEDED(device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgiDevice)))
        {
            if (SUCCEEDED(dxgiDevice->GetAdapter(&dxgiAdapter)))
            {
                if (SUCCEEDED(dxgiAdapter->GetParent(__uuidof(IDXGIFactory), (void**)&dxgiFactory)))
                {
                    // This is a simplified approach - in practice, we'd need to get the actual window handle
                    hwnd = GetActiveWindow();
                }
                dxgiAdapter->Release();
            }
            dxgiDevice->Release();
        }
        if (dxgiFactory) dxgiFactory->Release();

        if (!hwnd)
        {
            PICK66_LOG_ERROR(L"Failed to get window handle for ImGui");
            return false;
        }

        // Initialize ImGui backends
        if (!ImGui_ImplWin32_Init(hwnd))
        {
            PICK66_LOG_ERROR(L"Failed to initialize ImGui Win32 backend");
            return false;
        }

        if (!ImGui_ImplDX11_Init(device, context))
        {
            PICK66_LOG_ERROR(L"Failed to initialize ImGui DX11 backend");
            ImGui_ImplWin32_Shutdown();
            return false;
        }

        // Initialize timing
        QueryPerformanceFrequency(&m_frequency);
        QueryPerformanceCounter(&m_lastFrameTime);

        m_initialized = true;
        PICK66_LOG(L"OverlayManager initialized successfully");
        return true;
    }

    void OverlayManager::Shutdown()
    {
        if (!m_initialized)
            return;

        ImGui_ImplDX11_Shutdown();
        ImGui_ImplWin32_Shutdown();
        ImGui::DestroyContext();

        m_initialized = false;
        m_device = nullptr;
        m_context = nullptr;
        
        PICK66_LOG(L"OverlayManager shutdown");
    }

    void OverlayManager::NewFrame()
    {
        if (!m_initialized)
            return;

        UpdateStats();

        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();
        ImGui::NewFrame();
    }

    void OverlayManager::Render()
    {
        if (!m_initialized || !m_visible)
            return;

        RenderUI();
        ImGui::Render();
    }

    void OverlayManager::Present()
    {
        if (!m_initialized || !m_visible)
            return;

        ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());
    }

    void OverlayManager::RenderUI()
    {
        ImGui::SetNextWindowPos(ImVec2(50, 50), ImGuiCond_FirstUseEver);
        ImGui::SetNextWindowSize(ImVec2(400, 300), ImGuiCond_FirstUseEver);

        if (ImGui::Begin("Pick66 Overlay", &m_visible, ImGuiWindowFlags_AlwaysAutoResize))
        {
            ImGui::Text("Pick66 D3D11/DXGI Proxy");
            ImGui::Separator();

            // Performance metrics
            ImGui::Text("Performance:");
            ImGui::Text("  FPS: %.1f", m_fps);
            ImGui::Text("  Frame Time: %.3f ms", m_frameTime);
            ImGui::Text("  Frame Count: %zu", m_frameCount);
            
            ImGui::Separator();
            
            // Proxy information
            ImGui::Text("Proxy Information:");
            ImGui::Text("  Version: %s", PICK66_VERSION_STRING);
            ImGui::Text("  Overlay Enabled: Yes");
            
            ImGui::Separator();
            
            // Controls
            ImGui::Text("Controls:");
            ImGui::Text("  Alt+F12: Toggle this overlay");
            
            if (ImGui::Button("Close Overlay"))
            {
                m_visible = false;
            }
        }
        ImGui::End();
    }

    void OverlayManager::UpdateStats()
    {
        m_frameCount++;
        
        LARGE_INTEGER currentTime;
        QueryPerformanceCounter(&currentTime);
        
        double deltaTime = static_cast<double>(currentTime.QuadPart - m_lastFrameTime.QuadPart) / m_frequency.QuadPart;
        m_frameTime = static_cast<float>(deltaTime * 1000.0); // Convert to milliseconds
        m_fps = 1.0f / static_cast<float>(deltaTime);
        
        m_lastFrameTime = currentTime;
    }

    LRESULT OverlayManager::WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
    {
        if (m_initialized && m_visible)
        {
            extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
            if (ImGui_ImplWin32_WndProcHandler(hwnd, msg, wParam, lParam))
                return true;
        }
        
        return 0; // Let the original window procedure handle it
    }

    // D3D11HookManager implementation
    D3D11HookManager& D3D11HookManager::Instance()
    {
        static D3D11HookManager instance;
        return instance;
    }

    bool D3D11HookManager::Initialize()
    {
        if (m_initialized)
            return true;

        PICK66_LOG(L"D3D11HookManager initializing...");
        
        m_overlayManager = &OverlayManager::Instance();
        m_initialized = true;
        
        PICK66_LOG(L"D3D11HookManager initialized");
        return true;
    }

    void D3D11HookManager::Shutdown()
    {
        if (!m_initialized)
            return;

        RemoveInputHook();
        
        if (m_overlayManager)
        {
            m_overlayManager->Shutdown();
            m_overlayManager = nullptr;
        }

        m_device = nullptr;
        m_context = nullptr;
        m_initialized = false;
        
        PICK66_LOG(L"D3D11HookManager shutdown");
    }

    void D3D11HookManager::OnDeviceCreated(ID3D11Device* device, ID3D11DeviceContext* context)
    {
        if (!m_initialized || !device || !context)
            return;

        m_device = device;
        m_context = context;

        auto config = ProxyCommon::LoadConfig();
        if (config.OverlayEnabled && m_overlayManager && !m_overlayInitialized)
        {
            if (m_overlayManager->Initialize(device, context))
            {
                m_overlayInitialized = true;
                PICK66_LOG(L"Overlay initialized on device creation");
            }
        }
    }

    void D3D11HookManager::OnSwapChainPresent(IDXGISwapChain* swapChain)
    {
        if (!m_initialized)
            return;

        OnBeforePresent(swapChain);
        // Present happens in the actual proxy
        OnAfterPresent(swapChain);
    }

    void D3D11HookManager::OnBeforePresent(IDXGISwapChain* swapChain)
    {
        if (!m_initialized || !m_overlayManager || !m_overlayInitialized)
            return;

        // Check for overlay toggle
        if (ProxyCommon::IsOverlayTogglePressed())
        {
            m_overlayManager->Toggle();
            PICK66_DEBUG_LOG(m_overlayManager->IsVisible() ? L"Overlay shown" : L"Overlay hidden");
        }

        // Install input hook if we haven't already
        if (!m_originalWndProc && swapChain)
        {
            DXGI_SWAP_CHAIN_DESC desc;
            if (SUCCEEDED(swapChain->GetDesc(&desc)) && desc.OutputWindow)
            {
                InstallInputHook(desc.OutputWindow);
            }
        }

        m_overlayManager->NewFrame();
        m_overlayManager->Render();
    }

    void D3D11HookManager::OnAfterPresent(IDXGISwapChain* swapChain)
    {
        if (!m_initialized || !m_overlayManager || !m_overlayInitialized)
            return;

        m_overlayManager->Present();
    }

    void D3D11HookManager::InstallInputHook(HWND hwnd)
    {
        if (m_originalWndProc || !hwnd)
            return;

        m_targetWindow = hwnd;
        m_originalWndProc = (WNDPROC)SetWindowLongPtrW(hwnd, GWLP_WNDPROC, (LONG_PTR)WndProcHook);
        
        if (m_originalWndProc)
        {
            PICK66_LOG(L"Input hook installed");
        }
        else
        {
            PICK66_LOG_ERROR(L"Failed to install input hook");
        }
    }

    void D3D11HookManager::RemoveInputHook()
    {
        if (m_originalWndProc && m_targetWindow)
        {
            SetWindowLongPtrW(m_targetWindow, GWLP_WNDPROC, (LONG_PTR)m_originalWndProc);
            m_originalWndProc = nullptr;
            m_targetWindow = nullptr;
            PICK66_LOG(L"Input hook removed");
        }
    }

    LRESULT CALLBACK D3D11HookManager::WndProcHook(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
    {
        auto& instance = Instance();
        
        if (instance.m_overlayManager)
        {
            auto result = instance.m_overlayManager->WndProc(hwnd, msg, wParam, lParam);
            if (result != 0)
                return result;
        }

        return CallWindowProcW(instance.m_originalWndProc, hwnd, msg, wParam, lParam);
    }
}