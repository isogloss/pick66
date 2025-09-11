using Pick6.Core;

namespace Pick6.ModGui;

/// <summary>
/// Log sink that forwards log messages to the ImGui mod menu
/// </summary>
public class ImGuiLogSink : ILogSink
{
    public void WriteLog(LogLevel level, DateTime timestamp, string message)
    {
        GuiState.Instance.AddLogEntry(level.ToString(), timestamp, message);
    }
}