#pragma once

#include <string>
#include <map>
#include <functional>
#include <memory>

namespace Pick6 {

struct KeyBind {
    int virtualKey;      // Virtual key code (VK_*)
    bool ctrl;
    bool alt;
    bool shift;
    std::string description;
};

class KeybindManager {
public:
    KeybindManager();
    ~KeybindManager();

    // Action callback type
    using ActionCallback = std::function<void()>;

    // Register a keybind for an action
    void RegisterKeybind(const std::string& actionName, const KeyBind& keybind, ActionCallback callback);
    
    // Update a keybind
    void UpdateKeybind(const std::string& actionName, const KeyBind& newKeybind);
    
    // Remove a keybind
    void RemoveKeybind(const std::string& actionName);
    
    // Get current keybind for an action
    KeyBind GetKeybind(const std::string& actionName) const;
    
    // Get all registered actions
    std::map<std::string, KeyBind> GetAllKeybinds() const;
    
    // Start/stop global hotkey monitoring
    void StartMonitoring();
    void StopMonitoring();
    
    // Save/load keybinds to/from file
    void SaveToFile(const std::string& filename);
    void LoadFromFile(const std::string& filename);
    
    // Convert virtual key to string representation
    static std::string VirtualKeyToString(int virtualKey);
    static int StringToVirtualKey(const std::string& keyStr);

private:
    class Impl;
    std::unique_ptr<Impl> pImpl;
};

} // namespace Pick6