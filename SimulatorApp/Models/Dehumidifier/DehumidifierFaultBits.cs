namespace SimulatorApp.Models.Dehumidifier;

/// <summary>
/// 除湿机状态位（来自 FX协议 0x1009-0x100A，U32 bitmask）。
/// 各 bit=1 表示对应状态有效（可同时多位有效）。
/// 来源：FX除湿机通讯协议 V1.1，第4页，状态寄存器说明。
/// </summary>
[Flags]
public enum DehumidifierStatusBits : uint
{
    None                = 0,
    StandbyMode         = 1 << 0,  // bit0  待机模式
    DehumiMode          = 1 << 1,  // bit1  除湿模式
    ForcedMode          = 1 << 2,  // bit2  强制模式
    Fan1Running         = 1 << 3,  // bit3  风机1 运行
    Fan2Running         = 1 << 4,  // bit4  风机2 运行
    Fan3Running         = 1 << 5,  // bit5  风机3 运行
    Fan4Running         = 1 << 6,  // bit6  风机4 运行
    DehumiModuleRunning = 1 << 7,  // bit7  除湿模块运行
    EngineeringMode     = 1 << 8,  // bit8  工程模式
}

/// <summary>
/// 除湿机故障位（来自 FX协议 0x100B-0x100C，U32 bitmask）。
/// 各 bit=1 表示对应故障/告警有效。
/// 来源：FX除湿机通讯协议 V1.1，第4-5页，故障寄存器说明。
/// 备注：开机指令(0x6011)会清除故障标志；若故障依然存在则再次置位。
/// </summary>
[Flags]
public enum DehumidifierFaultBits : uint
{
    None               = 0,
    ModbusInterrupt    = 1 << 0,  // bit0   MODBUS 通讯中断
    OverVoltage        = 1 << 1,  // bit1   过电压
    UnderVoltage       = 1 << 2,  // bit2   欠电压
    TempSensorFail     = 1 << 3,  // bit3   温度传感器失效
    HumiSensorFail     = 1 << 4,  // bit4   湿度传感器失效
    Ntc1SensorFail     = 1 << 5,  // bit5   NTC1 传感器失效
    Ntc2SensorFail     = 1 << 6,  // bit6   NTC2 传感器失效
    DehumiModuleFault  = 1 << 7,  // bit7   除湿模块故障
    FanFault1          = 1 << 8,  // bit8   风机故障1
    FanFault2          = 1 << 9,  // bit9   风机故障2
    FanFault3          = 1 << 10, // bit10  风机故障3
    FanFault4          = 1 << 11, // bit11  风机故障4
    HighHumidity       = 1 << 12, // bit12  湿度过大
}
