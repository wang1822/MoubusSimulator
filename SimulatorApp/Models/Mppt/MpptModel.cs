using SimulatorApp.Services;

namespace SimulatorApp.Models.Mppt;

/// <summary>MPPT 光伏控制器 数据模型（字段待补充）。</summary>
public class MpptModel : DeviceModelBase
{
    public override string DeviceName  => "MPPT 光伏控制器";
    public override int    BaseAddress => 40064;

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
