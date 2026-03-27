using SimulatorApp.Services;

namespace SimulatorApp.Models.AirConditioner;

/// <summary>空调 数据模型（字段待补充）。</summary>
public class AirConditionerModel : DeviceModelBase
{
    public override string DeviceName  => "空调";
    public override int    BaseAddress => 52352;

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
