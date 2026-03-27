namespace SimulatorApp.Models.Bms;

/// <summary>BMS 电池管理系统 故障/告警位（待字段文档补充）。</summary>
[Flags]
public enum BmsFaultBits : ushort
{
    None = 0,
    // TODO: 根据字段文档补充各 bit 定义
    Bit0 = 1 << 0,
    Bit1 = 1 << 1,
}
