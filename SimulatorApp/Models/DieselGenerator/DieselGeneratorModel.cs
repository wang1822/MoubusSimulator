using SimulatorApp.Services;

namespace SimulatorApp.Models.DieselGenerator;

/// <summary>柴发 数据模型（字段待补充）。</summary>
public class DieselGeneratorModel : DeviceModelBase
{
    public override string DeviceName  => "柴发";
    public override int    BaseAddress => 53504;

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
