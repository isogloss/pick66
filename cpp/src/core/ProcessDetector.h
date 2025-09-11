#pragma once

#include <string>
#include <vector>
#include <functional>
#include <memory>
#include <cstdint>

namespace Pick6 {

struct ProcessInfo {
    uint32_t processId;
    std::string processName;
    std::string windowTitle;
    bool hasVulkanSupport;
    bool isVisible;
};

class ProcessDetector {
public:
    ProcessDetector();
    ~ProcessDetector();

    // Find all FiveM-related processes
    std::vector<ProcessInfo> FindFiveMProcesses();
    
    // Find process by name
    std::vector<ProcessInfo> FindProcessesByName(const std::string& name);
    
    // Check if a specific process exists
    bool ProcessExists(uint32_t processId);
    bool ProcessExists(const std::string& processName);
    
    // Monitor for new FiveM processes (async)
    using ProcessCallback = std::function<void(const ProcessInfo&)>;
    void StartMonitoring(ProcessCallback onNewProcess);
    void StopMonitoring();
    
    // Check if a process has Vulkan support
    bool HasVulkanSupport(uint32_t processId);

private:
    class Impl;
    std::unique_ptr<Impl> pImpl;
};

} // namespace Pick6