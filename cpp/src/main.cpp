#include "gui/MainWindow.h"
#include "core/GameCapture.h"
#include "core/ProcessDetector.h"
#include "projection/ProjectionWindow.h"
#include "gui/KeybindManager.h"

#include <memory>
#include <iostream>

#ifdef _WIN32
#include <windows.h>
#include <commdlg.h>
#include <shellapi.h>

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    (void)hInstance; (void)hPrevInstance; (void)lpCmdLine; (void)nCmdShow; // Suppress warnings
#else
int main(int argc, char* argv[]) {
    (void)argc; (void)argv; // Suppress warnings
#endif
    
    try {
        // Initialize main components
        auto gameCapture = std::make_unique<Pick6::GameCapture>();
        auto processDetector = std::make_unique<Pick6::ProcessDetector>();
        auto projectionWindow = std::make_unique<Pick6::ProjectionWindow>();
        auto keybindManager = std::make_shared<Pick6::KeybindManager>();
        auto mainWindow = std::make_unique<Pick6::MainWindow>();

        // Setup default keybinds
        keybindManager->RegisterKeybind("toggle_loader", 
            {0x4C, true, false, false, "Ctrl+L - Toggle Loader"}, // Ctrl+L
            [&mainWindow]() {
                static bool visible = true;
                if (visible) {
                    mainWindow->Hide();
                } else {
                    mainWindow->Show();
                }
                visible = !visible;
            });

        keybindManager->RegisterKeybind("toggle_projection", 
            {0x50, true, false, false, "Ctrl+P - Toggle Projection"}, // Ctrl+P
            [&projectionWindow]() {
                if (projectionWindow->IsVisible()) {
                    projectionWindow->Hide();
                } else {
                    projectionWindow->Show();
                }
            });

        // Initialize projection window
        if (!projectionWindow->Initialize()) {
            std::cerr << "Failed to initialize projection window" << std::endl;
            return 1;
        }
        
        // Enable stealth mode for projection window
        projectionWindow->EnableStealth(true);
        projectionWindow->SetBorderless(true);
        projectionWindow->SetTopmost(true);

        // Set up main window callbacks
        mainWindow->SetKeybindManager(keybindManager);
        
        mainWindow->SetStartInjectionCallback([&]() {
            mainWindow->UpdateStatus("Starting injection...");
            
            // Start monitoring for FiveM processes
            processDetector->StartMonitoring([&](const Pick6::ProcessInfo& processInfo) {
                mainWindow->UpdateProcessStatus("Found: " + processInfo.processName + " (PID: " + std::to_string(processInfo.processId) + ")");
                
                // Start capture
                if (gameCapture->StartCapture(processInfo.processId)) {
                    mainWindow->UpdateCaptureStatus(std::string("Injection successful - ") +
                        (processInfo.hasVulkanSupport ? "Vulkan" : "Window capture"));
                    
                    // Auto-start projection if enabled
                    if (mainWindow->GetAutoProjection()) {
                        projectionWindow->Show();
                    }
                } else {
                    mainWindow->UpdateCaptureStatus("Injection failed");
                }
            });
            
            mainWindow->UpdateStatus("Monitoring for FiveM processes...");
        });

        mainWindow->SetStopInjectionCallback([&]() {
            gameCapture->StopCapture();
            processDetector->StopMonitoring();
            projectionWindow->Hide();
            mainWindow->UpdateStatus("Injection stopped");
            mainWindow->UpdateProcessStatus("Not monitoring");
            mainWindow->UpdateCaptureStatus("Not capturing");
        });

        mainWindow->SetShowProjectionCallback([&]() {
            projectionWindow->Show();
        });

        mainWindow->SetHideProjectionCallback([&]() {
            projectionWindow->Hide();
        });

        // Set up frame capture callback
        gameCapture->SetFrameCallback([&](const Pick6::FrameData& frame) {
            projectionWindow->UpdateFrame(frame);
        });

        // Initialize and show main window
        if (!mainWindow->Initialize()) {
            std::cerr << "Failed to initialize main window" << std::endl;
            return 1;
        }

        // Start keybind monitoring
        keybindManager->StartMonitoring();

        // Load saved keybinds
        keybindManager->LoadFromFile("keybinds.cfg");

        mainWindow->Show();
        mainWindow->UpdateStatus("Ready - Press 'Start Injection' to begin");

        // Run main message loop
        int result = mainWindow->Run();

        // Cleanup
        keybindManager->StopMonitoring();
        keybindManager->SaveToFile("keybinds.cfg");

        return result;

    } catch (const std::exception& e) {
        std::cerr << "Application error: " << e.what() << std::endl;
        return 1;
    }
}