#pragma once

#include <string>
#include <functional>
#include <memory>
#include <vector>
#include <cstdint>

namespace Pick6 {

enum class CaptureMethod {
    VulkanInjection,
    WindowCapture,
    DirectX11Capture
};

struct FrameData {
    std::vector<uint8_t> data;
    int width;
    int height;
    int channels; // RGBA = 4
    uint64_t timestamp;
};

class GameCapture {
public:
    GameCapture();
    ~GameCapture();

    // Callback for when a new frame is captured
    using FrameCallback = std::function<void(const FrameData&)>;
    void SetFrameCallback(FrameCallback callback);

    // Start capturing from the specified process
    bool StartCapture(const std::string& processName);
    bool StartCapture(uint32_t processId);
    
    // Stop capturing
    void StopCapture();
    
    // Check if currently capturing
    bool IsCapturing() const;
    
    // Get current capture method
    CaptureMethod GetCaptureMethod() const;
    
    // Configure capture settings
    void SetTargetFPS(int fps);
    void SetOutputResolution(int width, int height);

private:
    class Impl;
    std::unique_ptr<Impl> pImpl;
};

} // namespace Pick6