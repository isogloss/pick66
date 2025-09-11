#include "KeybindManager.h"
#include <fstream>
#include <sstream>
#include <thread>
#include <map>

#ifdef _WIN32
#include <windows.h>
#endif

namespace Pick6 {

class KeybindManager::Impl {
public:
    struct RegisteredKeybind {
        KeyBind keybind;
        ActionCallback callback;
        int hotkeyId;
    };

    std::map<std::string, RegisteredKeybind> registeredKeybinds;
    bool monitoring = false;
    std::thread monitorThread;
    int nextHotkeyId = 1;

#ifdef _WIN32
    HWND messageWindow = nullptr;

    void CreateMessageWindow() {
        const char* CLASS_NAME = "Pick6KeybindWindow";
        
        WNDCLASS wc = {};
        wc.lpfnWndProc = KeybindWindowProc;
        wc.hInstance = GetModuleHandle(nullptr);
        wc.lpszClassName = CLASS_NAME;

        RegisterClass(&wc);

        messageWindow = CreateWindow(
            CLASS_NAME, "",
            0, 0, 0, 0, 0,
            HWND_MESSAGE, nullptr, GetModuleHandle(nullptr), this);
    }

    static LRESULT CALLBACK KeybindWindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
        if (uMsg == WM_CREATE) {
            CREATESTRUCT* pCreate = reinterpret_cast<CREATESTRUCT*>(lParam);
            SetWindowLongPtr(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pCreate->lpCreateParams));
            return 0;
        }

        auto impl = reinterpret_cast<KeybindManager::Impl*>(GetWindowLongPtr(hwnd, GWLP_USERDATA));
        if (impl && uMsg == WM_HOTKEY) {
            impl->HandleHotkey(static_cast<int>(wParam));
            return 0;
        }

        return DefWindowProc(hwnd, uMsg, wParam, lParam);
    }

    void HandleHotkey(int hotkeyId) {
        for (const auto& pair : registeredKeybinds) {
            if (pair.second.hotkeyId == hotkeyId && pair.second.callback) {
                pair.second.callback();
                break;
            }
        }
    }

    void RegisterSystemHotkey(const std::string& actionName, const KeyBind& keybind) {
        UINT modifiers = 0;
        if (keybind.ctrl) modifiers |= MOD_CONTROL;
        if (keybind.alt) modifiers |= MOD_ALT;
        if (keybind.shift) modifiers |= MOD_SHIFT;

        int hotkeyId = nextHotkeyId++;
        if (RegisterHotKey(messageWindow, hotkeyId, modifiers, keybind.virtualKey)) {
            registeredKeybinds[actionName].hotkeyId = hotkeyId;
        }
    }

    void UnregisterSystemHotkey(const std::string& actionName) {
        auto it = registeredKeybinds.find(actionName);
        if (it != registeredKeybinds.end() && it->second.hotkeyId != 0) {
            UnregisterHotKey(messageWindow, it->second.hotkeyId);
            it->second.hotkeyId = 0;
        }
    }

    void StartMessageLoop() {
        MSG msg;
        while (monitoring && GetMessage(&msg, nullptr, 0, 0)) {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
    }
#else
    // Simplified Linux implementation - would need X11 or other system APIs for real hotkeys
    void StartPollingLoop() {
        while (monitoring) {
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
            // In a real implementation, we would poll for key states here
        }
    }
#endif
};

KeybindManager::KeybindManager() : pImpl(std::make_unique<Impl>()) {
#ifdef _WIN32
    pImpl->CreateMessageWindow();
#endif
}

KeybindManager::~KeybindManager() {
    StopMonitoring();
#ifdef _WIN32
    if (pImpl->messageWindow) {
        DestroyWindow(pImpl->messageWindow);
    }
#endif
}

void KeybindManager::RegisterKeybind(const std::string& actionName, const KeyBind& keybind, ActionCallback callback) {
    pImpl->registeredKeybinds[actionName] = {keybind, callback, 0};
    
    if (pImpl->monitoring) {
#ifdef _WIN32
        pImpl->RegisterSystemHotkey(actionName, keybind);
#endif
    }
}

void KeybindManager::UpdateKeybind(const std::string& actionName, const KeyBind& newKeybind) {
    auto it = pImpl->registeredKeybinds.find(actionName);
    if (it != pImpl->registeredKeybinds.end()) {
#ifdef _WIN32
        if (pImpl->monitoring) {
            pImpl->UnregisterSystemHotkey(actionName);
        }
#endif
        it->second.keybind = newKeybind;
#ifdef _WIN32
        if (pImpl->monitoring) {
            pImpl->RegisterSystemHotkey(actionName, newKeybind);
        }
#endif
    }
}

void KeybindManager::RemoveKeybind(const std::string& actionName) {
#ifdef _WIN32
    if (pImpl->monitoring) {
        pImpl->UnregisterSystemHotkey(actionName);
    }
#endif
    pImpl->registeredKeybinds.erase(actionName);
}

KeyBind KeybindManager::GetKeybind(const std::string& actionName) const {
    auto it = pImpl->registeredKeybinds.find(actionName);
    if (it != pImpl->registeredKeybinds.end()) {
        return it->second.keybind;
    }
    return {};
}

std::map<std::string, KeyBind> KeybindManager::GetAllKeybinds() const {
    std::map<std::string, KeyBind> result;
    for (const auto& pair : pImpl->registeredKeybinds) {
        result[pair.first] = pair.second.keybind;
    }
    return result;
}

void KeybindManager::StartMonitoring() {
    if (pImpl->monitoring) {
        return;
    }

    pImpl->monitoring = true;

#ifdef _WIN32
    // Register all existing hotkeys
    for (const auto& pair : pImpl->registeredKeybinds) {
        pImpl->RegisterSystemHotkey(pair.first, pair.second.keybind);
    }
    
    pImpl->monitorThread = std::thread(&KeybindManager::Impl::StartMessageLoop, pImpl.get());
#else
    pImpl->monitorThread = std::thread(&KeybindManager::Impl::StartPollingLoop, pImpl.get());
#endif
}

void KeybindManager::StopMonitoring() {
    if (!pImpl->monitoring) {
        return;
    }

    pImpl->monitoring = false;

#ifdef _WIN32
    // Unregister all hotkeys
    for (const auto& pair : pImpl->registeredKeybinds) {
        pImpl->UnregisterSystemHotkey(pair.first);
    }
    
    // Send a message to break the message loop
    if (pImpl->messageWindow) {
        PostMessage(pImpl->messageWindow, WM_QUIT, 0, 0);
    }
#endif

    if (pImpl->monitorThread.joinable()) {
        pImpl->monitorThread.join();
    }
}

void KeybindManager::SaveToFile(const std::string& filename) {
    std::ofstream file(filename);
    if (!file.is_open()) {
        return;
    }

    for (const auto& pair : pImpl->registeredKeybinds) {
        const auto& keybind = pair.second.keybind;
        file << pair.first << "=" 
             << keybind.virtualKey << ","
             << (keybind.ctrl ? "1" : "0") << ","
             << (keybind.alt ? "1" : "0") << ","
             << (keybind.shift ? "1" : "0") << ","
             << keybind.description << std::endl;
    }
}

void KeybindManager::LoadFromFile(const std::string& filename) {
    std::ifstream file(filename);
    if (!file.is_open()) {
        return;
    }

    std::string line;
    while (std::getline(file, line)) {
        size_t eqPos = line.find('=');
        if (eqPos == std::string::npos) continue;

        std::string actionName = line.substr(0, eqPos);
        std::string keybindData = line.substr(eqPos + 1);

        std::istringstream iss(keybindData);
        std::string token;
        std::vector<std::string> tokens;
        
        while (std::getline(iss, token, ',')) {
            tokens.push_back(token);
        }

        if (tokens.size() >= 4) {
            KeyBind keybind;
            keybind.virtualKey = std::stoi(tokens[0]);
            keybind.ctrl = tokens[1] == "1";
            keybind.alt = tokens[2] == "1";
            keybind.shift = tokens[3] == "1";
            if (tokens.size() > 4) {
                keybind.description = tokens[4];
            }

            // Only update the keybind if the action exists
            auto it = pImpl->registeredKeybinds.find(actionName);
            if (it != pImpl->registeredKeybinds.end()) {
                UpdateKeybind(actionName, keybind);
            }
        }
    }
}

std::string KeybindManager::VirtualKeyToString(int virtualKey) {
#ifdef _WIN32
    switch (virtualKey) {
    case 0x41: return "A";
    case 0x42: return "B";
    case 0x43: return "C";
    case 0x44: return "D";
    case 0x45: return "E";
    case 0x46: return "F";
    case 0x47: return "G";
    case 0x48: return "H";
    case 0x49: return "I";
    case 0x4A: return "J";
    case 0x4B: return "K";
    case 0x4C: return "L";
    case 0x4D: return "M";
    case 0x4E: return "N";
    case 0x4F: return "O";
    case 0x50: return "P";
    case 0x51: return "Q";
    case 0x52: return "R";
    case 0x53: return "S";
    case 0x54: return "T";
    case 0x55: return "U";
    case 0x56: return "V";
    case 0x57: return "W";
    case 0x58: return "X";
    case 0x59: return "Y";
    case 0x5A: return "Z";
    case VK_F1: return "F1";
    case VK_F2: return "F2";
    case VK_F3: return "F3";
    case VK_F4: return "F4";
    case VK_F5: return "F5";
    case VK_F6: return "F6";
    case VK_F7: return "F7";
    case VK_F8: return "F8";
    case VK_F9: return "F9";
    case VK_F10: return "F10";
    case VK_F11: return "F11";
    case VK_F12: return "F12";
    case VK_SPACE: return "Space";
    case VK_RETURN: return "Enter";
    case VK_ESCAPE: return "Escape";
    default: return "Unknown";
    }
#else
    return "Key" + std::to_string(virtualKey);
#endif
}

int KeybindManager::StringToVirtualKey(const std::string& keyStr) {
    (void)keyStr; // Suppress unused parameter warning
#ifdef _WIN32
    if (keyStr.length() == 1 && keyStr[0] >= 'A' && keyStr[0] <= 'Z') {
        return keyStr[0];
    }
    
    if (keyStr == "Space") return VK_SPACE;
    if (keyStr == "Enter") return VK_RETURN;
    if (keyStr == "Escape") return VK_ESCAPE;
    if (keyStr == "F1") return VK_F1;
    if (keyStr == "F2") return VK_F2;
    if (keyStr == "F3") return VK_F3;
    if (keyStr == "F4") return VK_F4;
    if (keyStr == "F5") return VK_F5;
    if (keyStr == "F6") return VK_F6;
    if (keyStr == "F7") return VK_F7;
    if (keyStr == "F8") return VK_F8;
    if (keyStr == "F9") return VK_F9;
    if (keyStr == "F10") return VK_F10;
    if (keyStr == "F11") return VK_F11;
    if (keyStr == "F12") return VK_F12;
#endif
    
    return 0; // Unknown key
}

} // namespace Pick6