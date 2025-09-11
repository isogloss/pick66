#include "ProcessDetector.h"
#include <thread>
#include <chrono>
#include <algorithm>
#include <mutex>

#ifdef _WIN32
#include <windows.h>
#include <tlhelp32.h>
#include <psapi.h>
#endif

namespace Pick6 {

class ProcessDetector::Impl {
public:
    Impl() : monitoring(false) {}

    bool monitoring;
    std::thread monitorThread;
    ProcessCallback processCallback;
    std::vector<uint32_t> knownProcesses;

    std::vector<ProcessInfo> GetSystemProcesses() {
        std::vector<ProcessInfo> processes;

#ifdef _WIN32
        HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (hSnapshot == INVALID_HANDLE_VALUE) {
            return processes;
        }

        PROCESSENTRY32 pe32;
        pe32.dwSize = sizeof(PROCESSENTRY32);

        if (Process32First(hSnapshot, &pe32)) {
            do {
                ProcessInfo info;
                info.processId = pe32.th32ProcessID;
                info.processName = pe32.szExeFile;
                info.isVisible = true; // Simplified
                info.hasVulkanSupport = false; // Would need to check loaded DLLs
                
                // Get window title if available
                HWND hwnd = FindWindow(nullptr, nullptr);
                DWORD pid;
                while (hwnd != nullptr) {
                    GetWindowThreadProcessId(hwnd, &pid);
                    if (pid == pe32.th32ProcessID) {
                        char title[256];
                        if (GetWindowText(hwnd, title, sizeof(title)) > 0) {
                            info.windowTitle = title;
                            break;
                        }
                    }
                    hwnd = GetWindow(hwnd, GW_HWNDNEXT);
                }
                
                processes.push_back(info);
            } while (Process32Next(hSnapshot, &pe32));
        }

        CloseHandle(hSnapshot);
#else
        // Linux implementation would use /proc filesystem
        // For now, create dummy data for testing
        ProcessInfo info;
        info.processId = 1234;
        info.processName = "FiveM.exe";
        info.windowTitle = "FiveM - Test Server";
        info.hasVulkanSupport = true;
        info.isVisible = true;
        processes.push_back(info);
#endif

        return processes;
    }

    void MonitorLoop() {
        while (monitoring) {
            auto processes = GetSystemProcesses();
            auto fivemProcesses = FilterFiveMProcesses(processes);
            
            // Check for new processes
            for (const auto& process : fivemProcesses) {
                if (std::find(knownProcesses.begin(), knownProcesses.end(), process.processId) == knownProcesses.end()) {
                    knownProcesses.push_back(process.processId);
                    if (processCallback) {
                        processCallback(process);
                    }
                }
            }
            
            // Clean up dead processes
            knownProcesses.erase(std::remove_if(knownProcesses.begin(), knownProcesses.end(),
                [&processes](uint32_t pid) {
                    return std::find_if(processes.begin(), processes.end(),
                        [pid](const ProcessInfo& p) { return p.processId == pid; }) == processes.end();
                }), knownProcesses.end());
            
            std::this_thread::sleep_for(std::chrono::seconds(1));
        }
    }

    std::vector<ProcessInfo> FilterFiveMProcesses(const std::vector<ProcessInfo>& allProcesses) {
        std::vector<ProcessInfo> fivemProcesses;
        std::vector<std::string> fivemNames = {
            "FiveM.exe", "FiveM_b2060.exe", "FiveM_b2189.exe", "FiveM_b2372.exe",
            "FiveM_b2545.exe", "FiveM_b2612.exe", "FiveM_b2699.exe", "FiveM_b2802.exe",
            "FiveM_b2944.exe", "CitizenFX.exe"
        };

        for (const auto& process : allProcesses) {
            for (const auto& name : fivemNames) {
                if (process.processName.find(name) != std::string::npos ||
                    process.processName == name) {
                    fivemProcesses.push_back(process);
                    break;
                }
            }
        }

        return fivemProcesses;
    }
};

ProcessDetector::ProcessDetector() : pImpl(std::make_unique<Impl>()) {}

ProcessDetector::~ProcessDetector() {
    StopMonitoring();
}

std::vector<ProcessInfo> ProcessDetector::FindFiveMProcesses() {
    auto allProcesses = pImpl->GetSystemProcesses();
    return pImpl->FilterFiveMProcesses(allProcesses);
}

std::vector<ProcessInfo> ProcessDetector::FindProcessesByName(const std::string& name) {
    std::vector<ProcessInfo> result;
    auto allProcesses = pImpl->GetSystemProcesses();
    
    for (const auto& process : allProcesses) {
        if (process.processName.find(name) != std::string::npos) {
            result.push_back(process);
        }
    }
    
    return result;
}

bool ProcessDetector::ProcessExists(uint32_t processId) {
#ifdef _WIN32
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, processId);
    if (hProcess != nullptr) {
        CloseHandle(hProcess);
        return true;
    }
    return false;
#else
    // Linux implementation would check /proc/PID
    return processId == 1234; // Dummy for testing
#endif
}

bool ProcessDetector::ProcessExists(const std::string& processName) {
    auto processes = FindProcessesByName(processName);
    return !processes.empty();
}

void ProcessDetector::StartMonitoring(ProcessCallback onNewProcess) {
    if (pImpl->monitoring) {
        return;
    }
    
    pImpl->processCallback = onNewProcess;
    pImpl->monitoring = true;
    pImpl->monitorThread = std::thread(&ProcessDetector::Impl::MonitorLoop, pImpl.get());
}

void ProcessDetector::StopMonitoring() {
    if (!pImpl->monitoring) {
        return;
    }
    
    pImpl->monitoring = false;
    if (pImpl->monitorThread.joinable()) {
        pImpl->monitorThread.join();
    }
}

bool ProcessDetector::HasVulkanSupport(uint32_t processId) {
    // In a real implementation, this would check if the process has loaded Vulkan DLLs
    // For now, assume all FiveM processes have Vulkan support
    return ProcessExists(processId);
}

} // namespace Pick6