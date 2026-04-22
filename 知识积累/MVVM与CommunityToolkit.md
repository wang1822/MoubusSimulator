# MVVM 模式与 CommunityToolkit.Mvvm 8.x 深度指南

> 适用于 .NET 8 + WPF + CommunityToolkit.Mvvm 8.x 项目
> 本文档聚焦原理与进阶用法，基础绑定语法见 `WPF必备知识.md`

---

## 一、MVVM 模式核心原理

### 1.1 三层职责边界

```
┌──────────────────────────────────────────────────────┐
│  View (.xaml / .xaml.cs)                             │
│  职责：仅展示 UI，不含业务逻辑                          │
│  允许：控件事件处理（纯 UI 行为，如滚动到底部）           │
│  禁止：直接调用 Service、直接操作 Model 数据            │
├──────────────────────────────────────────────────────┤
│  ViewModel (.cs)                                     │
│  职责：状态管理 + 命令逻辑 + 数据转换                   │
│  允许：调用 Service、持有 Model 引用、触发消息           │
│  禁止：引用任何 WPF 控件类型（UIElement / Window 等）   │
├──────────────────────────────────────────────────────┤
│  Model / Service (.cs)                               │
│  职责：业务规则、数据访问、硬件通信                      │
│  允许：纯 C# 逻辑，无 UI 依赖                          │
│  禁止：引用 ViewModel 或 WPF 命名空间                   │
└──────────────────────────────────────────────────────┘
```

### 1.2 数据流向

```
用户操作 → View（事件） → Command（ViewModel）→ Service → 数据变更
数据变更 → Model 更新 → ViewModel 属性 → INotifyPropertyChanged → View 刷新
```

关键：**单向数据流**。View 不直接改 Model，Model 不直接通知 View。

### 1.3 为什么 ViewModel 不能持有控件引用

- 单元测试无法在无 UI 环境运行（会抛 `InvalidOperationException`）
- 破坏可测试性，ViewModel 的核心价值就是"不依赖 UI 可独立测试"
- 正确做法：通过属性绑定、命令、消息总线间接驱动 View

---

## 二、CommunityToolkit.Mvvm Source Generator 机制

### 2.1 partial class 是关键

```csharp
// 你写的代码（MyViewModel.cs）
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
}
```

Source Generator 在编译期自动生成（你看不见但真实存在）：

```csharp
// 自动生成的代码（概念示意）
public partial class MyViewModel
{
    public string Name
    {
        get => _name;
        set
        {
            if (!EqualityComparer<string>.Default.Equals(_name, value))
            {
                OnNameChanging(value);
                OnPropertyChanging("Name");
                _name = value;
                OnNameChanged(value);
                OnPropertyChanged("Name");
            }
        }
    }

    // 留给你扩展的分部方法（不实现就零开销）
    partial void OnNameChanging(string value);
    partial void OnNameChanged(string value);
}
```

**字段命名规则**：必须是 `_camelCase`（下划线前缀），生成属性为 `PascalCase`。

### 2.2 [ObservableProperty] 高级特性

```csharp
public partial class DeviceViewModel : ObservableObject
{
    // 变更时同时通知另一个属性（常用于计算属性）
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private float _voltage;

    public string DisplayText => $"{_voltage:F2} V / {_current:F2} A";

    // 变更时刷新命令的 CanExecute 状态
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private bool _isConnected;

    // 数据验证（需继承 ObservableValidator）
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, 1000, ErrorMessage = "电压范围 0~1000V")]
    private float _setVoltage;

    // 生成的 Changing/Changed 钩子
    partial void OnVoltageChanging(float oldValue, float newValue)
    {
        // oldValue 是变更前的值（需 CommunityToolkit 8.2+）
    }

    partial void OnVoltageChanged(float value)
    {
        FlushToRegisters();  // 写入 Modbus 寄存器
    }
}
```

### 2.3 [RelayCommand] 高级特性

```csharp
public partial class MasterViewModel : ObservableObject
{
    // 异步命令：自动处理 IsBusy，防止重复点击
    [RelayCommand]
    private async Task PollOnceAsync(CancellationToken token)
    {
        // 传入 CancellationToken，支持 CancelCommand 取消
        await _tcpService.PollAsync(token);
    }
    // 自动生成：PollOnceCommand（AsyncRelayCommand）
    // 自动生成：PollOnceCancelCommand（取消命令）

    // CanExecute 绑定到方法
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync() { ... }

    private bool CanConnect() => !IsConnected && !string.IsNullOrEmpty(IpAddress);

    // CanExecute 绑定到属性（布尔属性简写）
    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private void Export() { ... }

    public bool IsNotBusy => !IsBusy;
}
```

```xml
<!-- 异步命令的取消按钮 -->
<Button Content="轮询" Command="{Binding PollOnceCommand}"/>
<Button Content="取消" Command="{Binding PollOnceCancelCommand}"/>

<!-- 用 IsRunning 显示加载状态 -->
<ProgressBar Visibility="{Binding PollOnceCommand.IsRunning, Converter={StaticResource BoolToVis}}"/>
```

---

## 三、ObservableObject 继承体系

```
ObservableObject                    ← 基础，实现 INotifyPropertyChanged
    └── ObservableRecipient         ← 额外支持 Messenger 消息收发
            └── ObservableValidator ← 额外支持 INotifyDataErrorInfo 数据验证
```

### 3.1 ObservableRecipient（消息收发）

```csharp
public partial class SlaveViewModel : ObservableRecipient
{
    // 构造函数激活消息接收
    public SlaveViewModel() : base()
    {
        IsActive = true;  // 激活后才会收到消息
    }

    // 注册消息处理器
    protected override void OnActivated()
    {
        Messenger.Register<DeviceStatusMessage>(this, (r, msg) =>
        {
            StatusText = msg.Status;
        });
    }

    // 发送消息
    private void NotifyError(string error)
    {
        Messenger.Send(new ErrorOccurredMessage(error));
    }
}
```

### 3.2 ObservableValidator（数据验证）

```csharp
public partial class ConfigViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "IP 地址不能为空")]
    [RegularExpression(@"^\d{1,3}(\.\d{1,3}){3}$", ErrorMessage = "IP 格式错误")]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, 65535, ErrorMessage = "端口范围 1~65535")]
    private int _port = 502;

    [RelayCommand]
    private void Save()
    {
        ValidateAllProperties();  // 触发全部验证
        if (HasErrors) return;    // HasErrors 来自 INotifyDataErrorInfo
        // 保存逻辑...
    }
}
```

```xml
<!-- 显示验证错误 -->
<TextBox Text="{Binding IpAddress, UpdateSourceTrigger=PropertyChanged,
                         ValidatesOnNotifyDataErrors=True}"/>
<!-- 错误提示样式（在 Style.Triggers 中用 Validation.HasError） -->
<Style TargetType="TextBox">
    <Style.Triggers>
        <Trigger Property="Validation.HasError" Value="True">
            <Setter Property="BorderBrush" Value="Red"/>
            <Setter Property="ToolTip"
                    Value="{Binding (Validation.Errors)[0].ErrorContent,
                                    RelativeSource={RelativeSource Self}}"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

---

## 四、WeakReferenceMessenger 消息总线

解耦 ViewModel 之间、ViewModel 与 View 之间的通信，避免直接引用。

### 4.1 定义消息类型

```csharp
// 消息类：推荐用 record（只读，值语义）
public record DeviceConnectedMessage(string DeviceName, bool Success);
public record ShowAlarmMessage(string Title, string Content, AlarmLevel Level);
public record NavigateMessage(string TargetPage);
```

### 4.2 发送与接收

```csharp
// ViewModel A：发送
WeakReferenceMessenger.Default.Send(new DeviceConnectedMessage("PCS-01", true));

// ViewModel B：注册接收（在构造函数或 OnActivated 中）
WeakReferenceMessenger.Default.Register<DeviceConnectedMessage>(this, (recipient, msg) =>
{
    if (msg.Success)
        StatusLog.Add($"{msg.DeviceName} 连接成功");
});

// 取消注册（避免内存泄漏）
WeakReferenceMessenger.Default.UnregisterAll(this);
// 或在 Dispose/窗口关闭时调用
```

### 4.3 请求-响应模式（IRequestMessage）

```csharp
// 定义带返回值的消息
public class ConfirmDeleteRequest : RequestMessage<bool>
{
    public string ItemName { get; }
    public ConfirmDeleteRequest(string name) => ItemName = name;
}

// View 层注册响应（处理 UI 交互）
WeakReferenceMessenger.Default.Register<ConfirmDeleteRequest>(this, (r, msg) =>
{
    var result = MessageBox.Show($"确认删除 {msg.ItemName}？",
                                  "确认", MessageBoxButton.YesNo);
    msg.Reply(result == MessageBoxResult.Yes);
});

// ViewModel 层发送并等待响应
var reply = WeakReferenceMessenger.Default.Send(new ConfirmDeleteRequest("设备A"));
if (reply.Response)
    Items.Remove(selectedItem);
```

### 4.4 WeakReference vs StrongReference

| | WeakReferenceMessenger | StrongReferenceMessenger |
|--|--|--|
| 持有引用 | 弱引用 | 强引用 |
| 内存泄漏风险 | 低（GC 可回收） | 高（需手动 Unregister） |
| 推荐场景 | 默认使用 | 长生命周期、高频消息 |

---

## 五、依赖注入（DI）与 ViewModel 生命周期

### 5.1 注册服务和 ViewModel

```csharp
// App.xaml.cs
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        // 单例：整个应用生命周期共享一个实例
        services.AddSingleton<IModbusService, ModbusTcpService>();
        services.AddSingleton<IRegisterMapService, RegisterMapService>();

        // 单例 ViewModel（主窗口）
        services.AddSingleton<MainViewModel>();

        // 瞬态 ViewModel（每次打开对话框创建新实例）
        services.AddTransient<ConfigDialogViewModel>();

        // 注册 View（让 DI 容器创建并注入 ViewModel）
        services.AddSingleton<MainWindow>();

        Services = services.BuildServiceProvider();

        var window = Services.GetRequiredService<MainWindow>();
        window.Show();
    }
}
```

```csharp
// MainWindow.xaml.cs
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;  // 构造函数注入
    }
}
```

### 5.2 从 DI 容器获取瞬态 ViewModel

```csharp
// 打开配置对话框时
[RelayCommand]
private void OpenConfig()
{
    var vm = App.Services.GetRequiredService<ConfigDialogViewModel>();
    var dialog = new ConfigDialog { DataContext = vm };
    dialog.ShowDialog();
}
```

### 5.3 ViewModel 的清理

```csharp
public partial class DeviceViewModel : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _cts.Cancel();
        _cts.Dispose();
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _disposed = true;
    }
}
```

---

## 六、集合与列表 ViewModel 模式

### 6.1 ObservableCollection 的正确用法

```csharp
public partial class LogViewModel : ObservableObject
{
    // 对外暴露只读接口，防止 View 直接 Add/Clear
    public ObservableCollection<LogEntry> Logs { get; } = new();

    // 线程安全写法（后台线程添加日志）
    private readonly object _logLock = new();

    public LogViewModel()
    {
        // 方案：全局开启集合同步，允许后台线程操作
        BindingOperations.EnableCollectionSynchronization(Logs, _logLock);
    }

    public void AddLog(string message, LogLevel level)
    {
        lock (_logLock)
        {
            Logs.Add(new LogEntry(DateTime.Now, message, level));
            // 限制条数，防止内存无限增长
            while (Logs.Count > 1000)
                Logs.RemoveAt(0);
        }
    }
}
```

### 6.2 选中项模式

```csharp
public partial class DeviceListViewModel : ObservableObject
{
    public ObservableCollection<DeviceItem> Devices { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    private DeviceItem? _selectedDevice;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete()
    {
        Devices.Remove(_selectedDevice!);
        SelectedDevice = null;
    }

    private bool HasSelection => _selectedDevice != null;
}
```

```xml
<ListBox ItemsSource="{Binding Devices}"
         SelectedItem="{Binding SelectedDevice}"/>
<Button Content="删除" Command="{Binding DeleteCommand}"/>
```

### 6.3 分组显示

```csharp
// 使用 CollectionViewSource 分组（在 ViewModel 中）
public ICollectionView GroupedDevices { get; }

public DeviceViewModel()
{
    var source = new CollectionViewSource { Source = Devices };
    source.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DeviceItem.Category)));
    source.SortDescriptions.Add(new SortDescription(nameof(DeviceItem.Name), ListSortDirection.Ascending));
    GroupedDevices = source.View;
}
```

---

## 七、ViewModel 间通信的四种模式

### 7.1 直接属性引用（父子关系）

```csharp
// 父 ViewModel 持有子 ViewModel
public partial class MainViewModel : ObservableObject
{
    public PcsViewModel Pcs { get; } = new();
    public BmsViewModel Bms { get; } = new();
}
```

```xml
<!-- 子面板绑定到子 ViewModel -->
<local:PcsPanel DataContext="{Binding Pcs}"/>
```

### 7.2 消息总线（无直接依赖）

适合跨层、跨模块的松耦合通信（见第四节）。

### 7.3 共享 Service 单例

```csharp
// 两个 ViewModel 共享同一个 Service 实例
// ViewModel A
public class PcsViewModel(IRegisterMapService regService) : ObservableObject { }

// ViewModel B  
public class BmsViewModel(IRegisterMapService regService) : ObservableObject { }

// 注册为单例，DI 容器保证是同一个实例
services.AddSingleton<IRegisterMapService, RegisterMapService>();
```

### 7.4 事件（简单场景）

```csharp
// ViewModel 暴露事件
public event EventHandler<string>? ExportCompleted;

// View 在 DataContext 设置后订阅
void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
{
    if (e.OldValue is MyViewModel oldVm)
        oldVm.ExportCompleted -= OnExportCompleted;
    if (e.NewValue is MyViewModel newVm)
        newVm.ExportCompleted += OnExportCompleted;
}
```

---

## 八、导航模式（多页面/多面板）

### 8.1 ContentControl + DataTemplate 路由

```csharp
// 主 ViewModel
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject _currentPage;

    [RelayCommand]
    private void NavigateTo(string page)
    {
        CurrentPage = page switch
        {
            "PCS"   => App.Services.GetRequiredService<PcsViewModel>(),
            "BMS"   => App.Services.GetRequiredService<BmsViewModel>(),
            _       => throw new ArgumentException($"未知页面: {page}")
        };
    }
}
```

```xml
<!-- App.xaml 或 MainWindow.xaml 中注册 DataTemplate -->
<DataTemplate DataType="{x:Type vm:PcsViewModel}">
    <views:PcsPanel/>
</DataTemplate>
<DataTemplate DataType="{x:Type vm:BmsViewModel}">
    <views:BmsPanel/>
</DataTemplate>

<!-- 主窗口显示当前页面 -->
<ContentControl Content="{Binding CurrentPage}"/>
```

### 8.2 TabControl 多标签

```xml
<TabControl ItemsSource="{Binding DeviceTabs}"
            SelectedItem="{Binding ActiveTab}">
    <TabControl.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Title}"/>
        </DataTemplate>
    </TabControl.ItemTemplate>
    <TabControl.ContentTemplate>
        <DataTemplate>
            <!-- 根据类型自动路由到对应 View -->
            <ContentPresenter Content="{Binding}"/>
        </DataTemplate>
    </TabControl.ContentTemplate>
</TabControl>
```

---

## 九、单元测试 ViewModel

CommunityToolkit.Mvvm 的核心优势之一：ViewModel 无 UI 依赖，可直接在测试项目中实例化。

### 9.1 基础属性测试

```csharp
public class PcsViewModelTests
{
    [Fact]
    public void SetVoltage_ShouldClampToMax()
    {
        var vm = new PcsViewModel(Mock.Of<IModbusService>());
        vm.Voltage = 9999f;  // 超量程
        Assert.Equal(1000f, vm.Voltage);  // 截断到上限
    }

    [Fact]
    public void SetVoltage_ShouldRaisePropertyChanged()
    {
        var vm = new PcsViewModel(Mock.Of<IModbusService>());
        bool raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.Voltage))
                raised = true;
        };
        vm.Voltage = 100f;
        Assert.True(raised);
    }
}
```

### 9.2 命令测试

```csharp
[Fact]
public async Task ConnectCommand_ShouldSetIsConnected()
{
    var mockService = new Mock<IModbusService>();
    mockService.Setup(s => s.ConnectAsync()).Returns(Task.CompletedTask);

    var vm = new MasterViewModel(mockService.Object);
    await vm.ConnectCommand.ExecuteAsync(null);

    Assert.True(vm.IsConnected);
    mockService.Verify(s => s.ConnectAsync(), Times.Once);
}

[Fact]
public void ConnectCommand_CannotExecute_WhenAlreadyConnected()
{
    var vm = new MasterViewModel(Mock.Of<IModbusService>());
    vm.IsConnected = true;
    Assert.False(vm.ConnectCommand.CanExecute(null));
}
```

### 9.3 消息测试

```csharp
[Fact]
public void DeleteCommand_ShouldSendConfirmMessage()
{
    bool messageSent = false;
    WeakReferenceMessenger.Default.Register<ConfirmDeleteRequest>(this, (r, msg) =>
    {
        messageSent = true;
        msg.Reply(false);  // 模拟用户取消
    });

    var vm = new DeviceListViewModel();
    vm.SelectedDevice = new DeviceItem { Name = "测试设备" };
    vm.DeleteCommand.Execute(null);

    Assert.True(messageSent);
    Assert.Single(vm.Devices);  // 取消后设备未被删除
    WeakReferenceMessenger.Default.UnregisterAll(this);
}
```

---

## 十、本项目 MVVM 约定速查

### 10.1 ViewModel 文件结构规范

```csharp
public partial class XxxViewModel : ObservableObject
{
    // ── 1. 私有字段（Service 依赖） ──────────────────
    private readonly IModbusService _modbusService;
    private readonly object _lock = new();

    // ── 2. 构造函数 ──────────────────────────────────
    public XxxViewModel(IModbusService modbusService)
    {
        _modbusService = modbusService;
    }

    // ── 3. 可观察属性（[ObservableProperty]） ────────
    [ObservableProperty] private float _voltage;
    [ObservableProperty] private bool _isConnected;

    // ── 4. 属性变更钩子 ──────────────────────────────
    partial void OnVoltageChanged(float value) => FlushToRegisters();

    // ── 5. 计算属性（只读） ──────────────────────────
    public string DisplayStatus => _isConnected ? "在线" : "离线";

    // ── 6. 命令 ──────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync() { ... }

    private bool CanConnect() => !_isConnected;

    // ── 7. 私有方法 ──────────────────────────────────
    private void FlushToRegisters()
    {
        lock (_lock)
        {
            // 写入寄存器...
        }
    }
}
```

### 10.2 常见反模式（禁止）

```csharp
// ❌ ViewModel 引用 UI 类型
private Button _myButton;  // 禁止

// ❌ View.xaml.cs 包含业务逻辑
private void Button_Click(object sender, RoutedEventArgs e)
{
    _device.Voltage = 100;  // 业务逻辑应在 ViewModel
}

// ❌ 属性 Setter 直接操作集合（跨线程）
set { _logs.Add(value); }  // 后台线程调用会崩溃

// ❌ 使用 Thread.Sleep 在 ViewModel
Thread.Sleep(1000);  // 阻塞 UI 线程，应改用 await Task.Delay

// ❌ 忘记 partial 关键字
public class MyViewModel : ObservableObject  // Source Generator 不生效！
// 必须是：public partial class MyViewModel
```

### 10.3 属性命名对照

| 字段（你写） | 生成属性（绑定用） | 变更钩子 |
|---|---|---|
| `_voltage` | `Voltage` | `OnVoltageChanged(float value)` |
| `_isConnected` | `IsConnected` | `OnIsConnectedChanged(bool value)` |
| `_selectedItem` | `SelectedItem` | `OnSelectedItemChanged(T? value)` |
| `_statusText` | `StatusText` | `OnStatusTextChanged(string value)` |

---

*最后更新：2026-04-22*
