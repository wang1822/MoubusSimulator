using SimulatorApp.Services;

namespace SimulatorApp.Models.StorageMeter;

/// <summary>储能电表 数据模型（字段待补充）。</summary>
public class StorageMeterModel : DeviceModelBase
{
    public override string DeviceName  => "储能电表";
    public override int    BaseAddress => 48256;

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
