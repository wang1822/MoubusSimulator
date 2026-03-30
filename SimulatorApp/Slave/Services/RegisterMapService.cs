using SimulatorApp.Shared.Services;
using SimulatorApp.Slave.Models;
using SimulatorApp.Slave.ViewModels;
using System.IO;
using System.Text.Json;

namespace SimulatorApp.Slave.Services;

/// <summary>
/// 寄存器映射服务：负责将所有设备模型数据写入 RegisterBank，以及 JSON 快照的导入/导出。
/// </summary>
public class RegisterMapService
{
    private readonly RegisterBank _bank;

    public RegisterMapService(RegisterBank bank)
    {
        _bank = bank;
    }

    /// <summary>
    /// 将单个设备模型的当前字段值刷新到 RegisterBank。
    /// 由 DeviceViewModelBase.FlushToRegisters() 调用。
    /// </summary>
    public void Flush(DeviceModelBase model)
    {
        model.ToRegisters(_bank);
    }

    /// <summary>
    /// 将多个设备模型批量刷新到 RegisterBank。
    /// </summary>
    public void FlushAll(IEnumerable<DeviceModelBase> models)
    {
        foreach (var model in models)
            model.ToRegisters(_bank);
    }

    // ----------------------------------------------------------------
    // JSON 快照
    // ----------------------------------------------------------------

    /// <summary>
    /// 将所有设备的当前字段值序列化为 JSON 文件。
    /// JSON 结构：{ "设备名": { "字段名": 值, ... }, ... }
    /// </summary>
    public void SaveSnapshot(string filePath, IEnumerable<(string DeviceName, object FieldsObject)> deviceSnapshots)
    {
        var dict = deviceSnapshots.ToDictionary(x => x.DeviceName, x => x.FieldsObject);
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(dict, options);
        File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// 从 JSON 文件加载快照，返回原始字典（调用方负责将值映射回 ViewModel）。
    /// </summary>
    public Dictionary<string, JsonElement>? LoadSnapshot(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
    }

    // ----------------------------------------------------------------
    // 快照重载（供 SlaveViewModel 直接传入 DeviceViewModelBase 集合）
    // ----------------------------------------------------------------

    /// <summary>
    /// 将设备 ViewModel 集合序列化为 JSON 快照文件。
    /// 每个设备的数值型 public 属性都会被写入。
    /// </summary>
    public void SaveSnapshot(string filePath, IEnumerable<DeviceViewModelBase> devices)
    {
        var dict = new Dictionary<string, object>();
        var opts = new JsonSerializerOptions { WriteIndented = true };

        foreach (var vm in devices)
        {
            var props = vm.GetType().GetProperties()
                .Where(p => p.CanRead && (p.PropertyType == typeof(double)
                                       || p.PropertyType == typeof(float)
                                       || p.PropertyType == typeof(int)
                                       || p.PropertyType == typeof(ushort)
                                       || p.PropertyType == typeof(bool)
                                       || p.PropertyType == typeof(string)))
                .ToDictionary(p => p.Name, p => p.GetValue(vm));

            dict[vm.DeviceName] = props!;
        }

        File.WriteAllText(filePath,
            JsonSerializer.Serialize(dict, opts),
            System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// 从 JSON 快照文件加载并恢复到 ViewModel 集合。
    /// 属性名匹配时通过反射写入。
    /// </summary>
    public void LoadSnapshot(string filePath, IEnumerable<DeviceViewModelBase> devices)
    {
        if (!File.Exists(filePath)) return;
        string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (data == null) return;

        foreach (var vm in devices)
        {
            if (!data.TryGetValue(vm.DeviceName, out var el)) continue;
            var props = vm.GetType().GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name);

            foreach (var kv in el.EnumerateObject())
            {
                if (!props.TryGetValue(kv.Name, out var prop)) continue;
                try
                {
                    object? value = prop.PropertyType == typeof(double) ? (object)kv.Value.GetDouble()
                                  : prop.PropertyType == typeof(float)  ? kv.Value.GetSingle()
                                  : prop.PropertyType == typeof(int)    ? kv.Value.GetInt32()
                                  : prop.PropertyType == typeof(ushort) ? kv.Value.GetUInt16()
                                  : prop.PropertyType == typeof(bool)   ? kv.Value.GetBoolean()
                                  : prop.PropertyType == typeof(string) ? kv.Value.GetString()
                                  : null;
                    if (value != null) prop.SetValue(vm, value);
                }
                catch { /* 忽略类型不匹配 */ }
            }
            vm.FlushToRegisters();
        }
    }
}
