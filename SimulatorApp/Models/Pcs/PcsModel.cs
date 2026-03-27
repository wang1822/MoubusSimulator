using SimulatorApp.Services;

namespace SimulatorApp.Models.Pcs;

/// <summary>
/// PCS 储能变流器数据模型。
/// 寄存器基地址 7296，偏移量参考 EmsRegisterDefs.Pcs_*。
/// int16 字段存储原始寄存器值（有符号）。
/// </summary>
public class PcsModel : DeviceModelBase
{
    public override string DeviceName  => "PCS 储能变流器";
    public override int    BaseAddress => 7296;

    // ── 遥测（int16 原始值）──
    public short  DcVoltage          { get; set; }  // offset 170, ×0.1 V
    public short  DcCurrent          { get; set; }  // offset 171, ×(-0.1) A
    public short  DcPower            { get; set; }  // offset 174, ×(-0.1) kW
    public short  PhaseAVolt         { get; set; }  // offset 175, ×0.1 V
    public short  PhaseBVolt         { get; set; }  // offset 176, ×0.1 V
    public short  PhaseCVolt         { get; set; }  // offset 177, ×0.1 V
    public short  GridPhaseAVolt     { get; set; }  // offset 178, ×0.1 V
    public short  GridPhaseBVolt     { get; set; }  // offset 179, ×0.1 V
    public short  GridPhaseCVolt     { get; set; }  // offset 180, ×0.1 V
    public short  GridPhaseACurrent  { get; set; }  // offset 187, ×0.1 A
    public short  GridPhaseBCurrent  { get; set; }  // offset 188, ×0.1 A
    public short  GridPhaseCCurrent  { get; set; }  // offset 189, ×0.1 A
    public short  GridFrequency      { get; set; }  // offset 190, ×0.01 Hz
    public short  GridPowerFactor    { get; set; }  // offset 191, ×0.001
    public short  TotalActPower      { get; set; }  // offset 200, ×(-0.001) kW
    public short  PhaseAActPower     { get; set; }  // offset 201, ×(-0.001) kW
    public short  PhaseBActPower     { get; set; }  // offset 202, ×(-0.001) kW
    public short  PhaseCActPower     { get; set; }  // offset 203, ×(-0.001) kW
    public short  TotalReactPower    { get; set; }  // offset 212, ×(-0.001) kvar
    public short  DailyCharged       { get; set; }  // offset 245, ×0.1 kWh
    public short  DailyDischarged    { get; set; }  // offset 246, ×0.1 kWh
    public short  CumCharged         { get; set; }  // offset 251, ×0.1 kWh
    public short  CumDischarged      { get; set; }  // offset 252, ×0.1 kWh
    public short  Temp1              { get; set; }  // offset 263, ×0.1 ℃
    public short  Temp2              { get; set; }  // offset 264, ×0.1 ℃

    // ── 状态（uint16）──
    public ushort Alarm1             { get; set; }  // offset 224, bitmask
    public ushort Alarm2             { get; set; }  // offset 225
    public ushort Alarm3             { get; set; }  // offset 226
    public ushort Alarm4             { get; set; }  // offset 227
    public ushort Fault1             { get; set; }  // offset 228, bitmask
    public ushort Fault2             { get; set; }  // offset 229
    public ushort Fault3             { get; set; }  // offset 230
    public ushort Fault4             { get; set; }  // offset 231
    public ushort ChargingState      { get; set; }  // offset 232, 0=静置1=充电2=放电
    public ushort OperatingState     { get; set; }  // offset 233, 0=待机1=自检2=运行3=告警4=故障
    public ushort OperatingMode      { get; set; }  // offset 234, 0=并网1=离网2=切换中
    public byte   TimeoutFlag        { get; set; }  // offset 32,  0=在线

    public override void ToRegisters(RegisterBank bank)
    {
        int b = BaseAddress;
        bank.Write(b + EmsRegisterDefs.Pcs_TimeoutFlag,       TimeoutFlag);
        bank.Write(b + EmsRegisterDefs.Pcs_DcVoltage,         (ushort)DcVoltage);
        bank.Write(b + EmsRegisterDefs.Pcs_DcCurrent,         (ushort)DcCurrent);
        bank.Write(b + EmsRegisterDefs.Pcs_DcPower,           (ushort)DcPower);
        bank.Write(b + EmsRegisterDefs.Pcs_PhaseAVolt,        (ushort)PhaseAVolt);
        bank.Write(b + EmsRegisterDefs.Pcs_PhaseBVolt,        (ushort)PhaseBVolt);
        bank.Write(b + EmsRegisterDefs.Pcs_PhaseCVolt,        (ushort)PhaseCVolt);
        bank.Write(b + EmsRegisterDefs.Pcs_GridPhaseAVolt,    (ushort)GridPhaseAVolt);
        bank.Write(b + EmsRegisterDefs.Pcs_GridPhaseBVolt,    (ushort)GridPhaseBVolt);
        bank.Write(b + EmsRegisterDefs.Pcs_GridPhaseCVolt,    (ushort)GridPhaseCVolt);
        bank.Write(b + EmsRegisterDefs.Pcs_GridPhaseACurrent, (ushort)GridPhaseACurrent);
        bank.Write(b + EmsRegisterDefs.Pcs_GridPhaseBCurrent, (ushort)GridPhaseBCurrent);
        bank.Write(b + EmsRegisterDefs.Pcs_GridPhaseCCurrent, (ushort)GridPhaseCCurrent);
        bank.Write(b + EmsRegisterDefs.Pcs_GridFrequency,     (ushort)GridFrequency);
        bank.Write(b + EmsRegisterDefs.Pcs_GridPowerFactor,   (ushort)GridPowerFactor);
        bank.Write(b + EmsRegisterDefs.Pcs_TotalActPower,     (ushort)TotalActPower);
        bank.Write(b + EmsRegisterDefs.Pcs_PhaseAActPower,    (ushort)PhaseAActPower);
        bank.Write(b + EmsRegisterDefs.Pcs_PhaseBActPower,    (ushort)PhaseBActPower);
        bank.Write(b + EmsRegisterDefs.Pcs_PhaseCActPower,    (ushort)PhaseCActPower);
        bank.Write(b + EmsRegisterDefs.Pcs_TotalReactPower,   (ushort)TotalReactPower);
        bank.Write(b + EmsRegisterDefs.Pcs_Alarm1,            Alarm1);
        bank.Write(b + EmsRegisterDefs.Pcs_Alarm2,            Alarm2);
        bank.Write(b + EmsRegisterDefs.Pcs_Alarm3,            Alarm3);
        bank.Write(b + EmsRegisterDefs.Pcs_Alarm4,            Alarm4);
        bank.Write(b + EmsRegisterDefs.Pcs_Fault1,            Fault1);
        bank.Write(b + EmsRegisterDefs.Pcs_Fault2,            Fault2);
        bank.Write(b + EmsRegisterDefs.Pcs_Fault3,            Fault3);
        bank.Write(b + EmsRegisterDefs.Pcs_Fault4,            Fault4);
        bank.Write(b + EmsRegisterDefs.Pcs_ChargingState,     ChargingState);
        bank.Write(b + EmsRegisterDefs.Pcs_OperatingState,    OperatingState);
        bank.Write(b + EmsRegisterDefs.Pcs_OperatingMode,     OperatingMode);
        bank.Write(b + EmsRegisterDefs.Pcs_DailyCharged,      (ushort)DailyCharged);
        bank.Write(b + EmsRegisterDefs.Pcs_DailyDischarged,   (ushort)DailyDischarged);
        bank.Write(b + EmsRegisterDefs.Pcs_CumCharged,        (ushort)CumCharged);
        bank.Write(b + EmsRegisterDefs.Pcs_CumDischarged,     (ushort)CumDischarged);
        bank.Write(b + EmsRegisterDefs.Pcs_Temp1,             (ushort)Temp1);
        bank.Write(b + EmsRegisterDefs.Pcs_Temp2,             (ushort)Temp2);
    }

    public override void FromRegisters(RegisterBank bank)
    {
        int b = BaseAddress;
        TimeoutFlag       = (byte)bank.Read(b + EmsRegisterDefs.Pcs_TimeoutFlag);
        DcVoltage         = (short)bank.Read(b + EmsRegisterDefs.Pcs_DcVoltage);
        DcCurrent         = (short)bank.Read(b + EmsRegisterDefs.Pcs_DcCurrent);
        DcPower           = (short)bank.Read(b + EmsRegisterDefs.Pcs_DcPower);
        PhaseAVolt        = (short)bank.Read(b + EmsRegisterDefs.Pcs_PhaseAVolt);
        PhaseBVolt        = (short)bank.Read(b + EmsRegisterDefs.Pcs_PhaseBVolt);
        PhaseCVolt        = (short)bank.Read(b + EmsRegisterDefs.Pcs_PhaseCVolt);
        GridPhaseAVolt    = (short)bank.Read(b + EmsRegisterDefs.Pcs_GridPhaseAVolt);
        GridPhaseBVolt    = (short)bank.Read(b + EmsRegisterDefs.Pcs_GridPhaseBVolt);
        GridPhaseCVolt    = (short)bank.Read(b + EmsRegisterDefs.Pcs_GridPhaseCVolt);
        GridPhaseACurrent = (short)bank.Read(b + EmsRegisterDefs.Pcs_GridPhaseACurrent);
        GridPhaseBCurrent = (short)bank.Read(b + EmsRegisterDefs.Pcs_GridPhaseBCurrent);
        GridPhaseCCurrent = (short)bank.Read(b + EmsRegisterDefs.Pcs_GridPhaseCCurrent);
        GridFrequency     = (short)bank.Read(b + EmsRegisterDefs.Pcs_GridFrequency);
        GridPowerFactor   = (short)bank.Read(b + EmsRegisterDefs.Pcs_GridPowerFactor);
        TotalActPower     = (short)bank.Read(b + EmsRegisterDefs.Pcs_TotalActPower);
        PhaseAActPower    = (short)bank.Read(b + EmsRegisterDefs.Pcs_PhaseAActPower);
        PhaseBActPower    = (short)bank.Read(b + EmsRegisterDefs.Pcs_PhaseBActPower);
        PhaseCActPower    = (short)bank.Read(b + EmsRegisterDefs.Pcs_PhaseCActPower);
        TotalReactPower   = (short)bank.Read(b + EmsRegisterDefs.Pcs_TotalReactPower);
        Alarm1            = bank.Read(b + EmsRegisterDefs.Pcs_Alarm1);
        Alarm2            = bank.Read(b + EmsRegisterDefs.Pcs_Alarm2);
        Alarm3            = bank.Read(b + EmsRegisterDefs.Pcs_Alarm3);
        Alarm4            = bank.Read(b + EmsRegisterDefs.Pcs_Alarm4);
        Fault1            = bank.Read(b + EmsRegisterDefs.Pcs_Fault1);
        Fault2            = bank.Read(b + EmsRegisterDefs.Pcs_Fault2);
        Fault3            = bank.Read(b + EmsRegisterDefs.Pcs_Fault3);
        Fault4            = bank.Read(b + EmsRegisterDefs.Pcs_Fault4);
        ChargingState     = bank.Read(b + EmsRegisterDefs.Pcs_ChargingState);
        OperatingState    = bank.Read(b + EmsRegisterDefs.Pcs_OperatingState);
        OperatingMode     = bank.Read(b + EmsRegisterDefs.Pcs_OperatingMode);
        DailyCharged      = (short)bank.Read(b + EmsRegisterDefs.Pcs_DailyCharged);
        DailyDischarged   = (short)bank.Read(b + EmsRegisterDefs.Pcs_DailyDischarged);
        CumCharged        = (short)bank.Read(b + EmsRegisterDefs.Pcs_CumCharged);
        CumDischarged     = (short)bank.Read(b + EmsRegisterDefs.Pcs_CumDischarged);
        Temp1             = (short)bank.Read(b + EmsRegisterDefs.Pcs_Temp1);
        Temp2             = (short)bank.Read(b + EmsRegisterDefs.Pcs_Temp2);
    }
}
