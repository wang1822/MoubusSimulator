using SimulatorApp.Shared.Services;
using SimulatorApp.Slave.Models;
using SimulatorApp.Slave.Services;
using System.Collections.ObjectModel;

namespace SimulatorApp.Slave.ViewModels;

/// <summary>
/// 从协议文档导入的通用寄存器行数据 ViewModel。
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

    /// <summary>从协议文档格式（地址|中文名|英文名|读写|单位|描述）构��</summary>
    public ImportedDeviceViewModel(
        RegisterBank       bank,
        RegisterMapService mapSvc,
        string             deviceName,
        IEnumerable<(string ChineseName, string EnglishName, int Address, string ReadWrite, string Range, string Unit, string Note)> rows)
        : base(bank, mapSvc)
    {
        int n = System.Threading.Interlocked.Increment(ref _counter);
        DeviceName = string.IsNullOrWhiteSpace(deviceName) ? $"协议导入 #{n}" : $"{deviceName} #{n}";
        foreach (var (chinese, english, addr, rw, range, unit, note) in rows)
            Rows.Add(new ImportedRegisterRow(chinese, english, addr, rw, range, unit, note));
    }

    public override bool IsImported => true;
    public override void GenerateData() { }
    public override void ClearAlarms()  { }
}

/// <summary>单条导入寄存器行（只读展示）</summary>
public sealed class ImportedRegisterRow
{
    public string ChineseName { get; }
    public string EnglishName { get; }
    public int    Address     { get; }
    public string AddressHex  => $"0x{Address:X4}";
    public string ReadWrite   { get; }
    public string Range       { get; }
    public string Unit        { get; }
    public string Note        { get; }

    public ImportedRegisterRow(string chineseName, string englishName, int address,
                                string readWrite, string range, string unit, string note)
    {
        ChineseName = chineseName;
        EnglishName = englishName ?? string.Empty;
        Address     = address;
        ReadWrite   = readWrite   ?? string.Empty;
        Range       = range       ?? string.Empty;
        Unit        = unit        ?? string.Empty;
        Note        = note        ?? string.Empty;
    }
}
