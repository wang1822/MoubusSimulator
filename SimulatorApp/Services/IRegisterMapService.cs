using SimulatorApp.Models;

namespace SimulatorApp.Services;

public interface IRegisterMapService
{
    /// <summary>把所有设备 Model 写入 RegisterBank。</summary>
    void FlushAll();

    /// <summary>从 RegisterBank 读回所有设备 Model。</summary>
    void LoadAll();

    /// <summary>保存当前寄存器快照到 JSON 文件。</summary>
    Task SaveSnapshotAsync(string path);

    /// <summary>从 JSON 文件还原寄存器快照。</summary>
    Task LoadSnapshotAsync(string path);
}
