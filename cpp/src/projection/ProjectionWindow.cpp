#include "ProjectionWindow.h"
#include "StealthManager.h"
#include <iostream>
#include <mutex>

#ifdef _WIN32
#include <windows.h>
#include <dwmapi.h>
#include <d3d11.h>
#include <dxgi.h>

#pragma comment(lib, "d3d11.lib")
#pragma comment(lib, "dxgi.lib")
#pragma comment(lib, "dwmapi.lib")
#endif

#ifdef HAVE_GTK3
#include <gtk/gtk.h>
#include <cairo.h>
#endif

namespace Pick6 {

#ifdef _WIN32
class ProjectionWindow::Impl {
public:
    HWND hwnd = nullptr;
    bool visible = false;
    bool fullscreen = false;
    bool borderless = true;
    bool topmost = true;
    bool stealthEnabled = false;
    int monitorIndex = 0;
    
    // DirectX 11 components for rendering
    ID3D11Device* d3dDevice = nullptr;
    ID3D11DeviceContext* d3dContext = nullptr;
    IDXGISwapChain* swapChain = nullptr;
    ID3D11RenderTargetView* renderTargetView = nullptr;
    ID3D11Texture2D* frameTexture = nullptr;
    
    FrameData currentFrame;
    std::mutex frameMutex;

    static LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
        ProjectionWindow::Impl* impl = nullptr;
        
        if (uMsg == WM_NCCREATE) {
            CREATESTRUCT* pCreate = reinterpret_cast<CREATESTRUCT*>(lParam);
            impl = reinterpret_cast<ProjectionWindow::Impl*>(pCreate->lpCreateParams);
            SetWindowLongPtr(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(impl));
        } else {
            impl = reinterpret_cast<ProjectionWindow::Impl*>(GetWindowLongPtr(hwnd, GWLP_USERDATA));
        }

        if (impl) {
            return impl->HandleMessage(hwnd, uMsg, wParam, lParam);
        }

        return DefWindowProc(hwnd, uMsg, wParam, lParam);
    }

    LRESULT HandleMessage(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
        switch (uMsg) {
        case WM_CREATE:
            InitializeDirectX();
            return 0;

        case WM_PAINT:
            RenderFrame();
            return 0;

        case WM_SIZE:
            if (swapChain) {
                ResizeBuffers();
            }
            return 0;

        case WM_KEYDOWN:
            if (wParam == VK_ESCAPE) {
                ShowWindow(hwnd, SW_HIDE);
            }
            return 0;

        case WM_DESTROY:
            CleanupDirectX();
            return 0;
        }

        return DefWindowProc(hwnd, uMsg, wParam, lParam);
    }

    bool CreateProjectionWindow() {
        const char* CLASS_NAME = "Pick6ProjectionWindow";
        
        WNDCLASS wc = {};
        wc.lpfnWndProc = ProjectionWindow::Impl::WindowProc;
        wc.hInstance = GetModuleHandle(nullptr);
        wc.lpszClassName = CLASS_NAME;
        wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
        wc.hbrBackground = CreateSolidBrush(RGB(0, 0, 0)); // Black background

        RegisterClass(&wc);

        // Get monitor dimensions
        RECT monitorRect = GetMonitorRect(monitorIndex);
        
        DWORD dwStyle = WS_POPUP; // Borderless
        DWORD dwExStyle = WS_EX_NOACTIVATE | WS_EX_LAYERED;
        
        if (topmost) {
            dwExStyle |= WS_EX_TOPMOST;
        }

        hwnd = CreateWindowEx(
            dwExStyle,
            CLASS_NAME,
            "Pick6 Projection",
            dwStyle,
            monitorRect.left, monitorRect.top,
            monitorRect.right - monitorRect.left,
            monitorRect.bottom - monitorRect.top,
            nullptr, nullptr, GetModuleHandle(nullptr), this);

        if (!hwnd) {
            return false;
        }

        // Make window transparent initially
        SetLayeredWindowAttributes(hwnd, RGB(0, 0, 0), 255, LWA_ALPHA);

        return true;
    }

    RECT GetMonitorRect(int index) {
        RECT rect = {0, 0, 1920, 1080}; // Default fallback
        
        // Get the specified monitor
        DISPLAY_DEVICE dd = {};
        dd.cb = sizeof(dd);
        
        if (EnumDisplayDevices(nullptr, index, &dd, 0)) {
            DEVMODE dm = {};
            dm.dmSize = sizeof(dm);
            
            if (EnumDisplaySettings(dd.DeviceName, ENUM_CURRENT_SETTINGS, &dm)) {
                rect.left = dm.dmPosition.x;
                rect.top = dm.dmPosition.y;
                rect.right = dm.dmPosition.x + dm.dmPelsWidth;
                rect.bottom = dm.dmPosition.y + dm.dmPelsHeight;
            }
        }
        
        return rect;
    }

    void InitializeDirectX() {
        DXGI_SWAP_CHAIN_DESC scd = {};
        scd.BufferCount = 1;
        scd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
        scd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
        scd.OutputWindow = hwnd;
        scd.SampleDesc.Count = 1;
        scd.Windowed = TRUE;

        D3D11CreateDeviceAndSwapChain(
            nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0, nullptr, 0,
            D3D11_SDK_VERSION, &scd, &swapChain, &d3dDevice, nullptr, &d3dContext);

        if (swapChain) {
            ID3D11Texture2D* backBuffer;
            swapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&backBuffer);
            d3dDevice->CreateRenderTargetView(backBuffer, nullptr, &renderTargetView);
            backBuffer->Release();
        }
    }

    void CleanupDirectX() {
        if (renderTargetView) renderTargetView->Release();
        if (frameTexture) frameTexture->Release();
        if (swapChain) swapChain->Release();
        if (d3dContext) d3dContext->Release();
        if (d3dDevice) d3dDevice->Release();
    }

    void ResizeBuffers() {
        if (renderTargetView) {
            renderTargetView->Release();
            renderTargetView = nullptr;
        }

        RECT clientRect;
        GetClientRect(hwnd, &clientRect);
        swapChain->ResizeBuffers(0, clientRect.right, clientRect.bottom, DXGI_FORMAT_UNKNOWN, 0);

        ID3D11Texture2D* backBuffer;
        swapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&backBuffer);
        d3dDevice->CreateRenderTargetView(backBuffer, nullptr, &renderTargetView);
        backBuffer->Release();
    }

    void RenderFrame() {
        if (!d3dContext || !renderTargetView) return;

        std::lock_guard<std::mutex> lock(frameMutex);
        
        // Clear the back buffer
        float clearColor[4] = {0.0f, 0.0f, 0.0f, 1.0f};
        d3dContext->ClearRenderTargetView(renderTargetView, clearColor);

        if (!currentFrame.data.empty()) {
            // In a real implementation, we would:
            // 1. Create or update a texture with the frame data
            // 2. Render it as a fullscreen quad
            // 3. Handle aspect ratio and scaling
            
            // For now, just present the cleared buffer
            // TODO: Implement actual frame rendering
        }

        swapChain->Present(1, 0); // VSync enabled
    }

    void EnableStealthMode() {
        if (!hwnd) return;

        // Hide from Alt+Tab
        StealthManager::HideFromAltTab(hwnd);
        
        // Hide from taskbar
        StealthManager::HideFromTaskbar(hwnd);
        
        // Set as tool window for additional stealth
        StealthManager::SetAsToolWindow(hwnd, true);
        
        stealthEnabled = true;
    }

    void DisableStealthMode() {
        if (!hwnd) return;

        StealthManager::ShowInAltTab(hwnd);
        StealthManager::ShowInTaskbar(hwnd);
        StealthManager::SetAsToolWindow(hwnd, false);
        
        stealthEnabled = false;
    }
};

#else
#ifdef HAVE_GTK3
// Linux/GTK implementation for development
class ProjectionWindow::Impl {
public:
    GtkWidget* window = nullptr;
    GtkWidget* drawingArea = nullptr;
    bool visible = false;
    bool fullscreen = false;
    bool stealthEnabled = false;
    FrameData currentFrame;
    std::mutex frameMutex;

    bool CreateProjectionWindow() {
        window = gtk_window_new(GTK_WINDOW_TOPLEVEL);
        gtk_window_set_title(GTK_WINDOW(window), "Pick6 Projection");
        gtk_window_set_decorated(GTK_WINDOW(window), FALSE); // Borderless
        gtk_window_set_default_size(GTK_WINDOW(window), 1920, 1080);
        
        drawingArea = gtk_drawing_area_new();
        gtk_container_add(GTK_CONTAINER(window), drawingArea);
        
        g_signal_connect(window, "key-press-event", G_CALLBACK(OnKeyPress), this);
        g_signal_connect(drawingArea, "draw", G_CALLBACK(OnDraw), this);
        
        return true;
    }

    static gboolean OnKeyPress(GtkWidget* widget, GdkEventKey* event, gpointer data) {
        if (event->keyval == GDK_KEY_Escape) {
            auto impl = static_cast<Impl*>(data);
            gtk_widget_hide(impl->window);
            return TRUE;
        }
        return FALSE;
    }

    static gboolean OnDraw(GtkWidget* widget, cairo_t* cr, gpointer data) {
        auto impl = static_cast<Impl*>(data);
        std::lock_guard<std::mutex> lock(impl->frameMutex);
        
        // Clear with black
        cairo_set_source_rgb(cr, 0, 0, 0);
        cairo_paint(cr);
        
        if (!impl->currentFrame.data.empty()) {
            // In a real implementation, we would render the frame data here
            // For now, just draw a colored rectangle as a placeholder
            cairo_set_source_rgb(cr, 1, 0, 0); // Red placeholder
            cairo_rectangle(cr, 100, 100, 200, 150);
            cairo_fill(cr);
        }
        
        return FALSE;
    }
};
#else
// Fallback implementation without GUI
class ProjectionWindow::Impl {
public:
    bool visible = false;
    bool fullscreen = false;
    bool borderless = true;
    bool topmost = true;
    bool stealthEnabled = false;
    int monitorIndex = 0;
    FrameData currentFrame;
    std::mutex frameMutex;

    bool CreateProjectionWindow() {
        std::cout << "Projection window created (console mode)" << std::endl;
        return true;
    }
};
#endif
#endif

ProjectionWindow::ProjectionWindow() : pImpl(std::make_unique<Impl>()) {}

ProjectionWindow::~ProjectionWindow() = default;

bool ProjectionWindow::Initialize() {
    return pImpl->CreateProjectionWindow();
}

void ProjectionWindow::Show() {
    if (!pImpl->visible) {
#ifdef _WIN32
        ShowWindow(pImpl->hwnd, SW_SHOWNOACTIVATE);
        if (pImpl->fullscreen) {
            SetWindowPos(pImpl->hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }
#else
#ifdef HAVE_GTK3
        gtk_widget_show_all(pImpl->window);
        if (pImpl->fullscreen) {
            gtk_window_fullscreen(GTK_WINDOW(pImpl->window));
        }
#else
        std::cout << "Projection window shown (console mode)" << std::endl;
#endif
#endif
        pImpl->visible = true;
    }
}

void ProjectionWindow::Hide() {
    if (pImpl->visible) {
#ifdef _WIN32
        ShowWindow(pImpl->hwnd, SW_HIDE);
#else
#ifdef HAVE_GTK3
        gtk_widget_hide(pImpl->window);
#else
        std::cout << "Projection window hidden (console mode)" << std::endl;
#endif
#endif
        pImpl->visible = false;
    }
}

bool ProjectionWindow::IsVisible() const {
    return pImpl->visible;
}

void ProjectionWindow::UpdateFrame(const FrameData& frame) {
    std::lock_guard<std::mutex> lock(pImpl->frameMutex);
    pImpl->currentFrame = frame;
    
#ifdef _WIN32
    if (pImpl->hwnd && pImpl->visible) {
        InvalidateRect(pImpl->hwnd, nullptr, FALSE);
    }
#else
#ifdef HAVE_GTK3
    if (pImpl->drawingArea && pImpl->visible) {
        gtk_widget_queue_draw(pImpl->drawingArea);
    }
#else
    static int frameCount = 0;
    if (++frameCount % 60 == 0) { // Log every 60 frames
        std::cout << "Frame updated: " << frame.width << "x" << frame.height 
                  << " (frame " << frameCount << ")" << std::endl;
    }
#endif
#endif
}

void ProjectionWindow::SetMonitor(int monitorIndex) {
    pImpl->monitorIndex = monitorIndex;
}

void ProjectionWindow::SetBorderless(bool borderless) {
    pImpl->borderless = borderless;
    // Would need to recreate window or update window style
}

void ProjectionWindow::SetTopmost(bool topmost) {
    pImpl->topmost = topmost;
#ifdef _WIN32
    if (pImpl->hwnd) {
        SetWindowPos(pImpl->hwnd, topmost ? HWND_TOPMOST : HWND_NOTOPMOST, 
                     0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }
#endif
}

void ProjectionWindow::EnableStealth(bool enabled) {
#ifdef _WIN32
    if (enabled) {
        pImpl->EnableStealthMode();
    } else {
        pImpl->DisableStealthMode();
    }
#endif
    pImpl->stealthEnabled = enabled;
}

bool ProjectionWindow::IsStealthEnabled() const {
    return pImpl->stealthEnabled;
}

void ProjectionWindow::SetFullscreen(bool fullscreen) {
    pImpl->fullscreen = fullscreen;
#ifdef _WIN32
    if (pImpl->hwnd) {
        if (fullscreen) {
            RECT monitorRect = pImpl->GetMonitorRect(pImpl->monitorIndex);
            SetWindowPos(pImpl->hwnd, HWND_TOPMOST,
                         monitorRect.left, monitorRect.top,
                         monitorRect.right - monitorRect.left,
                         monitorRect.bottom - monitorRect.top,
                         SWP_SHOWWINDOW);
        }
    }
#else
#ifdef HAVE_GTK3
    if (pImpl->window) {
        if (fullscreen) {
            gtk_window_fullscreen(GTK_WINDOW(pImpl->window));
        } else {
            gtk_window_unfullscreen(GTK_WINDOW(pImpl->window));
        }
    }
#else
    std::cout << "Fullscreen: " << (fullscreen ? "enabled" : "disabled") << std::endl;
#endif
#endif
}

bool ProjectionWindow::IsFullscreen() const {
    return pImpl->fullscreen;
}

} // namespace Pick6