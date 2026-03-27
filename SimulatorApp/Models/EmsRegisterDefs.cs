namespace SimulatorApp.Models;

/// <summary>
/// GS215 EMS 对外 Modbus 通讯协议 — 寄存器地址常量。
/// 来源：GS215 EMS对外 Modbus通讯协议0225(1).xlsx
/// 功能码 FC03（读保持寄存器）、FC02（读离散输入）、FC16（写多寄存器）。
/// 注意：标注 int16 的字段寄存器类型为 int16_t（有符号），读取时需强制转换为 short。
///       标注 uint16 的字段寄存器类型为 uint16_t（无符号），直接读取。
///       标注 int32(2reg) 的字段占 2 个寄存器，Big-Endian 有符号 32 位整数。
/// </summary>
public static class EmsRegisterDefs
{
    // ─────────────────────────────────────────────────────────────
    // 区域起始地址
    // ─────────────────────────────────────────────────────────────
    public const ushort EmsInfoBase        = 0;      // EMS信息      0-255
    public const ushort EmsEnvBase         = 256;    // EMS环境信息  256-383
    public const ushort ExtMeterBase       = 384;    // 外部电表     384-1407 (4×256)
    public const ushort StsInstrBase       = 1408;   // STS仪表      1408-1919 (2×256)
    public const ushort StsControlBase     = 1920;   // STS控制IO板  1920-2175
    public const ushort PcsBase            = 7296;   // PCS          7296-23679 (32×512)
    public const ushort BmsBase            = 23680;  // BMS          23680-40063 (128×128)
    public const ushort MpptBase           = 40064;  // MPPT         40064-48255 (32×256)
    public const ushort StorageMeterBase   = 48256;  // 储能电表     48256-52351 (16×256)
    public const ushort AirCondBase        = 52352;  // 空调         52352-60543 (64×128)
    public const ushort DiDoBase           = 60544;  // DI/DO控制器  60544-64640 (64×64)

    // ─────────────────────────────────────────────────────────────
    // EMS 信息区（FC03，起始地址 0，lc_idx 从0开始）
    // ─────────────────────────────────────────────────────────────
    public const ushort SystemState            = 0;   // uint32  ×1   0=运行1=待机2=离线3=故障4=告警
    public const ushort RunScene               = 2;   // uint16  ×1   0=并网1=离网
    public const ushort DefineScene            = 3;   // uint16  ×1   0=初始1=充电2=放电3=待机
    public const ushort CabinetOnlineNum       = 4;   // uint32  ×1
    public const ushort CabinetTotalNum        = 6;   // uint32  ×1
    public const ushort PCSOnlineNum           = 8;   // uint32  ×1
    public const ushort PCSTotalNum            = 10;  // uint32  ×1
    public const ushort BatteryStackOnlineNum  = 12;  // uint32  ×1
    public const ushort BatteryStackTotalNum   = 14;  // uint32  ×1
    public const ushort BatteryClusterOnlineNum= 16;  // uint32  ×1
    public const ushort BatteryClusterTotalNum = 18;  // uint32  ×1
    public const ushort ACOnlineNum            = 20;  // uint32  ×1
    public const ushort ACTotalNum             = 22;  // uint32  ×1
    public const ushort DehumiOnlineNum        = 24;  // uint32  ×1
    public const ushort DehumiTotalNum         = 26;  // uint32  ×1
    public const ushort MeterOnlineNum         = 28;  // uint32  ×1
    public const ushort MeterTotalNum          = 30;  // uint32  ×1
    public const ushort SOC                    = 32;  // uint64  ×0.1  %
    public const ushort SOH                    = 36;  // uint64  ×0.1  %
    public const ushort TodayCharge            = 40;  // uint64  ×0.001 kWh
    public const ushort TodayDischarge         = 44;  // uint64  ×0.01  kWh
    public const ushort CumulativeCharge       = 48;  // uint64  ×0.01  kWh
    public const ushort CumulativeDischarge    = 52;  // uint64  ×0.01  kWh
    public const ushort PCSActivePower         = 56;  // uint64  ×0.0001 W
    public const ushort GridActivePower        = 60;  // uint64  ×0.001  W
    public const ushort LoadActivePower        = 64;  // uint64  ×0.001  W
    public const ushort ModeState              = 68;  // uint16  ×1   1=本地0=远程2=维护
    public const ushort RunningCondition       = 69;  // uint16  ×1   1=投运2=暂未投运3=停运
    public const ushort SignalQuality          = 70;  // int32   ×1
    public const ushort PVOutputPower          = 72;  // float32 ×0.001
    public const ushort GenTotalPower          = 74;  // float32 ×1
    public const ushort MPPTTotalNum           = 76;  // uint16  ×1
    public const ushort MPPTOnlineNum          = 77;  // uint16  ×1
    public const ushort STSTotalNum            = 78;  // uint16  ×1
    public const ushort STSOnlineNum           = 79;  // uint16  ×1
    public const ushort IOBoardCurrentNum      = 80;  // uint16  ×1
    public const ushort IOBoardTotalNum        = 81;  // uint16  ×1
    public const ushort DeviceChargeNum        = 86;  // uint32  ×1
    public const ushort DeviceDischargeNum     = 88;  // uint32  ×1
    public const ushort DeviceFaultNum         = 92;  // uint32  ×1
    public const ushort AlarmNum               = 94;  // uint32  ×1
    public const ushort OfflineNum             = 96;  // uint32  ×1
    public const ushort BatteryPower           = 110; // uint64  ×0.001 kW

    // ─────────────────────────────────────────────────────────────
    // EMS 环境信息区（FC03，起始地址 256，env_idx 从0开始）
    // ─────────────────────────────────────────────────────────────
    public const ushort QF1GridFeedback        = 256; // uint8  ×1  0=未合闸1=已合闸
    public const ushort DieselGenerator        = 257; // uint8  ×1  0=未运行1=运行中
    public const ushort EmergencyStop          = 258; // uint8  ×1
    public const ushort DieselGenEnable        = 260; // uint8  ×1  0=关闭1=开启
    public const ushort DoControlMode         = 262; // uint8  ×1  1=手动0=自动

    // ─────────────────────────────────────────────────────────────
    // 遥调遥控区（FC16 写，起始地址 114-117）
    // ─────────────────────────────────────────────────────────────
    public const ushort CtrlSocUpperLimit      = 114; // uint16 SOC上限 0-100
    public const ushort CtrlSocLowerLimit      = 115; // uint16 SOC下限 0-100
    public const ushort CtrlControlMode        = 116; // uint16 控制模式 0=本地1=远程
    public const ushort CtrlActivePower        = 117; // uint16 远程控制有功功率

    // ─────────────────────────────────────────────────────────────
    // 外部电表（FC03，起始384，每个设备256寄存器，meter_idx 从0开始）
    // 以下为偏移量（相对 ExtMeterBase + meter_idx * 256）
    // regtype=int32_t，2个寄存器 Big-Endian 有符号
    // ─────────────────────────────────────────────────────────────
    public const int Meter_L1PhaseVoltage      = 0;   // int32(2reg) ×0.001 V
    public const int Meter_L2PhaseVoltage      = 2;   // int32(2reg) ×0.001 V
    public const int Meter_L3PhaseVoltage      = 4;   // int32(2reg) ×0.001 V
    public const int Meter_L1Current           = 6;   // int32(2reg) ×0.001 A
    public const int Meter_L2Current           = 8;   // int32(2reg) ×0.001 A
    public const int Meter_L3Current           = 10;  // int32(2reg) ×0.001 A
    public const int Meter_L1ActivePower       = 12;  // int32(2reg) ×0.001 kW
    public const int Meter_L2ActivePower       = 14;  // int32(2reg) ×0.001 kW
    public const int Meter_L3ActivePower       = 16;  // int32(2reg) ×0.001 kW
    public const int Meter_TolActivePower      = 48;  // int32(2reg) ×0.001 kW
    public const int Meter_TolReactivePower    = 52;  // int32(2reg) ×0.001 kvar
    public const int Meter_TolPowerFactor      = 54;  // int32(2reg) ×0.001
    public const int Meter_Frequency           = 58;  // int32(2reg) ×0.001 Hz
    public const int Meter_PosActiveCharge     = 60;  // int32(2reg) ×0.001
    public const int Meter_RevActiveCharge     = 62;  // int32(2reg) ×0.001
    public const int Meter_TimeoutFlag         = 176; // uint16  ×1   0=在线1=超时

    // 每台设备的寄存器偏移步长
    public const int MeterDeviceStride         = 256;

    // ─────────────────────────────────────────────────────────────
    // PCS 储能变流器（FC03，起始7296，每台设备512寄存器）
    // 以下为偏移量（相对 PcsBase + pcs_idx * 512）
    // regtype 标注：int16 = int16_t（1寄存器有符号）, uint16 = 无符号
    // ─────────────────────────────────────────────────────────────
    public const int Pcs_TimeoutFlag        = 32;  // 7328, uint8
    public const int Pcs_DcVoltage          = 170; // 7466, int16×0.1 V
    public const int Pcs_DcCurrent          = 171; // 7467, int16×(-0.1) A（负值=充电）
    public const int Pcs_DcPower            = 174; // 7470, int16×(-0.1) kW
    public const int Pcs_PhaseAVolt         = 175; // 7471, int16×0.1 V
    public const int Pcs_PhaseBVolt         = 176; // 7472, int16×0.1 V
    public const int Pcs_PhaseCVolt         = 177; // 7473, int16×0.1 V
    public const int Pcs_GridPhaseAVolt     = 178; // 7474, int16×0.1 V
    public const int Pcs_GridPhaseBVolt     = 179; // 7475, int16×0.1 V
    public const int Pcs_GridPhaseCVolt     = 180; // 7476, int16×0.1 V
    public const int Pcs_GridPhaseACurrent  = 187; // 7483, int16×0.1 A
    public const int Pcs_GridPhaseBCurrent  = 188; // 7484, int16×0.1 A
    public const int Pcs_GridPhaseCCurrent  = 189; // 7485, int16×0.1 A
    public const int Pcs_GridFrequency      = 190; // 7486, int16×0.01 Hz
    public const int Pcs_GridPowerFactor    = 191; // 7487, int16×0.001
    public const int Pcs_TotalActPower      = 200; // 7496, int16×(-0.001) kW（负=放电）
    public const int Pcs_PhaseAActPower     = 201; // 7497, int16×(-0.001) kW
    public const int Pcs_PhaseBActPower     = 202; // 7498, int16×(-0.001) kW
    public const int Pcs_PhaseCActPower     = 203; // 7499, int16×(-0.001) kW
    public const int Pcs_TotalReactPower    = 212; // 7508, int16×(-0.001) kW
    public const int Pcs_Alarm1             = 224; // 7520, uint16 bitmask
    public const int Pcs_Alarm2             = 225; // 7521
    public const int Pcs_Alarm3             = 226; // 7522
    public const int Pcs_Alarm4             = 227; // 7523
    public const int Pcs_Fault1             = 228; // 7524
    public const int Pcs_Fault2             = 229; // 7525
    public const int Pcs_Fault3             = 230; // 7526
    public const int Pcs_Fault4             = 231; // 7527
    public const int Pcs_ChargingState      = 232; // 7528, uint16  0=静置1=充电2=放电
    public const int Pcs_OperatingState     = 233; // 7529, uint16  0=待机1=自检2=运行3=告警4=故障
    public const int Pcs_OperatingMode      = 234; // 7530, ENUM
    public const int Pcs_DailyCharged       = 245; // 7541, int16×0.1 kWh
    public const int Pcs_DailyDischarged    = 246; // 7542, int16×0.1 kWh
    public const int Pcs_CumCharged         = 251; // 7547, int16×0.1 kWh
    public const int Pcs_CumDischarged      = 252; // 7548, int16×0.1 kWh
    public const int Pcs_Temp1              = 263; // 7559, int16×0.1 ℃
    public const int Pcs_Temp2              = 264; // 7560, int16×0.1 ℃
    public const int PcsDeviceStride        = 512;

    // ─────────────────────────────────────────────────────────────
    // BMS 电池管理系统（FC03，起始23680，每台设备128寄存器）
    // 以下为偏移量（相对 BmsBase + bms_idx * 128）
    // ─────────────────────────────────────────────────────────────
    public const int Bms_TotalVolt          = 0;   // 23680, int16×0.1 V
    public const int Bms_Current            = 1;   // 23681, int16×0.1 A（正=充电）
    public const int Bms_Soc                = 2;   // 23682, int16×0.1 %
    public const int Bms_AmpereHourSoc      = 3;   // 23683, int16×0.1 %
    public const int Bms_Soh                = 4;   // 23684, int16×0.1 %
    public const int Bms_AllowChargeCurr    = 6;   // 23686, int16×0.1 A
    public const int Bms_AllowDischargeCurr = 7;   // 23687, int16×0.1 A
    public const int Bms_SystemState        = 8;   // 23688, uint8  0=静置1=充电2=放电
    public const int Bms_SystemSubstate     = 9;   // 23689, uint8
    public const int Bms_MaxCellVolt        = 19;  // 23699, int16×0.001 V
    public const int Bms_MaxVoltPackNo      = 20;  // 23700, uint8
    public const int Bms_MinCellVolt        = 22;  // 23702, int16×0.001 V
    public const int Bms_MinVoltPackNo      = 23;  // 23703, uint8
    public const int Bms_VoltDiff           = 25;  // 23705, int16×0.001 V
    public const int Bms_MaxCellTemp        = 30;  // 23710, int32(2reg)×1 ℃
    public const int Bms_MinCellTemp        = 34;  // 23714, int32(2reg)×1 ℃
    public const int Bms_FaultState         = 93;  // 23773, uint8
    public const int Bms_Alarm1             = 94;  // 23774, uint16 bitmask
    public const int Bms_Alarm2             = 95;  // 23775
    public const int Bms_Alarm3             = 96;  // 23776
    public const int Bms_Alarm4             = 97;  // 23777
    public const int Bms_Fault1             = 98;  // 23778
    public const int Bms_Fault2             = 99;  // 23779
    public const int Bms_Fault3             = 100; // 23780
    public const int Bms_Fault4             = 101; // 23781
    public const int Bms_Fault5             = 102; // 23782
    public const int Bms_Fault6             = 103; // 23783
    public const int Bms_Fault7             = 104; // 23784
    public const int Bms_TimeoutFlag        = 109; // 23789, uint8
    public const int BmsDeviceStride        = 128;

    // ─────────────────────────────────────────────────────────────
    // MPPT 光伏控制器（FC03，起始40064，每台设备256寄存器）
    // 以下为偏移量（相对 MpptBase + mppt_idx * 256）
    // ─────────────────────────────────────────────────────────────
    public const int Mppt_TimeoutFlag          = 0;   // 40064, uint8
    public const int Mppt_OutputVolt           = 97;  // 40161, int16×0.1 V
    public const int Mppt_OutputCurrent        = 98;  // 40162, int16×0.1 A
    public const int Mppt_OutputPower          = 99;  // 40163, int16×0.1 W
    public const int Mppt_PVTotalPower         = 100; // 40164, int16×0.01 W
    public const int Mppt_DCVolt1              = 101; // 40165, int16×0.1 V
    public const int Mppt_DCCurrent1           = 102; // 40166, int16×0.1 A
    public const int Mppt_DCPower1             = 103; // 40167, int16×0.001 kW
    public const int Mppt_DCVolt2              = 104; // 40168
    public const int Mppt_DCCurrent2           = 105; // 40169
    public const int Mppt_DCPower2             = 106; // 40170
    public const int Mppt_DCVolt3              = 107; // 40171
    public const int Mppt_DCCurrent3           = 108; // 40172
    public const int Mppt_DCPower3             = 109; // 40173
    public const int Mppt_DCVolt4              = 110; // 40174
    public const int Mppt_DCCurrent4           = 111; // 40175
    public const int Mppt_DCPower4             = 112; // 40176
    public const int Mppt_DailyTotal           = 134; // 40198, uint16×0.1 kWh
    public const int Mppt_HeatSinkTemp         = 138; // 40202, uint16×0.1 ℃
    public const int Mppt_Alarm1               = 140; // 40204, uint16 bitmask
    public const int Mppt_Alarm2               = 141; // 40205
    public const int Mppt_Fault1               = 142; // 40206
    public const int Mppt_Fault2               = 143; // 40207
    public const int Mppt_Fault3               = 144; // 40208
    public const int Mppt_Fault4               = 145; // 40209
    public const int MpptDeviceStride          = 256;

    // ─────────────────────────────────────────────────────────────
    // 储能电表（FC03，起始48256，每台设备256寄存器）
    // 以下为偏移量（相对 StorageMeterBase + idx * 256）
    // regtype=int32_t，2个寄存器 Big-Endian 有符号
    // ─────────────────────────────────────────────────────────────
    public const int Sm_L1PhaseVoltage      = 0;   // int32(2reg) ×0.001 V
    public const int Sm_L2PhaseVoltage      = 2;   // int32(2reg) ×0.001 V
    public const int Sm_L3PhaseVoltage      = 4;   // int32(2reg) ×0.001 V
    public const int Sm_L1Current           = 6;   // int32(2reg) ×0.001 A
    public const int Sm_L2Current           = 8;   // int32(2reg) ×0.001 A
    public const int Sm_L3Current           = 10;  // int32(2reg) ×0.001 A
    public const int Sm_L1ActivePower       = 12;  // int32(2reg) ×1 kW
    public const int Sm_L2ActivePower       = 14;  // int32(2reg) ×1 kW
    public const int Sm_L3ActivePower       = 16;  // int32(2reg) ×1 kW
    public const int Sm_TolActivePower      = 48;  // int32(2reg) ×0.001 kW
    public const int Sm_TolReactivePower    = 52;  // int32(2reg) ×0.001 kvar
    public const int Sm_TolPowerFactor      = 54;  // int32(2reg) ×0.001
    public const int Sm_Frequency           = 58;  // int32(2reg) ×0.001 Hz
    public const int Sm_PosActiveCharge     = 60;  // int32(2reg) ×0.001
    public const int Sm_RevActiveCharge     = 62;  // int32(2reg) ×0.001
    public const int Sm_TolActiveEnergy     = 136; // int32(2reg) ×0.001 kWh (addr=48392)
    public const int Sm_TimeoutFlag         = 176; // uint16  0=在线1=超时 (addr=48432)
    public const int SmDeviceStride         = 256;

    // ─────────────────────────────────────────────────────────────
    // 空调（FC03，起始52352，每台设备128寄存器）
    // 以下为偏移量（相对 AirCondBase + idx * 128）
    // ─────────────────────────────────────────────────────────────
    public const int Air_AllRunState        = 1;   // 52353, uint16  综合运行状态
    public const int Air_ExterRunState      = 2;   // 52354, uint16  外机运行状态
    public const int Air_InterRunState      = 3;   // 52355, uint16  内机运行状态
    public const int Air_CompressRunState   = 4;   // 52356, uint16  压缩机运行状态
    public const int Air_OutTemp1           = 6;   // 52358, uint16×0.1 ℃
    public const int Air_OutCoilTemp        = 7;   // 52359, uint16×0.1 ℃
    public const int Air_ExhaustTemp        = 8;   // 52360, uint16×0.1 ℃
    public const int Air_InterTemp1         = 9;   // 52361, uint16×0.1 ℃
    public const int Air_InterTemp2         = 10;  // 52362, uint16×1 ℃
    public const int Air_InterRh1           = 11;  // 52363, uint16×1 %RH
    public const int Air_InterRh2           = 12;  // 52364, uint16×1 %RH
    public const int Air_InterCoilTemp      = 13;  // 52365, uint16×0.1 ℃
    public const int Air_InputCurrent       = 14;  // 52366, uint16×0.001 A
    public const int Air_ACVoltage          = 15;  // 52367, uint16×1 V
    public const int Air_CompressorCurrent  = 17;  // 52369, uint16×0.001 A
    public const int Air_ExterFanSpeed      = 18;  // 52370, uint16×1 r/min
    public const int Air_InterFanSpeed      = 19;  // 52371, uint16×1 r/min
    public const int Air_CompressorFre      = 20;  // 52372, uint16×1 Hz
    public const int Air_HighTempAlarm      = 41;  // 52393, uint16
    public const int Air_LowTempAlarm       = 42;  // 52394
    public const int Air_HighRHAlarm        = 43;  // 52395
    public const int Air_LowRHAlarm         = 44;  // 52396
    public const int Air_FaultLevel         = 71;  // 52423, uint16
    public const int Air_TimeoutFlag        = 72;  // 52424, uint8
    public const int AirCondDeviceStride    = 128;

    // ─────────────────────────────────────────────────────────────
    // DI/DO 动环控制器（FC03，起始60544）
    // 以下为偏移量（相对 DiDoBase）
    // ─────────────────────────────────────────────────────────────
    public const int DiDo_EmergencyStop     = 0;   // 60544, uint8  0=正常1=急停
    public const int DiDo_QF1GridFeedback   = 1;   // 60545, uint8  0=未合闸1=合闸
    public const int DiDo_QF2BatFeedback    = 2;   // 60546, uint8
    public const int DiDo_WaterSensor       = 3;   // 60547, uint8
    public const int DiDo_GasLowAlarm       = 6;   // 60550, uint8
    public const int DiDo_GasHighAlarm      = 7;   // 60551, uint8
    public const int DiDo_TempSensor        = 12;  // 60556, uint8
    public const int DiDo_SmokeSensor       = 13;  // 60557, uint8
    public const int DiDo_ControlMode       = 22;  // 60566, uint8  0=自动1=手动
    public const int DiDo_TimeoutFlag       = 40;  // 60584, uint8

    // ─────────────────────────────────────────────────────────────
    // 轮询块配置（主站轮询时使用）
    // ─────────────────────────────────────────────────────────────
    /// <summary>EMS 信息块：0-113，114个寄存器。</summary>
    public const ushort EmsInfoPollStart  = 0;
    public const ushort EmsInfoPollCount  = 114;

    /// <summary>外部电表块（第1台）：384起，178个寄存器（到超时标志）。</summary>
    public const ushort ExtMeterPollStart = 384;
    public const ushort ExtMeterPollCount = 178;

    /// <summary>PCS块（第1台）：7296起，264个寄存器（到温度字段）。</summary>
    public const ushort PcsPollStart      = 7296;
    public const ushort PcsPollCount      = 265;

    /// <summary>BMS块（第1台）：23680起，110个寄存器（到超时标志）。</summary>
    public const ushort BmsPollStart      = 23680;
    public const ushort BmsPollCount      = 110;

    /// <summary>MPPT块（第1台）：40064起，157个寄存器（到故障字）。</summary>
    public const ushort MpptPollStart     = 40064;
    public const ushort MpptPollCount     = 157;

    /// <summary>储能电表块（第1台）：48256起，178个寄存器（到超时标志）。</summary>
    public const ushort SmPollStart       = 48256;
    public const ushort SmPollCount       = 178;

    /// <summary>空调块（第1台）：52352起，73个寄存器（到超时标志）。</summary>
    public const ushort AirCondPollStart  = 52352;
    public const ushort AirCondPollCount  = 73;

    /// <summary>DI/DO动环控制器：60544起，41个寄存器（到超时标志）。</summary>
    public const ushort DiDoPollStart     = 60544;
    public const ushort DiDoPollCount     = 41;
}
