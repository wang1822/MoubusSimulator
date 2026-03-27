namespace SimulatorApp.Models.ExternalMeter;

/// <summary>外部电表 故障/告警位（待字段文档补充）。</summary>
[Flags]
public enum ExternalMeterFaultBits : ushort
{
    None = 0,
    // TODO: 根据字段文档补充各 bit 定义
    Bit0 = 1 << 0,
    Bit1 = 1 << 1,
}
