using SimulatorApp.Services;

namespace SimulatorApp.Models.Dehumidifier;

/// <summary>
/// 除湿机 数据模型（FX除湿机通讯协议 V1.1 全量映射）。
/// 基地址：53248，共 48 个保持寄存器。
///
/// EMS 寄存器偏移表（相对 BaseAddress = 53248）：
/// ──────────────────────────────────────────────────────────────────
///  ── 状态寄存器（FX 0x1001~0x1023，只读）──
///  +0    HwVersion           硬件版本        U16           FX: 0x1001
///  +1    SwVersion           软件版本        U16           FX: 0x1002
///  +2~3  DeviceId            设备编号        U32           FX: 0x1003-0x1004
///  +4~5  SwDate              软件日期        U32           FX: 0x1005-0x1006
///  +6~7  Heartbeat           心跳值          U32           FX: 0x1007-0x1008
///  +8~9  StatusWord          状态字          U32 bitmask   FX: 0x1009-0x100A
///              bit0 待机模式  bit1 除湿模式  bit2 强制模式
///              bit3 风机1运行 bit4 风机2运行 bit5 风机3运行
///              bit6 风机4运行 bit7 除湿模块运行 bit8 工程模式
///  +10~11 FaultWord          故障字          U32 bitmask   FX: 0x100B-0x100C
///              bit0 MODBUS通讯中断  bit1 过电压    bit2 欠电压
///              bit3 温度传感器失效  bit4 湿度传感器失效
///              bit5 NTC1传感器失效  bit6 NTC2传感器失效
///              bit7 除湿模块故障    bit8 风机故障1  bit9 风机故障2
///              bit10 风机故障3      bit11 风机故障4  bit12 湿度过大
///  +12   Temperature         温度            S16  0.1℃    FX: 0x100D
///  +13   Humidity            湿度            U16  0.1%RH  FX: 0x100E
///  +14   Ntc1Temp            NTC1温度        S16  0.1℃    FX: 0x100F
///  +15   Ntc2Temp            NTC2温度        S16  0.1℃    FX: 0x1010
///  +16   InputVoltage        输入电压        U16  0.01V    FX: 0x1011
///  +17   OutputVoltage       输出电压        U16  1mV      FX: 0x1012
///  +18   TecCurrent          TEC电流         U16  1mA      FX: 0x1013
///  +19~20 MainBoardRuntime   主板工作时长    U32  h        FX: 0x1014-0x1015
///  +21~22 Fan1Runtime        风机1工作时长   U32  h        FX: 0x1016-0x1017
///  +23~24 Fan2Runtime        风机2工作时长   U32  h        FX: 0x1018-0x1019
///  +25~26 Fan3Runtime        风机3工作时长   U32  h        FX: 0x101A-0x101B
///  +27~28 Fan4Runtime        风机4工作时长   U32  h        FX: 0x101C-0x101D
///  +29~30 DehumiModuleRuntime 除湿模块时长  U32  h        FX: 0x101E-0x101F
///  +31   Fan1Current         风机1电流       U16  1mA      FX: 0x1020
///  +32   Fan2Current         风机2电流       U16  1mA      FX: 0x1021
///  +33   Fan3Current         风机3电流       U16  1mA      FX: 0x1022
///  +34   Fan4Current         风机4电流       U16  1mA      FX: 0x1023
///  ── 参数寄存器（FX 0x7031~0x7035，可读写）──
///  +35   DehumiSetPoint      除湿设定点      U16  0.1%RH  FX: 0x7031 (35-90%RH)
///  +36   DehumiReturnDiff    除湿回差        U16  0.1%RH  FX: 0x7032 (0-55%RH)
///  +37   OverVoltageSet      过电压值        U16  0.1V    FX: 0x7033
///  +38   UnderVoltageSet     欠电压值        U16  0.1V    FX: 0x7034
///  +39   VoltageHysteresis   电压保护回差    U16  0.1V    FX: 0x7035
///  ── 命令寄存器（FX 0x6011~0x6012，只写）──
///  +40   RunCmd              运行指令        U16  0=停止,1=自动除湿,2=强制除湿
///  +41~47 Reserve            预留
/// ──────────────────────────────────────────────────────────────────
/// </summary>
public class DehumidifierModel : DeviceModelBase
{
    public override string DeviceName  => "除湿机";
    public override int    BaseAddress => 53248;

    // ── 版本/设备信息 ──
    public ushort HwVersion  { get; set; } = 0x0101;
    public ushort SwVersion  { get; set; } = 0x0101;
    public uint   DeviceId   { get; set; } = 0;
    public uint   SwDate     { get; set; } = 0x07E80105;  // 2024-01-05
    public uint   Heartbeat  { get; set; } = 0;

    // ── 状态字 / 故障字 ──
    public uint StatusWord { get; set; } = 0;
    public uint FaultWord  { get; set; } = 0;

    // ── 遥测 ──
    public short  Temperature   { get; set; } = 250;   // 25.0℃
    public ushort Humidity      { get; set; } = 700;   // 70.0%RH
    public short  Ntc1Temp      { get; set; } = 250;   // 25.0℃
    public short  Ntc2Temp      { get; set; } = 250;   // 25.0℃
    public ushort InputVoltage  { get; set; } = 1200;  // 12.00V (×0.01V)
    public ushort OutputVoltage { get; set; } = 0;     // 0 mV
    public ushort TecCurrent    { get; set; } = 0;     // 0 mA

    // ── 工作时长 ──
    public uint MainBoardRuntime    { get; set; } = 0;
    public uint Fan1Runtime         { get; set; } = 0;
    public uint Fan2Runtime         { get; set; } = 0;
    public uint Fan3Runtime         { get; set; } = 0;
    public uint Fan4Runtime         { get; set; } = 0;
    public uint DehumiModuleRuntime { get; set; } = 0;

    // ── 风机电流 ──
    public ushort Fan1Current { get; set; } = 0;
    public ushort Fan2Current { get; set; } = 0;
    public ushort Fan3Current { get; set; } = 0;
    public ushort Fan4Current { get; set; } = 0;

    // ── 参数寄存器 ──
    public ushort DehumiSetPoint    { get; set; } = 600;  // 60.0%RH
    public ushort DehumiReturnDiff  { get; set; } = 50;   //  5.0%RH
    public ushort OverVoltageSet    { get; set; } = 280;  // 28.0V
    public ushort UnderVoltageSet   { get; set; } = 180;  // 18.0V
    public ushort VoltageHysteresis { get; set; } = 20;   //  2.0V

    // ── 运行指令 ──
    public ushort RunCmd { get; set; } = 0;  // 0=停止,1=自动除湿,2=强制除湿

    public override void ToRegisters(RegisterBank bank)
    {
        int b = BaseAddress;

        // 版本/设备信息
        bank.Write(b +  0, HwVersion);
        bank.Write(b +  1, SwVersion);
        bank.Write(b +  2, (ushort)(DeviceId >> 16));
        bank.Write(b +  3, (ushort)(DeviceId & 0xFFFF));
        bank.Write(b +  4, (ushort)(SwDate >> 16));
        bank.Write(b +  5, (ushort)(SwDate & 0xFFFF));
        bank.Write(b +  6, (ushort)(Heartbeat >> 16));
        bank.Write(b +  7, (ushort)(Heartbeat & 0xFFFF));

        // 状态字/故障字 U32 (Big-Endian 字)
        bank.Write(b +  8, (ushort)(StatusWord >> 16));
        bank.Write(b +  9, (ushort)(StatusWord & 0xFFFF));
        bank.Write(b + 10, (ushort)(FaultWord >> 16));
        bank.Write(b + 11, (ushort)(FaultWord & 0xFFFF));

        // 遥测
        bank.Write(b + 12, (ushort)Temperature);
        bank.Write(b + 13, Humidity);
        bank.Write(b + 14, (ushort)Ntc1Temp);
        bank.Write(b + 15, (ushort)Ntc2Temp);
        bank.Write(b + 16, InputVoltage);
        bank.Write(b + 17, OutputVoltage);
        bank.Write(b + 18, TecCurrent);

        // 工作时长 U32
        bank.Write(b + 19, (ushort)(MainBoardRuntime >> 16));
        bank.Write(b + 20, (ushort)(MainBoardRuntime & 0xFFFF));
        bank.Write(b + 21, (ushort)(Fan1Runtime >> 16));
        bank.Write(b + 22, (ushort)(Fan1Runtime & 0xFFFF));
        bank.Write(b + 23, (ushort)(Fan2Runtime >> 16));
        bank.Write(b + 24, (ushort)(Fan2Runtime & 0xFFFF));
        bank.Write(b + 25, (ushort)(Fan3Runtime >> 16));
        bank.Write(b + 26, (ushort)(Fan3Runtime & 0xFFFF));
        bank.Write(b + 27, (ushort)(Fan4Runtime >> 16));
        bank.Write(b + 28, (ushort)(Fan4Runtime & 0xFFFF));
        bank.Write(b + 29, (ushort)(DehumiModuleRuntime >> 16));
        bank.Write(b + 30, (ushort)(DehumiModuleRuntime & 0xFFFF));

        // 风机电流
        bank.Write(b + 31, Fan1Current);
        bank.Write(b + 32, Fan2Current);
        bank.Write(b + 33, Fan3Current);
        bank.Write(b + 34, Fan4Current);

        // 参数寄存器
        bank.Write(b + 35, DehumiSetPoint);
        bank.Write(b + 36, DehumiReturnDiff);
        bank.Write(b + 37, OverVoltageSet);
        bank.Write(b + 38, UnderVoltageSet);
        bank.Write(b + 39, VoltageHysteresis);

        // 运行指令
        bank.Write(b + 40, RunCmd);

        // +41~47 预留，写 0
        for (int i = 41; i <= 47; i++)
            bank.Write(b + i, 0);
    }

    public override void FromRegisters(RegisterBank bank)
    {
        int b = BaseAddress;

        HwVersion = bank.Read(b + 0);
        SwVersion = bank.Read(b + 1);
        DeviceId  = ((uint)bank.Read(b + 2) << 16) | bank.Read(b + 3);
        SwDate    = ((uint)bank.Read(b + 4) << 16) | bank.Read(b + 5);
        Heartbeat = ((uint)bank.Read(b + 6) << 16) | bank.Read(b + 7);

        StatusWord = ((uint)bank.Read(b +  8) << 16) | bank.Read(b +  9);
        FaultWord  = ((uint)bank.Read(b + 10) << 16) | bank.Read(b + 11);

        Temperature   = (short)bank.Read(b + 12);
        Humidity      = bank.Read(b + 13);
        Ntc1Temp      = (short)bank.Read(b + 14);
        Ntc2Temp      = (short)bank.Read(b + 15);
        InputVoltage  = bank.Read(b + 16);
        OutputVoltage = bank.Read(b + 17);
        TecCurrent    = bank.Read(b + 18);

        MainBoardRuntime    = ((uint)bank.Read(b + 19) << 16) | bank.Read(b + 20);
        Fan1Runtime         = ((uint)bank.Read(b + 21) << 16) | bank.Read(b + 22);
        Fan2Runtime         = ((uint)bank.Read(b + 23) << 16) | bank.Read(b + 24);
        Fan3Runtime         = ((uint)bank.Read(b + 25) << 16) | bank.Read(b + 26);
        Fan4Runtime         = ((uint)bank.Read(b + 27) << 16) | bank.Read(b + 28);
        DehumiModuleRuntime = ((uint)bank.Read(b + 29) << 16) | bank.Read(b + 30);

        Fan1Current = bank.Read(b + 31);
        Fan2Current = bank.Read(b + 32);
        Fan3Current = bank.Read(b + 33);
        Fan4Current = bank.Read(b + 34);

        DehumiSetPoint    = bank.Read(b + 35);
        DehumiReturnDiff  = bank.Read(b + 36);
        OverVoltageSet    = bank.Read(b + 37);
        UnderVoltageSet   = bank.Read(b + 38);
        VoltageHysteresis = bank.Read(b + 39);

        RunCmd = bank.Read(b + 40);
    }
}
