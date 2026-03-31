using SimulatorApp.Shared.Services;
using SimulatorApp.Slave.Models;
using SimulatorApp.Slave.Services;
using System.Collections.ObjectModel;

namespace SimulatorApp.Slave.ViewModels;

/// <summary>
/// 从 Excel/剪贴板直接导入的通用寄存器行数据 ViewModel。
/// 不绑定任何具体设备模型，仅作展示用。
/// </summary>
public class ImportedDeviceViewModel : DeviceViewModelBase
{
    // 空模型占位（不写寄存器）
    private sealed class NullModel : DeviceModelBase
    {
        public override string DeviceName  => "";
        public override int    BaseAddress => 0;
        public override void ToRegisters(RegisterBank bank)  { }
        public override void FromRegisters(RegisterBank bank) { }
    }

    private static int _counter = 0;

    private readonly NullModel _nullModel = new();
    protected override DeviceModelBase Model     => _nullModel;
    protected override void            SyncToModel() { }

    public override string DeviceName { get; }

    /// <summary>解析后的寄存器行，供面板 DataGrid 绑定</summary>
    public ObservableCollection<ImportedRegisterRow> Rows { get; } = new();

    public ImportedDeviceViewModel(
        RegisterBank       bank,
        RegisterMapService mapSvc,
        string             deviceName,
        IEnumerable<(string ChineseName, int Address, double Value)> rows)
        : base(bank, mapSvc)
    {
        int n = System.Threading.Interlocked.Increment(ref _counter);
        DeviceName = string.IsNullOrWhiteSpace(deviceName) ? $"导入数据 #{n}" : $"{deviceName} #{n}";
        foreach (var (name, addr, val) in rows)
            Rows.Add(new ImportedRegisterRow(name, addr, val));
    }

    // 无随机数据、无告警，均为空操作
    public override void GenerateData() { }
    public override void ClearAlarms()  { }
}

/// <summary>单条导入寄存器行（只读展示）</summary>
public sealed class ImportedRegisterRow
{
    public string ChineseName { get; }
    public int    Address     { get; }
    public string AddressHex  => $"0x{Address:X4}";
    public int    ValueDec    { get; }
    public string ValueHex    => $"0x{ValueDec:X4}";

    public ImportedRegisterRow(string chineseName, int address, double value)
    {
        ChineseName = chineseName;
        Address     = address;
        ValueDec    = (int)value;
    }
}
