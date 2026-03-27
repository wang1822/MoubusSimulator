using SimulatorApp.Services;

namespace SimulatorApp.Models.StsControl;

/// <summary>STS 控制IO卡 数据模型（字段待补充）。</summary>
public class StsControlModel : DeviceModelBase
{
    public override string DeviceName  => "STS 控制IO卡";
    public override int    BaseAddress => 1920;

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
