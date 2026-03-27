using SimulatorApp.Services;

namespace SimulatorApp.Models.StsInstrument;

/// <summary>STS 转换开关（仪表） 数据模型（字段待补充）。</summary>
public class StsInstrumentModel : DeviceModelBase
{
    public override string DeviceName  => "STS 转换开关（仪表）";
    public override int    BaseAddress => 1408;

    // TODO: 根据字段文档添加 CLR 属性

    public override void ToRegisters(RegisterBank bank)
    {
        // TODO: 根据字段文档实现
    }

    public override void FromRegisters(RegisterBank bank)
    {
        // TODO: 根据字段文档实现
    }
}
