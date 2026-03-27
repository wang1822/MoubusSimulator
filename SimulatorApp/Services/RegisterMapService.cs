using System.IO;
using System.Text.Json;
using SimulatorApp.Logging;
using SimulatorApp.Models;

namespace SimulatorApp.Services;

/// <summary>负责把所有设备 Model 刷入 RegisterBank，以及快照导入导出。</summary>
public class RegisterMapService : IRegisterMapService
{
    private readonly RegisterBank       _bank;
    private readonly AppLogger          _log;
    private readonly List<DeviceModelBase> _devices;

    public RegisterMapService(RegisterBank bank, AppLogger log, IEnumerable<DeviceModelBase> devices)
    {
        _bank    = bank;
        _log     = log;
        _devices = devices.ToList();
    }

    public void FlushAll()
    {
        foreach (var d in _devices)
        {
            try { d.ToRegisters(_bank); }
            catch (Exception ex) { _log.Error($"[RegisterMap] FlushAll 设备={d.DeviceName}", ex); }
        }
    }

    public void LoadAll()
    {
        foreach (var d in _devices)
        {
            try { d.FromRegisters(_bank); }
            catch (Exception ex) { _log.Error($"[RegisterMap] LoadAll 设备={d.DeviceName}", ex); }
        }
    }

    public async Task SaveSnapshotAsync(string path)
    {
        var snap = new Dictionary<string, object>();
        foreach (var d in _devices)
            snap[d.DeviceName] = new { d.SlaveId, d.BaseAddress };

        var json = JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
        _log.Info($"[快照] 已保存 → {path}");
    }

    public async Task LoadSnapshotAsync(string path)
    {
        if (!File.Exists(path))
        {
            _log.Warn($"[快照] 文件不存在: {path}");
            return;
        }
        var json = await File.ReadAllTextAsync(path);
        _log.Info($"[快照] 已加载 ← {path}  ({json.Length} bytes)");
        // TODO: 根据字段文档实现完整反序列化
    }
}
