using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using SimulatorApp.Logging;
using SimulatorApp.Models;
using SimulatorApp.Models.AirConditioner;
using SimulatorApp.Models.Bms;
using SimulatorApp.Models.DIDOController;
using SimulatorApp.Models.Dehumidifier;
using SimulatorApp.Models.DieselGenerator;
using SimulatorApp.Models.ExternalMeter;
using SimulatorApp.Models.GasDetector;
using SimulatorApp.Models.Mppt;
using SimulatorApp.Models.Pcs;
using SimulatorApp.Models.StorageMeter;
using SimulatorApp.Models.StsControl;
using SimulatorApp.Models.StsInstrument;
using SimulatorApp.Services;
using SimulatorApp.ViewModels;
using SimulatorApp.Views;

namespace SimulatorApp;

public partial class App : Application
{
    private IServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化 NLog
        LogManager.Setup().LoadConfigurationFromFile("Logging/nlog.config");

        _services = BuildServices();

        // 全局异常处理
        DispatcherUnhandledException += (_, args) =>
        {
            var log = _services.GetRequiredService<AppLogger>();
            log.Error("未处理异常", args.Exception);
            args.Handled = true;
        };

        var mainWindow = _services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LogManager.Shutdown();
        base.OnExit(e);
    }

    private static IServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();

        // ===== 基础服务 =====
        sc.AddSingleton<AppLogger>();
        sc.AddSingleton<RegisterBank>();

        // ===== 设备 Model =====
        sc.AddSingleton<PcsModel>();
        sc.AddSingleton<BmsModel>();
        sc.AddSingleton<MpptModel>();
        sc.AddSingleton<StsInstrumentModel>();
        sc.AddSingleton<StsControlModel>();
        sc.AddSingleton<AirConditionerModel>();
        sc.AddSingleton<DehumidifierModel>();
        sc.AddSingleton<DieselGeneratorModel>();
        sc.AddSingleton<GasDetectorModel>();
        sc.AddSingleton<ExternalMeterModel>();
        sc.AddSingleton<StorageMeterModel>();
        sc.AddSingleton<DIDOControllerModel>();

        // DeviceModelBase 集合（RegisterMapService 用）
        sc.AddSingleton<IEnumerable<DeviceModelBase>>(sp => new DeviceModelBase[]
        {
            sp.GetRequiredService<PcsModel>(),
            sp.GetRequiredService<BmsModel>(),
            sp.GetRequiredService<MpptModel>(),
            sp.GetRequiredService<StsInstrumentModel>(),
            sp.GetRequiredService<StsControlModel>(),
            sp.GetRequiredService<AirConditionerModel>(),
            sp.GetRequiredService<DehumidifierModel>(),
            sp.GetRequiredService<DieselGeneratorModel>(),
            sp.GetRequiredService<GasDetectorModel>(),
            sp.GetRequiredService<ExternalMeterModel>(),
            sp.GetRequiredService<StorageMeterModel>(),
            sp.GetRequiredService<DIDOControllerModel>(),
        });

        // ===== Modbus 服务 =====
        sc.AddSingleton<TcpSlaveService>();
        sc.AddSingleton<RtuSlaveService>();
        sc.AddSingleton<TcpMasterService>();
        sc.AddSingleton<RtuMasterService>();

        // ===== RegisterMapService =====
        sc.AddSingleton<IRegisterMapService, RegisterMapService>();

        // ===== 设备 ViewModel =====
        sc.AddSingleton<PcsViewModel>();
        sc.AddSingleton<BmsViewModel>();
        sc.AddSingleton<MpptViewModel>();
        sc.AddSingleton<StsInstrumentViewModel>();
        sc.AddSingleton<StsControlViewModel>();
        sc.AddSingleton<AirConditionerViewModel>();
        sc.AddSingleton<DehumidifierViewModel>();
        sc.AddSingleton<DieselGeneratorViewModel>();
        sc.AddSingleton<GasDetectorViewModel>();
        sc.AddSingleton<ExternalMeterViewModel>();
        sc.AddSingleton<StorageMeterViewModel>();
        sc.AddSingleton<DIDOControllerViewModel>();

        // ===== 主 ViewModel =====
        sc.AddSingleton<SlaveViewModel>();
        sc.AddSingleton<MasterViewModel>();
        sc.AddSingleton<MainViewModel>();

        // ===== Views =====
        sc.AddTransient<MainWindow>();

        return sc.BuildServiceProvider();
    }
}
