using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulatorApp.Helpers;
using SimulatorApp.Logging;
using SimulatorApp.Models;
using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>
/// Modbus 主站 ViewModel。
/// 负责：连接管理、多块定时轮询、EMS/PCS/BMS/MPPT/储能电表/空调/DI-DO 寄存器解析、遥调遥控写操作。
/// </summary>
public partial class MasterViewModel : ObservableObject
{
    private readonly IMasterService _tcpMaster;
    private readonly IMasterService _rtuMaster;
    private readonly RegisterBank   _bank;
    private readonly AppLogger      _log;

    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;

    // ─────────────────────────────────────────────────────────────
    // 协议选择
    // ─────────────────────────────────────────────────────────────
    [ObservableProperty] private ProtocolType _protocol = ProtocolType.Tcp;
    [ObservableProperty] private bool _isTcp = true;
    [ObservableProperty] private bool _isRtu;

    partial void OnProtocolChanged(ProtocolType value)
    {
        IsTcp = value == ProtocolType.Tcp;
        IsRtu = value == ProtocolType.Rtu;
    }
    partial void OnIsTcpChanged(bool value) { if (value) Protocol = ProtocolType.Tcp; }
    partial void OnIsRtuChanged(bool value) { if (value) Protocol = ProtocolType.Rtu; }

    // ─────────────────────────────────────────────────────────────
    // 连接参数
    // ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _host    = "127.0.0.1";
    [ObservableProperty] private int    _tcpPort = 502;
    [ObservableProperty] private string _comPort = "COM1";
    [ObservableProperty] private int    _baudRate = 9600;
    [ObservableProperty] private byte   _slaveId  = 1;
    [ObservableProperty] private int    _pollIntervalMs = 2000;

    // ─────────────────────────────────────────────────────────────
    // 连接状态
    // ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isConnected;
    [ObservableProperty] private bool   _isPolling;
    [ObservableProperty] private string _statusText = "未连接";

    // ═══════════════════════════════════════════════════════════
    // ①  EMS 系统总览
    // ═══════════════════════════════════════════════════════════
    [ObservableProperty] private string _systemStateText = "--";
    [ObservableProperty] private string _runSceneText    = "--";
    [ObservableProperty] private string _modeStateText   = "--";
    [ObservableProperty] private string _defineSceneText = "--";  // 工况：初始/充电/放电/待机
    [ObservableProperty] private double _soc   = 0;
    [ObservableProperty] private double _soh   = 0;
    [ObservableProperty] private double _pcsActivePower      = 0;   // kW
    [ObservableProperty] private double _gridActivePower     = 0;   // kW
    [ObservableProperty] private double _loadActivePower     = 0;   // kW
    [ObservableProperty] private double _pvOutputPower       = 0;   // kW
    [ObservableProperty] private double _batteryPower        = 0;   // kW
    [ObservableProperty] private double _todayCharge         = 0;   // kWh
    [ObservableProperty] private double _todayDischarge      = 0;   // kWh
    [ObservableProperty] private double _cumulativeCharge    = 0;   // kWh
    [ObservableProperty] private double _cumulativeDischarge = 0;   // kWh
    [ObservableProperty] private uint   _pcsOnlineNum    = 0;
    [ObservableProperty] private uint   _alarmNum        = 0;
    [ObservableProperty] private uint   _faultNum        = 0;

    // ═══════════════════════════════════════════════════════════
    // ②  外部电表（第1台，meter_idx=0，起始地址 384）
    // ═══════════════════════════════════════════════════════════
    [ObservableProperty] private double _mL1Voltage     = 0;   // V
    [ObservableProperty] private double _mL2Voltage     = 0;
    [ObservableProperty] private double _mL3Voltage     = 0;
    [ObservableProperty] private double _mL1Current     = 0;   // A
    [ObservableProperty] private double _mL2Current     = 0;
    [ObservableProperty] private double _mL3Current     = 0;
    [ObservableProperty] private double _mL1Power       = 0;   // kW
    [ObservableProperty] private double _mL2Power       = 0;
    [ObservableProperty] private double _mL3Power       = 0;
    [ObservableProperty] private double _mTolActivePower   = 0; // kW
    [ObservableProperty] private double _mTolReactivePower = 0; // kvar
    [ObservableProperty] private double _mPowerFactor      = 0;
    [ObservableProperty] private double _mFrequency        = 0; // Hz
    [ObservableProperty] private double _mPosActiveCharge  = 0;
    [ObservableProperty] private double _mRevActiveCharge  = 0;
    [ObservableProperty] private bool   _mOnline = false;

    // ═══════════════════════════════════════════════════════════
    // ③  PCS 储能变流器（第1台，pcs_idx=0，起始地址 7296）
    // ═══════════════════════════════════════════════════════════
    [ObservableProperty] private string _pcsOperatingStateText = "--";
    [ObservableProperty] private string _pcsChargingStateText  = "--";
    [ObservableProperty] private bool   _pcsOnline = false;
    [ObservableProperty] private double _pcsDcVoltage      = 0;  // V
    [ObservableProperty] private double _pcsDcCurrent      = 0;  // A
    [ObservableProperty] private double _pcsDcPower        = 0;  // kW
    [ObservableProperty] private double _pcsPhaseAVolt     = 0;  // V
    [ObservableProperty] private double _pcsPhaseBVolt     = 0;
    [ObservableProperty] private double _pcsPhaseCVolt     = 0;
    [ObservableProperty] private double _pcsGridPhaseAVolt = 0;
    [ObservableProperty] private double _pcsGridPhaseBVolt = 0;
    [ObservableProperty] private double _pcsGridPhaseCVolt = 0;
    [ObservableProperty] private double _pcsGridPhaseACurr = 0;  // A
    [ObservableProperty] private double _pcsGridPhaseBCurr = 0;
    [ObservableProperty] private double _pcsGridPhaseCCurr = 0;
    [ObservableProperty] private double _pcsGridFrequency  = 0;  // Hz
    [ObservableProperty] private double _pcsTotalActPower  = 0;  // kW
    [ObservableProperty] private double _pcsTotalReactPower= 0;  // kvar
    [ObservableProperty] private double _pcsDailyCharged   = 0;  // kWh
    [ObservableProperty] private double _pcsDailyDischarged= 0;  // kWh
    [ObservableProperty] private double _pcsTemp1          = 0;  // ℃
    [ObservableProperty] private ushort _pcsAlarm1         = 0;
    [ObservableProperty] private ushort _pcsAlarm2         = 0;
    [ObservableProperty] private ushort _pcsFault1         = 0;
    [ObservableProperty] private ushort _pcsFault2         = 0;

    // ═══════════════════════════════════════════════════════════
    // ④  BMS 电池管理系统（第1台，bms_idx=0，起始地址 23680）
    // ═══════════════════════════════════════════════════════════
    [ObservableProperty] private string _bmsSystemStateText   = "--";
    [ObservableProperty] private bool   _bmsOnline = false;
    [ObservableProperty] private double _bmsTotalVolt          = 0;  // V
    [ObservableProperty] private double _bmsCurrent            = 0;  // A（正=充）
    [ObservableProperty] private double _bmsSoc                = 0;  // %
    [ObservableProperty] private double _bmsSoh                = 0;  // %
    [ObservableProperty] private double _bmsAllowChargeCurr    = 0;  // A
    [ObservableProperty] private double _bmsAllowDischargeCurr = 0;  // A
    [ObservableProperty] private double _bmsMaxCellVolt        = 0;  // V
    [ObservableProperty] private double _bmsMinCellVolt        = 0;  // V
    [ObservableProperty] private double _bmsVoltDiff           = 0;  // V
    [ObservableProperty] private int    _bmsMaxCellTemp        = 0;  // ℃
    [ObservableProperty] private int    _bmsMinCellTemp        = 0;  // ℃
    [ObservableProperty] private ushort _bmsAlarm1      = 0;
    [ObservableProperty] private ushort _bmsAlarm2      = 0;
    [ObservableProperty] private ushort _bmsFault1      = 0;
    [ObservableProperty] private ushort _bmsFault2      = 0;
    [ObservableProperty] private ushort _bmsFault3      = 0;

    // ═══════════════════════════════════════════════════════════
    // ⑤  MPPT 光伏控制器（第1台，mppt_idx=0，起始地址 40064）
    // ═══════════════════════════════════════════════════════════
    [ObservableProperty] private bool   _mpptOnline = false;
    [ObservableProperty] private double _mpptOutputVolt    = 0;  // V
    [ObservableProperty] private double _mpptOutputCurrent = 0;  // A
    [ObservableProperty] private double _mpptOutputPower   = 0;  // W
    [ObservableProperty] private double _mpptPVTotalPower  = 0;  // W
    [ObservableProperty] private double _mpptDCVolt1       = 0;  // V
    [ObservableProperty] private double _mpptDCCurrent1    = 0;  // A
    [ObservableProperty] private double _mpptDCVolt2       = 0;
    [ObservableProperty] private double _mpptDCCurrent2    = 0;
    [ObservableProperty] private double _mpptDCVolt3       = 0;
    [ObservableProperty] private double _mpptDCCurrent3    = 0;
    [ObservableProperty] private double _mpptDCVolt4       = 0;
    [ObservableProperty] private double _mpptDCCurrent4    = 0;
    [ObservableProperty] private double _mpptDailyTotal    = 0;  // kWh
    [ObservableProperty] private double _mpptHeatSinkTemp  = 0;  // ℃
    [ObservableProperty] private ushort _mpptAlarm1        = 0;
    [ObservableProperty] private ushort _mpptFault1        = 0;
    [ObservableProperty] private ushort _mpptFault2        = 0;

    // ═══════════════════════════════════════════════════════════
    // ⑥  储能电表（第1台，idx=0，起始地址 48256）
    // ═══════════════════════════════════════════════════════════
    [ObservableProperty] private bool   _smOnline = false;
    [ObservableProperty] private double _smL1Voltage     = 0;  // V
    [ObservableProperty] private double _smL2Voltage     = 0;
    [ObservableProperty] private double _smL3Voltage     = 0;
    [ObservableProperty] private double _smL1Current     = 0;  // A
    [ObservableProperty] private double _smL2Current     = 0;
    [ObservableProperty] private double _smL3Current     = 0;
    [ObservableProperty] private double _smTolActivePower   = 0; // kW
    [ObservableProperty] private double _smTolReactivePower = 0; // kvar
    [ObservableProperty] private double _smPowerFactor      = 0;
    [ObservableProperty] private double _smFrequency        = 0; // Hz
    [ObservableProperty] private double _smTolActiveEnergy  = 0; // kWh

    // ═══════════════════════════════════════════════════════════
    // ⑦  空调（第1台，idx=0，起始地址 52352）
    // ═══════════════════════════════════════════════════════════
    [ObservableProperty] private bool   _airOnline = false;
    [ObservableProperty] private string _airRunStateText = "--";
    [ObservableProperty] private double _airOutTemp      = 0;  // ℃
    [ObservableProperty] private double _airInterTemp1   = 0;  // ℃
    [ObservableProperty] private double _airInterTemp2   = 0;  // ℃
    [ObservableProperty] private int    _airInterRh1     = 0;  // %RH
    [ObservableProperty] private int    _airCompressorFre= 0;  // Hz
    [ObservableProperty] private int    _airFaultLevel   = 0;

    // ═══════════════════════════════════════════════════════════
    // ⑧  DI/DO 动环控制器（起始地址 60544）
    // ═══════════════════════════════════════════════════════════
    [ObservableProperty] private bool   _diDoOnline = false;
    [ObservableProperty] private bool   _diDoEmergencyStop = false;
    [ObservableProperty] private bool   _diDoQF1Closed = false;
    [ObservableProperty] private bool   _diDoWaterAlarm = false;
    [ObservableProperty] private bool   _diDoGasHighAlarm = false;
    [ObservableProperty] private bool   _diDoSmokeAlarm = false;
    [ObservableProperty] private string _diDoControlModeText = "--";

    // ═══════════════════════════════════════════════════════════
    // 遥调遥控输入值
    // ═══════════════════════════════════════════════════════════
    [ObservableProperty] private int    _ctrlSocUpper   = 90;
    [ObservableProperty] private int    _ctrlSocLower   = 20;
    [ObservableProperty] private int    _ctrlMode       = 0;    // 0=本地 1=远程
    [ObservableProperty] private int    _ctrlPower      = 0;    // W

    // 控制模式 RadioButton 辅助属性
    public bool IsCtrlModeLocal  { get => CtrlMode == 0; set { if (value) CtrlMode = 0; } }
    public bool IsCtrlModeRemote { get => CtrlMode == 1; set { if (value) CtrlMode = 1; } }

    partial void OnCtrlModeChanged(int value)
    {
        OnPropertyChanged(nameof(IsCtrlModeLocal));
        OnPropertyChanged(nameof(IsCtrlModeRemote));
    }

    // ─────────────────────────────────────────────────────────────
    // 通讯日志
    // ─────────────────────────────────────────────────────────────
    public ObservableCollection<string> CommLog { get; } = new();

    // 兼容旧版 ReadResults 绑定
    public ObservableCollection<string> ReadResults => CommLog;

    private IMasterService ActiveService => Protocol == ProtocolType.Tcp ? _tcpMaster : _rtuMaster;

    public MasterViewModel(TcpMasterService tcpMaster, RtuMasterService rtuMaster,
                           RegisterBank bank, AppLogger log)
    {
        _tcpMaster = tcpMaster;
        _rtuMaster = rtuMaster;
        _bank      = bank;
        _log       = log;
    }

    // ─────────────────────────────────────────────────────────────
    // 连接 / 断开
    // ─────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (IsConnected) { await DisconnectInternalAsync(); return; }

        try
        {
            var svc = ActiveService;
            svc.SlaveId = SlaveId;
            if (Protocol == ProtocolType.Tcp) { _tcpMaster.Host = Host; _tcpMaster.Port = TcpPort; }
            else                              { _rtuMaster.ComPort = ComPort; _rtuMaster.BaudRate = BaudRate; }

            await svc.ConnectAsync();
            IsConnected = true;
            StatusText  = Protocol == ProtocolType.Tcp ? $"已连接 {Host}:{TcpPort}" : $"已连接 {ComPort}";
            AddLog($"[连接] {StatusText}  SlaveId={SlaveId}");
            _log.Info($"[主站] 已连接 Protocol={Protocol}");
        }
        catch (Exception ex)
        {
            StatusText = $"连接失败: {ex.Message}";
            AddLog($"[错误] 连接失败 {ex.Message}");
            _log.Error("[主站] 连接失败", ex);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 启动 / 停止轮询
    // ─────────────────────────────────────────────────────────────
    [RelayCommand]
    private void StartPolling()
    {
        if (!IsConnected || IsPolling) return;
        _pollCts  = new CancellationTokenSource();
        IsPolling = true;
        AddLog($"[轮询] 启动，间隔 {PollIntervalMs} ms");

        _pollTask = Task.Run(async () =>
        {
            var token = _pollCts.Token;
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(PollIntervalMs));
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                try   { await PollAllBlocksAsync(token); }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _log.Error("[主站] 轮询异常", ex);
                    AddLogOnUi($"[错误] {ex.Message}");
                }
            }
            IsPolling = false;
        }, _pollCts.Token);
    }

    [RelayCommand]
    private async Task StopPollingAsync()
    {
        if (!IsPolling) return;
        _pollCts?.Cancel();
        if (_pollTask != null) await _pollTask.ConfigureAwait(false);
        IsPolling = false;
        AddLog("[轮询] 已停止");
    }

    // ─────────────────────────────────────────────────────────────
    // 手动读一次
    // ─────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ReadOnceAsync()
    {
        if (!IsConnected) return;
        try   { await PollAllBlocksAsync(CancellationToken.None); }
        catch (Exception ex) { AddLog($"[错误] 手动读取失败 {ex.Message}"); }
    }

    // ─────────────────────────────────────────────────────────────
    // 遥调遥控写命令
    // ─────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task WriteSocLimitsAsync()
    {
        if (!IsConnected) return;
        int upper = Math.Clamp(CtrlSocUpper, 0, 100);
        int lower = Math.Clamp(CtrlSocLower, 0, 100);
        if (lower >= upper) { AddLog("[WARN] SOC下限必须小于上限"); return; }
        try
        {
            await ActiveService.WriteRegistersAsync(EmsRegisterDefs.CtrlSocUpperLimit, [(ushort)upper, (ushort)lower]);
            AddLog($"[写] SOC上限={upper}%  下限={lower}%");
        }
        catch (Exception ex) { AddLog($"[错误] 写SOC限值失败 {ex.Message}"); }
    }

    [RelayCommand]
    private async Task WriteControlModeAsync()
    {
        if (!IsConnected) return;
        int mode = Math.Clamp(CtrlMode, 0, 1);
        try
        {
            await ActiveService.WriteRegistersAsync(EmsRegisterDefs.CtrlControlMode, [(ushort)mode]);
            AddLog($"[写] 控制模式={(mode == 0 ? "本地" : "远程")}");
        }
        catch (Exception ex) { AddLog($"[错误] 写控制模式失败 {ex.Message}"); }
    }

    [RelayCommand]
    private async Task WriteActivePowerAsync()
    {
        if (!IsConnected) return;
        try
        {
            await ActiveService.WriteRegistersAsync(EmsRegisterDefs.CtrlActivePower, [(ushort)(CtrlPower & 0xFFFF)]);
            AddLog($"[写] 有功功率设定值={CtrlPower} W");
        }
        catch (Exception ex) { AddLog($"[错误] 写有功功率失败 {ex.Message}"); }
    }

    // ─────────────────────────────────────────────────────────────
    // 强制断开
    // ─────────────────────────────────────────────────────────────
    public async Task ForceStopAsync()
    {
        await StopPollingAsync();
        if (IsConnected) await DisconnectInternalAsync();
    }

    // ─────────────────────────────────────────────────────────────
    // 多块轮询主循环
    // ─────────────────────────────────────────────────────────────
    private async Task PollAllBlocksAsync(CancellationToken ct)
    {
        var svc = ActiveService;

        // 块1：EMS 信息 (0, 114 regs)
        await svc.PollBlockAsync(EmsRegisterDefs.EmsInfoPollStart, EmsRegisterDefs.EmsInfoPollCount, ct);
        DecodeEmsBlock();
        AddLogOnUi($"[FC03] EMS  addr=0 qty=114  SOC={Soc:F1}%  PCS={PcsActivePower:F2}kW");

        // 块2：外部电表 (384, 178 regs)
        await svc.PollBlockAsync(EmsRegisterDefs.ExtMeterPollStart, EmsRegisterDefs.ExtMeterPollCount, ct);
        DecodeMeterBlock();
        AddLogOnUi($"[FC03] 外部电表  addr=384 qty=178  L1V={ML1Voltage:F1}V  f={MFrequency:F2}Hz");

        // 块3：PCS (7296, 265 regs)
        await svc.PollBlockAsync(EmsRegisterDefs.PcsPollStart, EmsRegisterDefs.PcsPollCount, ct);
        DecodePcsBlock();
        AddLogOnUi($"[FC03] PCS  addr=7296 qty=265  DC={PcsDcVoltage:F1}V  P={PcsTotalActPower:F2}kW  {PcsOperatingStateText}");

        // 块4：BMS (23680, 110 regs)
        await svc.PollBlockAsync(EmsRegisterDefs.BmsPollStart, EmsRegisterDefs.BmsPollCount, ct);
        DecodeBmsBlock();
        AddLogOnUi($"[FC03] BMS  addr=23680 qty=110  SOC={BmsSoc:F1}%  I={BmsCurrent:F1}A  {BmsSystemStateText}");

        // 块5：MPPT (40064, 157 regs)
        await svc.PollBlockAsync(EmsRegisterDefs.MpptPollStart, EmsRegisterDefs.MpptPollCount, ct);
        DecodeMpptBlock();
        AddLogOnUi($"[FC03] MPPT  addr=40064 qty=157  PV={MpptPVTotalPower:F0}W  T={MpptHeatSinkTemp:F1}℃");

        // 块6：储能电表 (48256, 178 regs)
        await svc.PollBlockAsync(EmsRegisterDefs.SmPollStart, EmsRegisterDefs.SmPollCount, ct);
        DecodeStorageMeterBlock();
        AddLogOnUi($"[FC03] 储能电表  addr=48256 qty=178  P={SmTolActivePower:F2}kW  f={SmFrequency:F2}Hz");

        // 块7：空调 (52352, 73 regs)
        await svc.PollBlockAsync(EmsRegisterDefs.AirCondPollStart, EmsRegisterDefs.AirCondPollCount, ct);
        DecodeAirCondBlock();
        AddLogOnUi($"[FC03] 空调  addr=52352 qty=73  T外={AirOutTemp:F1}℃  RH={AirInterRh1}%");

        // 块8：DI/DO (60544, 41 regs)
        await svc.PollBlockAsync(EmsRegisterDefs.DiDoPollStart, EmsRegisterDefs.DiDoPollCount, ct);
        DecodeDiDoBlock();
        AddLogOnUi($"[FC03] DI/DO  addr=60544 qty=41  急停={DiDoEmergencyStop}  QF1={DiDoQF1Closed}");
    }

    // ─────────────────────────────────────────────────────────────
    // 解码：EMS 信息块（addr 0-113）
    // ─────────────────────────────────────────────────────────────
    private void DecodeEmsBlock()
    {
        uint state = ReadUInt32(EmsRegisterDefs.SystemState);
        SystemStateText = state switch { 0=>"运行", 1=>"待机", 2=>"离线", 3=>"故障", 4=>"告警", _=>$"未知({state})" };

        ushort scene = _bank.Read(EmsRegisterDefs.RunScene);
        RunSceneText = scene == 0 ? "并网" : "离网";

        ushort defScene = _bank.Read(EmsRegisterDefs.DefineScene);
        DefineSceneText = defScene switch { 0=>"初始", 1=>"充电", 2=>"放电", 3=>"待机", _=>$"({defScene})" };

        ushort mode = _bank.Read(EmsRegisterDefs.ModeState);
        ModeStateText = mode switch { 0=>"远程", 1=>"本地", 2=>"维护", _=>$"({mode})" };

        Soc                  = ReadUInt64Scaled(EmsRegisterDefs.SOC,                  0.1);
        Soh                  = ReadUInt64Scaled(EmsRegisterDefs.SOH,                  0.1);
        TodayCharge          = ReadUInt64Scaled(EmsRegisterDefs.TodayCharge,          0.001);
        TodayDischarge       = ReadUInt64Scaled(EmsRegisterDefs.TodayDischarge,       0.01);
        CumulativeCharge     = ReadUInt64Scaled(EmsRegisterDefs.CumulativeCharge,     0.01);
        CumulativeDischarge  = ReadUInt64Scaled(EmsRegisterDefs.CumulativeDischarge,  0.01);
        PcsActivePower       = ReadUInt64Scaled(EmsRegisterDefs.PCSActivePower,       0.0001) / 1000.0;
        GridActivePower      = ReadUInt64Scaled(EmsRegisterDefs.GridActivePower,      0.001)  / 1000.0;
        LoadActivePower      = ReadUInt64Scaled(EmsRegisterDefs.LoadActivePower,      0.001)  / 1000.0;
        PvOutputPower        = ReadFloat32Scaled(EmsRegisterDefs.PVOutputPower,       0.001);
        BatteryPower         = ReadUInt64Scaled(EmsRegisterDefs.BatteryPower,         0.001);
        PcsOnlineNum         = ReadUInt32(EmsRegisterDefs.PCSOnlineNum);
        AlarmNum             = ReadUInt32(EmsRegisterDefs.AlarmNum);
        FaultNum             = ReadUInt32(EmsRegisterDefs.DeviceFaultNum);
    }

    // ─────────────────────────────────────────────────────────────
    // 解码：外部电表块（addr 384-561）
    // regtype=int32_t，2寄存器 Big-Endian 有符号
    // ─────────────────────────────────────────────────────────────
    private void DecodeMeterBlock()
    {
        int b = EmsRegisterDefs.ExtMeterBase; // 384

        ML1Voltage  = ReadInt32Scaled(b + EmsRegisterDefs.Meter_L1PhaseVoltage, 0.001);
        ML2Voltage  = ReadInt32Scaled(b + EmsRegisterDefs.Meter_L2PhaseVoltage, 0.001);
        ML3Voltage  = ReadInt32Scaled(b + EmsRegisterDefs.Meter_L3PhaseVoltage, 0.001);
        ML1Current  = ReadInt32Scaled(b + EmsRegisterDefs.Meter_L1Current,      0.001);
        ML2Current  = ReadInt32Scaled(b + EmsRegisterDefs.Meter_L2Current,      0.001);
        ML3Current  = ReadInt32Scaled(b + EmsRegisterDefs.Meter_L3Current,      0.001);
        ML1Power    = ReadInt32Scaled(b + EmsRegisterDefs.Meter_L1ActivePower,  0.001);
        ML2Power    = ReadInt32Scaled(b + EmsRegisterDefs.Meter_L2ActivePower,  0.001);
        ML3Power    = ReadInt32Scaled(b + EmsRegisterDefs.Meter_L3ActivePower,  0.001);
        MTolActivePower   = ReadInt32Scaled(b + EmsRegisterDefs.Meter_TolActivePower,   0.001);
        MTolReactivePower = ReadInt32Scaled(b + EmsRegisterDefs.Meter_TolReactivePower, 0.001);
        MPowerFactor      = ReadInt32Scaled(b + EmsRegisterDefs.Meter_TolPowerFactor,   0.001);
        MFrequency        = ReadInt32Scaled(b + EmsRegisterDefs.Meter_Frequency,        0.001);
        MPosActiveCharge  = ReadInt32Scaled(b + EmsRegisterDefs.Meter_PosActiveCharge,  0.001);
        MRevActiveCharge  = ReadInt32Scaled(b + EmsRegisterDefs.Meter_RevActiveCharge,  0.001);
        MOnline = _bank.Read(b + EmsRegisterDefs.Meter_TimeoutFlag) == 0;
    }

    // ─────────────────────────────────────────────────────────────
    // 解码：PCS 块（addr 7296-7560）
    // regtype=int16_t，1寄存器有符号，value = (short)raw × scale
    // ─────────────────────────────────────────────────────────────
    private void DecodePcsBlock()
    {
        int b = EmsRegisterDefs.PcsBase; // 7296

        PcsOnline = _bank.Read(b + EmsRegisterDefs.Pcs_TimeoutFlag) == 0;

        PcsDcVoltage      = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_DcVoltage,         0.1);
        PcsDcCurrent      = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_DcCurrent,        -0.1);
        PcsDcPower        = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_DcPower,          -0.1);
        PcsPhaseAVolt     = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_PhaseAVolt,        0.1);
        PcsPhaseBVolt     = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_PhaseBVolt,        0.1);
        PcsPhaseCVolt     = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_PhaseCVolt,        0.1);
        PcsGridPhaseAVolt = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_GridPhaseAVolt,    0.1);
        PcsGridPhaseBVolt = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_GridPhaseBVolt,    0.1);
        PcsGridPhaseCVolt = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_GridPhaseCVolt,    0.1);
        PcsGridPhaseACurr = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_GridPhaseACurrent, 0.1);
        PcsGridPhaseBCurr = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_GridPhaseBCurrent, 0.1);
        PcsGridPhaseCCurr = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_GridPhaseCCurrent, 0.1);
        PcsGridFrequency  = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_GridFrequency,     0.01);
        PcsTotalActPower  = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_TotalActPower,    -0.001);
        PcsTotalReactPower= ReadInt16Scaled(b + EmsRegisterDefs.Pcs_TotalReactPower,  -0.001);
        PcsDailyCharged   = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_DailyCharged,      0.1);
        PcsDailyDischarged= ReadInt16Scaled(b + EmsRegisterDefs.Pcs_DailyDischarged,   0.1);
        PcsTemp1          = ReadInt16Scaled(b + EmsRegisterDefs.Pcs_Temp1,             0.1);

        PcsAlarm1 = _bank.Read(b + EmsRegisterDefs.Pcs_Alarm1);
        PcsAlarm2 = _bank.Read(b + EmsRegisterDefs.Pcs_Alarm2);
        PcsFault1 = _bank.Read(b + EmsRegisterDefs.Pcs_Fault1);
        PcsFault2 = _bank.Read(b + EmsRegisterDefs.Pcs_Fault2);

        ushort opState = _bank.Read(b + EmsRegisterDefs.Pcs_OperatingState);
        PcsOperatingStateText = opState switch { 0=>"待机", 1=>"自检", 2=>"运行", 3=>"告警", 4=>"故障", _=>$"({opState})" };

        ushort chgState = _bank.Read(b + EmsRegisterDefs.Pcs_ChargingState);
        PcsChargingStateText = chgState switch { 0=>"静置", 1=>"充电", 2=>"放电", _=>$"({chgState})" };
    }

    // ─────────────────────────────────────────────────────────────
    // 解码：BMS 块（addr 23680-23789）
    // ─────────────────────────────────────────────────────────────
    private void DecodeBmsBlock()
    {
        int b = EmsRegisterDefs.BmsBase; // 23680

        BmsOnline = _bank.Read(b + EmsRegisterDefs.Bms_TimeoutFlag) == 0;

        BmsTotalVolt          = ReadInt16Scaled(b + EmsRegisterDefs.Bms_TotalVolt,          0.1);
        BmsCurrent            = ReadInt16Scaled(b + EmsRegisterDefs.Bms_Current,            0.1);
        BmsSoc                = ReadInt16Scaled(b + EmsRegisterDefs.Bms_Soc,                0.1);
        BmsSoh                = ReadInt16Scaled(b + EmsRegisterDefs.Bms_Soh,                0.1);
        BmsAllowChargeCurr    = ReadInt16Scaled(b + EmsRegisterDefs.Bms_AllowChargeCurr,    0.1);
        BmsAllowDischargeCurr = ReadInt16Scaled(b + EmsRegisterDefs.Bms_AllowDischargeCurr, 0.1);
        BmsMaxCellVolt        = ReadInt16Scaled(b + EmsRegisterDefs.Bms_MaxCellVolt,        0.001);
        BmsMinCellVolt        = ReadInt16Scaled(b + EmsRegisterDefs.Bms_MinCellVolt,        0.001);
        BmsVoltDiff           = ReadInt16Scaled(b + EmsRegisterDefs.Bms_VoltDiff,           0.001);
        BmsMaxCellTemp        = ReadInt32(b + EmsRegisterDefs.Bms_MaxCellTemp);
        BmsMinCellTemp        = ReadInt32(b + EmsRegisterDefs.Bms_MinCellTemp);

        BmsAlarm1 = _bank.Read(b + EmsRegisterDefs.Bms_Alarm1);
        BmsAlarm2 = _bank.Read(b + EmsRegisterDefs.Bms_Alarm2);
        BmsFault1 = _bank.Read(b + EmsRegisterDefs.Bms_Fault1);
        BmsFault2 = _bank.Read(b + EmsRegisterDefs.Bms_Fault2);
        BmsFault3 = _bank.Read(b + EmsRegisterDefs.Bms_Fault3);

        ushort sysState = _bank.Read(b + EmsRegisterDefs.Bms_SystemState);
        BmsSystemStateText = sysState switch { 0=>"静置", 1=>"充电", 2=>"放电", _=>$"({sysState})" };
    }

    // ─────────────────────────────────────────────────────────────
    // 解码：MPPT 块（addr 40064-40220）
    // ─────────────────────────────────────────────────────────────
    private void DecodeMpptBlock()
    {
        int b = EmsRegisterDefs.MpptBase; // 40064

        MpptOnline = _bank.Read(b + EmsRegisterDefs.Mppt_TimeoutFlag) == 0;

        MpptOutputVolt    = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_OutputVolt,    0.1);
        MpptOutputCurrent = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_OutputCurrent, 0.1);
        MpptOutputPower   = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_OutputPower,   0.1);
        MpptPVTotalPower  = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_PVTotalPower,  0.01);
        MpptDCVolt1       = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_DCVolt1,       0.1);
        MpptDCCurrent1    = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_DCCurrent1,    0.1);
        MpptDCVolt2       = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_DCVolt2,       0.1);
        MpptDCCurrent2    = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_DCCurrent2,    0.1);
        MpptDCVolt3       = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_DCVolt3,       0.1);
        MpptDCCurrent3    = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_DCCurrent3,    0.1);
        MpptDCVolt4       = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_DCVolt4,       0.1);
        MpptDCCurrent4    = ReadInt16Scaled(b + EmsRegisterDefs.Mppt_DCCurrent4,    0.1);
        MpptDailyTotal    = ReadUInt16Scaled(b + EmsRegisterDefs.Mppt_DailyTotal,   0.1);
        MpptHeatSinkTemp  = ReadUInt16Scaled(b + EmsRegisterDefs.Mppt_HeatSinkTemp, 0.1);

        MpptAlarm1 = _bank.Read(b + EmsRegisterDefs.Mppt_Alarm1);
        MpptFault1 = _bank.Read(b + EmsRegisterDefs.Mppt_Fault1);
        MpptFault2 = _bank.Read(b + EmsRegisterDefs.Mppt_Fault2);
    }

    // ─────────────────────────────────────────────────────────────
    // 解码：储能电表块（addr 48256-48432）
    // regtype=int32_t，2寄存器 Big-Endian 有符号
    // ─────────────────────────────────────────────────────────────
    private void DecodeStorageMeterBlock()
    {
        int b = EmsRegisterDefs.StorageMeterBase; // 48256

        SmOnline = _bank.Read(b + EmsRegisterDefs.Sm_TimeoutFlag) == 0;

        SmL1Voltage  = ReadInt32Scaled(b + EmsRegisterDefs.Sm_L1PhaseVoltage, 0.001);
        SmL2Voltage  = ReadInt32Scaled(b + EmsRegisterDefs.Sm_L2PhaseVoltage, 0.001);
        SmL3Voltage  = ReadInt32Scaled(b + EmsRegisterDefs.Sm_L3PhaseVoltage, 0.001);
        SmL1Current  = ReadInt32Scaled(b + EmsRegisterDefs.Sm_L1Current,      0.001);
        SmL2Current  = ReadInt32Scaled(b + EmsRegisterDefs.Sm_L2Current,      0.001);
        SmL3Current  = ReadInt32Scaled(b + EmsRegisterDefs.Sm_L3Current,      0.001);
        SmTolActivePower   = ReadInt32Scaled(b + EmsRegisterDefs.Sm_TolActivePower,   0.001);
        SmTolReactivePower = ReadInt32Scaled(b + EmsRegisterDefs.Sm_TolReactivePower, 0.001);
        SmPowerFactor      = ReadInt32Scaled(b + EmsRegisterDefs.Sm_TolPowerFactor,   0.001);
        SmFrequency        = ReadInt32Scaled(b + EmsRegisterDefs.Sm_Frequency,        0.001);
        SmTolActiveEnergy  = ReadInt32Scaled(b + EmsRegisterDefs.Sm_TolActiveEnergy,  0.001);
    }

    // ─────────────────────────────────────────────────────────────
    // 解码：空调块（addr 52352-52424）
    // ─────────────────────────────────────────────────────────────
    private void DecodeAirCondBlock()
    {
        int b = EmsRegisterDefs.AirCondBase; // 52352

        AirOnline = _bank.Read(b + EmsRegisterDefs.Air_TimeoutFlag) == 0;

        ushort runState = _bank.Read(b + EmsRegisterDefs.Air_AllRunState);
        AirRunStateText = runState switch { 0=>"停机", 1=>"制冷", 2=>"制热", 3=>"除湿", 4=>"通风", _=>$"({runState})" };

        AirOutTemp      = ReadUInt16Scaled(b + EmsRegisterDefs.Air_OutTemp1,     0.1);
        AirInterTemp1   = ReadUInt16Scaled(b + EmsRegisterDefs.Air_InterTemp1,   0.1);
        AirInterTemp2   = (short)_bank.Read(b + EmsRegisterDefs.Air_InterTemp2) * 1.0;
        AirInterRh1     = _bank.Read(b + EmsRegisterDefs.Air_InterRh1);
        AirCompressorFre= _bank.Read(b + EmsRegisterDefs.Air_CompressorFre);
        AirFaultLevel   = _bank.Read(b + EmsRegisterDefs.Air_FaultLevel);
    }

    // ─────────────────────────────────────────────────────────────
    // 解码：DI/DO 动环控制器块（addr 60544-60584）
    // ─────────────────────────────────────────────────────────────
    private void DecodeDiDoBlock()
    {
        int b = EmsRegisterDefs.DiDoBase; // 60544

        DiDoOnline       = _bank.Read(b + EmsRegisterDefs.DiDo_TimeoutFlag) == 0;
        DiDoEmergencyStop= _bank.Read(b + EmsRegisterDefs.DiDo_EmergencyStop) != 0;
        DiDoQF1Closed    = _bank.Read(b + EmsRegisterDefs.DiDo_QF1GridFeedback) != 0;
        DiDoWaterAlarm   = _bank.Read(b + EmsRegisterDefs.DiDo_WaterSensor) != 0;
        DiDoGasHighAlarm = _bank.Read(b + EmsRegisterDefs.DiDo_GasHighAlarm) != 0;
        DiDoSmokeAlarm   = _bank.Read(b + EmsRegisterDefs.DiDo_SmokeSensor) != 0;

        ushort ctrlMode = _bank.Read(b + EmsRegisterDefs.DiDo_ControlMode);
        DiDoControlModeText = ctrlMode == 0 ? "自动" : "手动";
    }

    // ─────────────────────────────────────────────────────────────
    // 寄存器解码辅助方法
    // ─────────────────────────────────────────────────────────────

    /// <summary>4 寄存器拼成 uint64，×比例系数。Big-Endian 字序。</summary>
    private double ReadUInt64Scaled(int address, double scale)
    {
        ulong v = ((ulong)_bank.Read(address)     << 48)
                | ((ulong)_bank.Read(address + 1) << 32)
                | ((ulong)_bank.Read(address + 2) << 16)
                |  (ulong)_bank.Read(address + 3);
        return (double)v * scale;
    }

    /// <summary>2 寄存器拼成 uint32（高字在低地址）。</summary>
    private uint ReadUInt32(int address)
        => ((uint)_bank.Read(address) << 16) | _bank.Read(address + 1);

    /// <summary>2 寄存器拼成 int32（有符号，Big-Endian）。</summary>
    private int ReadInt32(int address)
        => (int)(((uint)_bank.Read(address) << 16) | _bank.Read(address + 1));

    /// <summary>2 寄存器拼成 int32 × 比例系数。</summary>
    private double ReadInt32Scaled(int address, double scale)
        => ReadInt32(address) * scale;

    /// <summary>float32（AB CD 字序），×比例系数（EMS区用）。</summary>
    private double ReadFloat32Scaled(int address, double scale)
    {
        float v = FloatRegisterHelper.FromRegisters(_bank.Read(address), _bank.Read(address + 1));
        return v * scale;
    }

    /// <summary>1 寄存器读为有符号 int16，×比例系数（PCS/BMS/MPPT 字段）。</summary>
    private double ReadInt16Scaled(int address, double scale)
        => (short)_bank.Read(address) * scale;

    /// <summary>1 寄存器读为无符号 uint16，×比例系数。</summary>
    private double ReadUInt16Scaled(int address, double scale)
        => _bank.Read(address) * scale;

    // ─────────────────────────────────────────────────────────────
    // 断开连接
    // ─────────────────────────────────────────────────────────────
    private async Task DisconnectInternalAsync()
    {
        await StopPollingAsync();
        try
        {
            await ActiveService.StopAsync();
            IsConnected = false;
            StatusText  = "未连接";
            AddLog("[连接] 已断开");
        }
        catch (Exception ex) { _log.Error("[主站] 断开异常", ex); }
    }

    // ─────────────────────────────────────────────────────────────
    // 日志工具
    // ─────────────────────────────────────────────────────────────
    private void AddLog(string msg)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff}  {msg}";
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            CommLog.Insert(0, line);
            while (CommLog.Count > 300) CommLog.RemoveAt(CommLog.Count - 1);
        });
    }

    private void AddLogOnUi(string msg) => AddLog(msg);
}
