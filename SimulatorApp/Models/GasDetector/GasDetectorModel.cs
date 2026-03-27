using SimulatorApp.Services;

namespace SimulatorApp.Models.GasDetector;

/// <summary>气体检测 数据模型（字段待补充）。</summary>
public class GasDetectorModel : DeviceModelBase
{
    public override string DeviceName  => "气体检测";
    public override int    BaseAddress => 53760;

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
