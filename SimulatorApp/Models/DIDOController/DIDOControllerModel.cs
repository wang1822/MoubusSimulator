using SimulatorApp.Services;

namespace SimulatorApp.Models.DIDOController;

/// <summary>DI/DO 动环控制器 数据模型（字段待补充）。</summary>
public class DIDOControllerModel : DeviceModelBase
{
    public override string DeviceName  => "DI/DO 动环控制器";
    public override int    BaseAddress => 60544;

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
