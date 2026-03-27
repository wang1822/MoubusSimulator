using SimulatorApp.Services;

namespace SimulatorApp.Models.Bms;

/// <summary>
/// BMS 电池管理系统数据模型。
/// 寄存器基地址 23680，偏移量参考 EmsRegisterDefs.Bms_*。
/// </summary>
public class BmsModel : DeviceModelBase
{
    public override string DeviceName  => "BMS 电池管理系统";
    public override int    BaseAddress => 23680;

    // ── 遥测（int16 原始值）──
    public short  TotalVolt          { get; set; }  // offset 0,  ×0.1 V
    public short  Current            { get; set; }  // offset 1,  ×0.1 A（正=充电）
    public short  Soc                { get; set; }  // offset 2,  ×0.1 %
    public short  AmpereHourSoc      { get; set; }  // offset 3,  ×0.1 %
    public short  Soh                { get; set; }  // offset 4,  ×0.1 %
    public short  AllowChargeCurr    { get; set; }  // offset 6,  ×0.1 A
    public short  AllowDischargeCurr { get; set; }  // offset 7,  ×0.1 A
    public short  MaxCellVolt        { get; set; }  // offset 19, ×0.001 V
    public short  MinCellVolt        { get; set; }  // offset 22, ×0.001 V
    public short  VoltDiff           { get; set; }  // offset 25, ×0.001 V
    public int    MaxCellTemp        { get; set; }  // offset 30, int32(2reg)×1 ℃
    public int    MinCellTemp        { get; set; }  // offset 34, int32(2reg)×1 ℃

    // ── 状态 ──
    public byte   SystemState        { get; set; }  // offset 8,  0=静置1=充电2=放电
    public byte   SystemSubstate     { get; set; }  // offset 9
    public byte   FaultState         { get; set; }  // offset 93
    public ushort Alarm1             { get; set; }  // offset 94, bitmask
    public ushort Alarm2             { get; set; }  // offset 95
    public ushort Alarm3             { get; set; }  // offset 96
    public ushort Alarm4             { get; set; }  // offset 97
    public ushort Fault1             { get; set; }  // offset 98, bitmask
    public ushort Fault2             { get; set; }  // offset 99
    public ushort Fault3             { get; set; }  // offset 100
    public ushort Fault4             { get; set; }  // offset 101
    public ushort Fault5             { get; set; }  // offset 102
    public ushort Fault6             { get; set; }  // offset 103
    public ushort Fault7             { get; set; }  // offset 104
    public byte   TimeoutFlag        { get; set; }  // offset 109, 0=在线

    public override void ToRegisters(RegisterBank bank)
    {
        int b = BaseAddress;
        bank.Write(b + EmsRegisterDefs.Bms_TotalVolt,          (ushort)TotalVolt);
        bank.Write(b + EmsRegisterDefs.Bms_Current,            (ushort)Current);
        bank.Write(b + EmsRegisterDefs.Bms_Soc,                (ushort)Soc);
        bank.Write(b + EmsRegisterDefs.Bms_AmpereHourSoc,      (ushort)AmpereHourSoc);
        bank.Write(b + EmsRegisterDefs.Bms_Soh,                (ushort)Soh);
        bank.Write(b + EmsRegisterDefs.Bms_AllowChargeCurr,    (ushort)AllowChargeCurr);
        bank.Write(b + EmsRegisterDefs.Bms_AllowDischargeCurr, (ushort)AllowDischargeCurr);
        bank.Write(b + EmsRegisterDefs.Bms_SystemState,        SystemState);
        bank.Write(b + EmsRegisterDefs.Bms_SystemSubstate,     SystemSubstate);
        bank.Write(b + EmsRegisterDefs.Bms_MaxCellVolt,        (ushort)MaxCellVolt);
        bank.Write(b + EmsRegisterDefs.Bms_MinCellVolt,        (ushort)MinCellVolt);
        bank.Write(b + EmsRegisterDefs.Bms_VoltDiff,           (ushort)VoltDiff);
        // int32: 高16位写 offset，低16位写 offset+1
        bank.Write(b + EmsRegisterDefs.Bms_MaxCellTemp,     (ushort)(MaxCellTemp >> 16));
        bank.Write(b + EmsRegisterDefs.Bms_MaxCellTemp + 1, (ushort)(MaxCellTemp & 0xFFFF));
        bank.Write(b + EmsRegisterDefs.Bms_MinCellTemp,     (ushort)(MinCellTemp >> 16));
        bank.Write(b + EmsRegisterDefs.Bms_MinCellTemp + 1, (ushort)(MinCellTemp & 0xFFFF));
        bank.Write(b + EmsRegisterDefs.Bms_FaultState,         FaultState);
        bank.Write(b + EmsRegisterDefs.Bms_Alarm1,             Alarm1);
        bank.Write(b + EmsRegisterDefs.Bms_Alarm2,             Alarm2);
        bank.Write(b + EmsRegisterDefs.Bms_Alarm3,             Alarm3);
        bank.Write(b + EmsRegisterDefs.Bms_Alarm4,             Alarm4);
        bank.Write(b + EmsRegisterDefs.Bms_Fault1,             Fault1);
        bank.Write(b + EmsRegisterDefs.Bms_Fault2,             Fault2);
        bank.Write(b + EmsRegisterDefs.Bms_Fault3,             Fault3);
        bank.Write(b + EmsRegisterDefs.Bms_Fault4,             Fault4);
        bank.Write(b + EmsRegisterDefs.Bms_Fault5,             Fault5);
        bank.Write(b + EmsRegisterDefs.Bms_Fault6,             Fault6);
        bank.Write(b + EmsRegisterDefs.Bms_Fault7,             Fault7);
        bank.Write(b + EmsRegisterDefs.Bms_TimeoutFlag,        TimeoutFlag);
    }

    public override void FromRegisters(RegisterBank bank)
    {
        int b = BaseAddress;
        TotalVolt          = (short)bank.Read(b + EmsRegisterDefs.Bms_TotalVolt);
        Current            = (short)bank.Read(b + EmsRegisterDefs.Bms_Current);
        Soc                = (short)bank.Read(b + EmsRegisterDefs.Bms_Soc);
        AmpereHourSoc      = (short)bank.Read(b + EmsRegisterDefs.Bms_AmpereHourSoc);
        Soh                = (short)bank.Read(b + EmsRegisterDefs.Bms_Soh);
        AllowChargeCurr    = (short)bank.Read(b + EmsRegisterDefs.Bms_AllowChargeCurr);
        AllowDischargeCurr = (short)bank.Read(b + EmsRegisterDefs.Bms_AllowDischargeCurr);
        SystemState        = (byte)bank.Read(b + EmsRegisterDefs.Bms_SystemState);
        SystemSubstate     = (byte)bank.Read(b + EmsRegisterDefs.Bms_SystemSubstate);
        MaxCellVolt        = (short)bank.Read(b + EmsRegisterDefs.Bms_MaxCellVolt);
        MinCellVolt        = (short)bank.Read(b + EmsRegisterDefs.Bms_MinCellVolt);
        VoltDiff           = (short)bank.Read(b + EmsRegisterDefs.Bms_VoltDiff);
        MaxCellTemp        = (int)((uint)bank.Read(b + EmsRegisterDefs.Bms_MaxCellTemp) << 16 | bank.Read(b + EmsRegisterDefs.Bms_MaxCellTemp + 1));
        MinCellTemp        = (int)((uint)bank.Read(b + EmsRegisterDefs.Bms_MinCellTemp) << 16 | bank.Read(b + EmsRegisterDefs.Bms_MinCellTemp + 1));
        FaultState         = (byte)bank.Read(b + EmsRegisterDefs.Bms_FaultState);
        Alarm1             = bank.Read(b + EmsRegisterDefs.Bms_Alarm1);
        Alarm2             = bank.Read(b + EmsRegisterDefs.Bms_Alarm2);
        Alarm3             = bank.Read(b + EmsRegisterDefs.Bms_Alarm3);
        Alarm4             = bank.Read(b + EmsRegisterDefs.Bms_Alarm4);
        Fault1             = bank.Read(b + EmsRegisterDefs.Bms_Fault1);
        Fault2             = bank.Read(b + EmsRegisterDefs.Bms_Fault2);
        Fault3             = bank.Read(b + EmsRegisterDefs.Bms_Fault3);
        Fault4             = bank.Read(b + EmsRegisterDefs.Bms_Fault4);
        Fault5             = bank.Read(b + EmsRegisterDefs.Bms_Fault5);
        Fault6             = bank.Read(b + EmsRegisterDefs.Bms_Fault6);
        Fault7             = bank.Read(b + EmsRegisterDefs.Bms_Fault7);
        TimeoutFlag        = (byte)bank.Read(b + EmsRegisterDefs.Bms_TimeoutFlag);
    }
}
