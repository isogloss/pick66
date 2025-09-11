#include "MainWindow.h"
#include <iostream>
#include <sstream>

#ifdef _WIN32
#include <windows.h>
#include <commctrl.h>
#include <windowsx.h>

// Windows Control IDs
#define ID_START_INJECTION 1001
#define ID_STOP_INJECTION 1002
#define ID_SHOW_PROJECTION 1003
#define ID_HIDE_PROJECTION 1004
#define ID_SETTINGS 1005
#define ID_AUTO_PROJECTION 1006
#define ID_FPS_SLIDER 1007
#define ID_STATUS_TEXT 1008
#define ID_PROCESS_TEXT 1009
#define ID_CAPTURE_TEXT 1010

#else
#ifdef HAVE_GTK3
#include <gtk/gtk.h>
#endif
#endif

namespace Pick6 {

#ifdef _WIN32
class MainWindow::Impl {
public:
    HWND hwnd = nullptr;
    HWND hStartButton = nullptr;
    HWND hStopButton = nullptr;
    HWND hShowProjectionButton = nullptr;
    HWND hHideProjectionButton = nullptr;
    HWND hSettingsButton = nullptr;
    HWND hAutoProjectionCheck = nullptr;
    HWND hFpsSlider = nullptr;
    HWND hStatusText = nullptr;
    HWND hProcessText = nullptr;
    HWND hCaptureText = nullptr;

    StartInjectionCallback startInjectionCallback;
    StopInjectionCallback stopInjectionCallback;
    ShowProjectionCallback showProjectionCallback;
    HideProjectionCallback hideProjectionCallback;
    SettingsCallback settingsCallback;

    bool autoProjection = true;
    int targetFPS = 60;
    std::shared_ptr<KeybindManager> keybindManager;

    static LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
        MainWindow::Impl* impl = nullptr;
        
        if (uMsg == WM_NCCREATE) {
            CREATESTRUCT* pCreate = reinterpret_cast<CREATESTRUCT*>(lParam);
            impl = reinterpret_cast<MainWindow::Impl*>(pCreate->lpCreateParams);
            SetWindowLongPtr(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(impl));
        } else {
            impl = reinterpret_cast<MainWindow::Impl*>(GetWindowLongPtr(hwnd, GWLP_USERDATA));
        }

        if (impl) {
            return impl->HandleMessage(hwnd, uMsg, wParam, lParam);
        }

        return DefWindowProc(hwnd, uMsg, wParam, lParam);
    }

    LRESULT HandleMessage(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
        switch (uMsg) {
        case WM_CREATE:
            CreateControls(hwnd);
            return 0;

        case WM_COMMAND:
            HandleCommand(LOWORD(wParam));
            return 0;

        case WM_HSCROLL:
            if (reinterpret_cast<HWND>(lParam) == hFpsSlider) {
                targetFPS = SendMessage(hFpsSlider, TBM_GETPOS, 0, 0);
                UpdateFpsDisplay();
            }
            return 0;

        case WM_DESTROY:
            PostQuitMessage(0);
            return 0;
        }

        return DefWindowProc(hwnd, uMsg, wParam, lParam);
    }

    void CreateControls(HWND parent) {
        // Start Injection Button
        hStartButton = CreateWindow(
            "BUTTON", "Start Injection",
            WS_TABSTOP | WS_VISIBLE | WS_CHILD | BS_DEFPUSHBUTTON,
            20, 20, 120, 40,
            parent, reinterpret_cast<HMENU>(ID_START_INJECTION), nullptr, nullptr);

        // Stop Injection Button
        hStopButton = CreateWindow(
            "BUTTON", "Stop Injection",
            WS_TABSTOP | WS_VISIBLE | WS_CHILD | BS_PUSHBUTTON,
            150, 20, 120, 40,
            parent, reinterpret_cast<HMENU>(ID_STOP_INJECTION), nullptr, nullptr);

        // Show Projection Button
        hShowProjectionButton = CreateWindow(
            "BUTTON", "Show Projection",
            WS_TABSTOP | WS_VISIBLE | WS_CHILD | BS_PUSHBUTTON,
            280, 20, 120, 40,
            parent, reinterpret_cast<HMENU>(ID_SHOW_PROJECTION), nullptr, nullptr);

        // Hide Projection Button
        hHideProjectionButton = CreateWindow(
            "BUTTON", "Hide Projection",
            WS_TABSTOP | WS_VISIBLE | WS_CHILD | BS_PUSHBUTTON,
            410, 20, 120, 40,
            parent, reinterpret_cast<HMENU>(ID_HIDE_PROJECTION), nullptr, nullptr);

        // Auto Projection Checkbox
        hAutoProjectionCheck = CreateWindow(
            "BUTTON", "Auto-start projection",
            WS_TABSTOP | WS_VISIBLE | WS_CHILD | BS_AUTOCHECKBOX,
            20, 80, 200, 20,
            parent, reinterpret_cast<HMENU>(ID_AUTO_PROJECTION), nullptr, nullptr);
        
        SendMessage(hAutoProjectionCheck, BM_SETCHECK, autoProjection ? BST_CHECKED : BST_UNCHECKED, 0);

        // FPS Slider
        CreateWindow(
            "STATIC", "Target FPS:",
            WS_VISIBLE | WS_CHILD,
            20, 110, 80, 20,
            parent, nullptr, nullptr, nullptr);

        hFpsSlider = CreateWindow(
            TRACKBAR_CLASS, "",
            WS_CHILD | WS_VISIBLE | TBS_HORZ | TBS_AUTOTICKS,
            100, 110, 200, 30,
            parent, reinterpret_cast<HMENU>(ID_FPS_SLIDER), nullptr, nullptr);

        SendMessage(hFpsSlider, TBM_SETRANGE, TRUE, MAKELONG(15, 120));
        SendMessage(hFpsSlider, TBM_SETPOS, TRUE, targetFPS);
        SendMessage(hFpsSlider, TBM_SETTICFREQ, 15, 0);

        // Status Labels
        CreateWindow(
            "STATIC", "Status:",
            WS_VISIBLE | WS_CHILD,
            20, 160, 80, 20,
            parent, nullptr, nullptr, nullptr);

        hStatusText = CreateWindow(
            "STATIC", "Ready",
            WS_VISIBLE | WS_CHILD,
            100, 160, 400, 20,
            parent, reinterpret_cast<HMENU>(ID_STATUS_TEXT), nullptr, nullptr);

        CreateWindow(
            "STATIC", "Process:",
            WS_VISIBLE | WS_CHILD,
            20, 180, 80, 20,
            parent, nullptr, nullptr, nullptr);

        hProcessText = CreateWindow(
            "STATIC", "Not monitoring",
            WS_VISIBLE | WS_CHILD,
            100, 180, 400, 20,
            parent, reinterpret_cast<HMENU>(ID_PROCESS_TEXT), nullptr, nullptr);

        CreateWindow(
            "STATIC", "Capture:",
            WS_VISIBLE | WS_CHILD,
            20, 200, 80, 20,
            parent, nullptr, nullptr, nullptr);

        hCaptureText = CreateWindow(
            "STATIC", "Not capturing",
            WS_VISIBLE | WS_CHILD,
            100, 200, 400, 20,
            parent, reinterpret_cast<HMENU>(ID_CAPTURE_TEXT), nullptr, nullptr);

        // Settings Button
        hSettingsButton = CreateWindow(
            "BUTTON", "Keybind Settings",
            WS_TABSTOP | WS_VISIBLE | WS_CHILD | BS_PUSHBUTTON,
            20, 240, 120, 30,
            parent, reinterpret_cast<HMENU>(ID_SETTINGS), nullptr, nullptr);

        UpdateFpsDisplay();
    }

    void HandleCommand(WORD commandId) {
        switch (commandId) {
        case ID_START_INJECTION:
            if (startInjectionCallback) startInjectionCallback();
            break;
        case ID_STOP_INJECTION:
            if (stopInjectionCallback) stopInjectionCallback();
            break;
        case ID_SHOW_PROJECTION:
            if (showProjectionCallback) showProjectionCallback();
            break;
        case ID_HIDE_PROJECTION:
            if (hideProjectionCallback) hideProjectionCallback();
            break;
        case ID_AUTO_PROJECTION:
            autoProjection = SendMessage(hAutoProjectionCheck, BM_GETCHECK, 0, 0) == BST_CHECKED;
            break;
        case ID_SETTINGS:
            if (settingsCallback) settingsCallback();
            break;
        }
    }

    void UpdateFpsDisplay() {
        std::ostringstream oss;
        oss << "Target FPS: " << targetFPS;
        SetWindowText(GetDlgItem(GetParent(hFpsSlider), 0), oss.str().c_str());
    }
};

#else
#ifdef HAVE_GTK3
// GTK implementation for Linux development
class MainWindow::Impl {
public:
    GtkWidget* window = nullptr;
    GtkWidget* startButton = nullptr;
    GtkWidget* stopButton = nullptr;
    GtkWidget* showProjectionButton = nullptr;
    GtkWidget* hideProjectionButton = nullptr;
    GtkWidget* autoProjectionCheck = nullptr;
    GtkWidget* statusLabel = nullptr;
    GtkWidget* processLabel = nullptr;
    GtkWidget* captureLabel = nullptr;

    StartInjectionCallback startInjectionCallback;
    StopInjectionCallback stopInjectionCallback;
    ShowProjectionCallback showProjectionCallback;
    HideProjectionCallback hideProjectionCallback;
    SettingsCallback settingsCallback;

    bool autoProjection = true;
    int targetFPS = 60;
    std::shared_ptr<KeybindManager> keybindManager;

    static void OnStartClicked(GtkWidget* widget, gpointer data) {
        auto impl = static_cast<Impl*>(data);
        if (impl->startInjectionCallback) impl->startInjectionCallback();
    }

    static void OnStopClicked(GtkWidget* widget, gpointer data) {
        auto impl = static_cast<Impl*>(data);
        if (impl->stopInjectionCallback) impl->stopInjectionCallback();
    }

    static void OnShowProjectionClicked(GtkWidget* widget, gpointer data) {
        auto impl = static_cast<Impl*>(data);
        if (impl->showProjectionCallback) impl->showProjectionCallback();
    }

    static void OnHideProjectionClicked(GtkWidget* widget, gpointer data) {
        auto impl = static_cast<Impl*>(data);
        if (impl->hideProjectionCallback) impl->hideProjectionCallback();
    }

    static void OnAutoProjectionToggled(GtkWidget* widget, gpointer data) {
        auto impl = static_cast<Impl*>(data);
        impl->autoProjection = gtk_toggle_button_get_active(GTK_TOGGLE_BUTTON(widget));
    }

    void CreateControls() {
        GtkWidget* vbox = gtk_box_new(GTK_ORIENTATION_VERTICAL, 10);
        gtk_container_add(GTK_CONTAINER(window), vbox);

        // Button row
        GtkWidget* hbox1 = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 10);
        gtk_box_pack_start(GTK_BOX(vbox), hbox1, FALSE, FALSE, 0);

        startButton = gtk_button_new_with_label("Start Injection");
        stopButton = gtk_button_new_with_label("Stop Injection");
        showProjectionButton = gtk_button_new_with_label("Show Projection");
        hideProjectionButton = gtk_button_new_with_label("Hide Projection");

        gtk_box_pack_start(GTK_BOX(hbox1), startButton, TRUE, TRUE, 0);
        gtk_box_pack_start(GTK_BOX(hbox1), stopButton, TRUE, TRUE, 0);
        gtk_box_pack_start(GTK_BOX(hbox1), showProjectionButton, TRUE, TRUE, 0);
        gtk_box_pack_start(GTK_BOX(hbox1), hideProjectionButton, TRUE, TRUE, 0);

        // Auto projection checkbox
        autoProjectionCheck = gtk_check_button_new_with_label("Auto-start projection");
        gtk_toggle_button_set_active(GTK_TOGGLE_BUTTON(autoProjectionCheck), autoProjection);
        gtk_box_pack_start(GTK_BOX(vbox), autoProjectionCheck, FALSE, FALSE, 0);

        // Status labels
        statusLabel = gtk_label_new("Status: Ready");
        processLabel = gtk_label_new("Process: Not monitoring");
        captureLabel = gtk_label_new("Capture: Not capturing");

        gtk_box_pack_start(GTK_BOX(vbox), statusLabel, FALSE, FALSE, 0);
        gtk_box_pack_start(GTK_BOX(vbox), processLabel, FALSE, FALSE, 0);
        gtk_box_pack_start(GTK_BOX(vbox), captureLabel, FALSE, FALSE, 0);

        // Connect signals
        g_signal_connect(startButton, "clicked", G_CALLBACK(OnStartClicked), this);
        g_signal_connect(stopButton, "clicked", G_CALLBACK(OnStopClicked), this);
        g_signal_connect(showProjectionButton, "clicked", G_CALLBACK(OnShowProjectionClicked), this);
        g_signal_connect(hideProjectionButton, "clicked", G_CALLBACK(OnHideProjectionClicked), this);
        g_signal_connect(autoProjectionCheck, "toggled", G_CALLBACK(OnAutoProjectionToggled), this);
    }
};
#else
// Fallback implementation without GUI
class MainWindow::Impl {
public:
    StartInjectionCallback startInjectionCallback;
    StopInjectionCallback stopInjectionCallback;
    ShowProjectionCallback showProjectionCallback;
    HideProjectionCallback hideProjectionCallback;
    SettingsCallback settingsCallback;

    bool autoProjection = true;
    int targetFPS = 60;
    std::shared_ptr<KeybindManager> keybindManager;
    bool running = true;

    void CreateControls() {
        // No GUI - console output only
        std::cout << "Pick6 C++ (Console Mode)" << std::endl;
        std::cout << "GUI not available - GTK3 not found" << std::endl;
        std::cout << "Press Ctrl+C to exit" << std::endl;
    }
};
#endif
#endif

MainWindow::MainWindow() : pImpl(std::make_unique<Impl>()) {}

MainWindow::~MainWindow() = default;

bool MainWindow::Initialize() {
#ifdef _WIN32
    // Initialize common controls
    INITCOMMONCONTROLSEX icex;
    icex.dwSize = sizeof(INITCOMMONCONTROLSEX);
    icex.dwICC = ICC_TRACKBAR_CLASS;
    InitCommonControlsEx(&icex);

    // Register window class
    const char* CLASS_NAME = "Pick6MainWindow";
    
    WNDCLASS wc = {};
    wc.lpfnWndProc = MainWindow::Impl::WindowProc;
    wc.hInstance = GetModuleHandle(nullptr);
    wc.lpszClassName = CLASS_NAME;
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = reinterpret_cast<HBRUSH>(COLOR_WINDOW + 1);

    if (!RegisterClass(&wc)) {
        return false;
    }

    // Create window
    pImpl->hwnd = CreateWindowEx(
        0,
        CLASS_NAME,
        "Pick6 - Game Capture (C++)",
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT, 600, 350,
        nullptr, nullptr, GetModuleHandle(nullptr), pImpl.get());

    return pImpl->hwnd != nullptr;

#else
#ifdef HAVE_GTK3
    gtk_init(nullptr, nullptr);
    
    pImpl->window = gtk_window_new(GTK_WINDOW_TOPLEVEL);
    gtk_window_set_title(GTK_WINDOW(pImpl->window), "Pick6 - Game Capture (C++)");
    gtk_window_set_default_size(GTK_WINDOW(pImpl->window), 600, 350);
    gtk_window_set_resizable(GTK_WINDOW(pImpl->window), FALSE);
    
    g_signal_connect(pImpl->window, "destroy", G_CALLBACK(gtk_main_quit), nullptr);
    
    pImpl->CreateControls();
    
    return true;
#else
    pImpl->CreateControls();
    return true;
#endif
#endif
}

void MainWindow::Show() {
#ifdef _WIN32
    ShowWindow(pImpl->hwnd, SW_SHOW);
    UpdateWindow(pImpl->hwnd);
#else
#ifdef HAVE_GTK3
    gtk_widget_show_all(pImpl->window);
#else
    std::cout << "Window would be shown (no GUI available)" << std::endl;
#endif
#endif
}

void MainWindow::Hide() {
#ifdef _WIN32
    ShowWindow(pImpl->hwnd, SW_HIDE);
#else
#ifdef HAVE_GTK3
    gtk_widget_hide(pImpl->window);
#else
    std::cout << "Window would be hidden (no GUI available)" << std::endl;
#endif
#endif
}

int MainWindow::Run() {
#ifdef _WIN32
    MSG msg = {};
    while (GetMessage(&msg, nullptr, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
    return static_cast<int>(msg.wParam);
#else
#ifdef HAVE_GTK3
    gtk_main();
    return 0;
#else
    // Console mode - simple message loop
    std::cout << "Running in console mode. Press Enter to exit..." << std::endl;
    std::cin.get();
    return 0;
#endif
#endif
}

void MainWindow::SetStartInjectionCallback(StartInjectionCallback callback) {
    pImpl->startInjectionCallback = callback;
}

void MainWindow::SetStopInjectionCallback(StopInjectionCallback callback) {
    pImpl->stopInjectionCallback = callback;
}

void MainWindow::SetShowProjectionCallback(ShowProjectionCallback callback) {
    pImpl->showProjectionCallback = callback;
}

void MainWindow::SetHideProjectionCallback(HideProjectionCallback callback) {
    pImpl->hideProjectionCallback = callback;
}

void MainWindow::SetSettingsCallback(SettingsCallback callback) {
    pImpl->settingsCallback = callback;
}

void MainWindow::UpdateStatus(const std::string& status) {
#ifdef _WIN32
    SetWindowText(pImpl->hStatusText, ("Status: " + status).c_str());
#else
#ifdef HAVE_GTK3
    gtk_label_set_text(GTK_LABEL(pImpl->statusLabel), ("Status: " + status).c_str());
#else
    std::cout << "Status: " << status << std::endl;
#endif
#endif
}

void MainWindow::UpdateProcessStatus(const std::string& processInfo) {
#ifdef _WIN32
    SetWindowText(pImpl->hProcessText, ("Process: " + processInfo).c_str());
#else
#ifdef HAVE_GTK3
    gtk_label_set_text(GTK_LABEL(pImpl->processLabel), ("Process: " + processInfo).c_str());
#else
    std::cout << "Process: " << processInfo << std::endl;
#endif
#endif
}

void MainWindow::UpdateCaptureStatus(const std::string& captureInfo) {
#ifdef _WIN32
    SetWindowText(pImpl->hCaptureText, ("Capture: " + captureInfo).c_str());
#else
#ifdef HAVE_GTK3
    gtk_label_set_text(GTK_LABEL(pImpl->captureLabel), ("Capture: " + captureInfo).c_str());
#else
    std::cout << "Capture: " << captureInfo << std::endl;
#endif
#endif
}

void MainWindow::SetAutoProjection(bool enabled) {
    pImpl->autoProjection = enabled;
#ifdef _WIN32
    if (pImpl->hAutoProjectionCheck) {
        SendMessage(pImpl->hAutoProjectionCheck, BM_SETCHECK, enabled ? BST_CHECKED : BST_UNCHECKED, 0);
    }
#else
#ifdef HAVE_GTK3
    if (pImpl->autoProjectionCheck) {
        gtk_toggle_button_set_active(GTK_TOGGLE_BUTTON(pImpl->autoProjectionCheck), enabled);
    }
#else
    std::cout << "Auto-projection: " << (enabled ? "enabled" : "disabled") << std::endl;
#endif
#endif
}

bool MainWindow::GetAutoProjection() const {
    return pImpl->autoProjection;
}

void MainWindow::SetTargetFPS(int fps) {
    pImpl->targetFPS = fps;
#ifdef _WIN32
    if (pImpl->hFpsSlider) {
        SendMessage(pImpl->hFpsSlider, TBM_SETPOS, TRUE, fps);
        pImpl->UpdateFpsDisplay();
    }
#endif
}

int MainWindow::GetTargetFPS() const {
    return pImpl->targetFPS;
}

void MainWindow::SetKeybindManager(std::shared_ptr<KeybindManager> keybindManager) {
    pImpl->keybindManager = keybindManager;
}

} // namespace Pick6