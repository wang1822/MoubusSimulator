# WPF 必备知识

> 适用于 .NET 8 + CommunityToolkit.Mvvm 8.x 项目

---

## 一、架构总览

```
View (.xaml)
  │  DataContext = ViewModel
  ▼
ViewModel (.cs)
  │  ObservableProperty / RelayCommand
  ▼
Model / Service (.cs)
  │  业务逻辑、数据访问
  ▼
数据库 / 网络 / 硬件
```

WPF 的核心是 **数据绑定**：View 只管显示，ViewModel 持有状态和命令，两者通过 `Binding` 自动同步，不需要手动操作控件。

---

## 二、XAML 基础

### 2.1 命名空间声明

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="clr-namespace:MyApp"
    xmlns:vm="clr-namespace:MyApp.ViewModels">
```

### 2.2 常用布局容器

| 容器 | 特点 | 典型用途 |
|------|------|---------|
| `Grid` | 行列网格，最灵活 | 主窗口布局 |
| `StackPanel` | 线性堆叠（Horizontal/Vertical） | 工具栏、表单 |
| `DockPanel` | 子元素靠边停靠，最后填满剩余 | 菜单栏+内容区 |
| `WrapPanel` | 超出宽度自动换行 | 标签、图标列表 |
| `Canvas` | 绝对坐标定位 | 图形绘制 |
| `UniformGrid` | 等分网格 | 数字键盘 |

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>   <!-- 内容高度 -->
        <RowDefinition Height="*"/>      <!-- 占满剩余 -->
        <RowDefinition Height="2*"/>     <!-- 占剩余的 2/3 -->
        <RowDefinition Height="200"/>    <!-- 固定像素 -->
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="200"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <TextBlock Grid.Row="0" Grid.Column="0" Text="左上角"/>
    <Border Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2"/>
</Grid>
```

### 2.3 常用控件

```xml
<!-- 文本 -->
<TextBlock Text="{Binding Status}" FontSize="14" FontWeight="Bold"/>
<TextBox Text="{Binding InputValue, UpdateSourceTrigger=PropertyChanged}"/>
<PasswordBox x:Name="pwdBox"/>

<!-- 按钮 -->
<Button Content="提交" Command="{Binding SubmitCommand}"
        CommandParameter="{Binding SelectedItem}"/>
<ToggleButton IsChecked="{Binding IsEnabled}"/>
<CheckBox IsChecked="{Binding IsActive}" Content="启用"/>
<RadioButton GroupName="mode" IsChecked="{Binding IsModeA}" Content="模式A"/>

<!-- 选择 -->
<ComboBox ItemsSource="{Binding Items}" SelectedItem="{Binding Selected}"
          DisplayMemberPath="Name"/>
<ListBox ItemsSource="{Binding Logs}" SelectedItem="{Binding SelectedLog}"/>

<!-- 数值 -->
<Slider Minimum="0" Maximum="100" Value="{Binding Volume}"/>
<ProgressBar Value="{Binding Progress}" Maximum="100"/>

<!-- 数据表格 -->
<DataGrid ItemsSource="{Binding RegisterList}"
          AutoGenerateColumns="False"
          CanUserAddRows="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="地址" Binding="{Binding Address}"/>
        <DataGridTextColumn Header="值"   Binding="{Binding Value, UpdateSourceTrigger=PropertyChanged}"/>
        <DataGridCheckBoxColumn Header="报警" Binding="{Binding IsAlarm}"/>
    </DataGrid.Columns>
</DataGrid>
```

---

## 三、数据绑定

### 3.1 绑定语法

```xml
<!-- 单向：ViewModel → View -->
<TextBlock Text="{Binding Name, Mode=OneWay}"/>

<!-- 双向：View ↔ ViewModel（TextBox 默认 TwoWay） -->
<TextBox Text="{Binding Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

<!-- 单次：只绑定一次，之后不更新 -->
<TextBlock Text="{Binding InitialValue, Mode=OneTime}"/>

<!-- 静态绑定（不需要 DataContext） -->
<TextBlock Text="{x:Static local:AppConst.Version}"/>
```

### 3.2 UpdateSourceTrigger

| 值 | 含义 |
|----|------|
| `LostFocus`（默认） | 控件失去焦点时更新 ViewModel |
| `PropertyChanged` | 每次键入立刻更新 |
| `Explicit` | 手动调用 `UpdateSource()` |

### 3.3 绑定路径写法

```xml
<!-- 属性嵌套 -->
<TextBlock Text="{Binding Device.Status.Description}"/>

<!-- 集合索引 -->
<TextBlock Text="{Binding Items[0].Name}"/>

<!-- 绑定到自身 -->
<TextBlock Text="{Binding RelativeSource={RelativeSource Self}, Path=ActualWidth}"/>

<!-- 绑定到父容器 -->
<TextBlock Text="{Binding RelativeSource={RelativeSource AncestorType=Window}, Path=Title}"/>

<!-- 绑定到同级元素 -->
<TextBlock Text="{Binding ElementName=mySlider, Path=Value}"/>
```

### 3.4 Converter（值转换器）

```csharp
// 实现接口
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => (Visibility)value == Visibility.Visible;
}
```

```xml
<!-- 在 Resources 里注册 -->
<Window.Resources>
    <local:BoolToVisibilityConverter x:Key="BoolToVis"/>
</Window.Resources>

<!-- 使用 -->
<TextBlock Visibility="{Binding IsConnected, Converter={StaticResource BoolToVis}}"/>
```

常用内置 Converter（需手动实现或用第三方库）：
- `BoolToVisibility`
- `NullToVisibility`
- `EnumToDescription`
- `NumberFormat`（StringFormat 更简便）

```xml
<!-- StringFormat 直接格式化 -->
<TextBlock Text="{Binding Voltage, StringFormat={}{0:F2} V}"/>
<TextBlock Text="{Binding UpdateTime, StringFormat=yyyy-MM-dd HH:mm:ss}"/>
```

---

## 四、MVVM 与 CommunityToolkit.Mvvm

### 4.1 ObservableObject 基类

```csharp
// 所有 ViewModel 继承此类
public partial class MyViewModel : ObservableObject
{
    // 生成 Name 属性 + PropertyChanged 通知
    [ObservableProperty]
    private string _name = string.Empty;

    // 属性变更回调
    partial void OnNameChanged(string value)
    {
        // value 是新值，可在此触发联动逻辑
    }

    // 带验证的属性
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "不能为空")]
    [Range(0, 100, ErrorMessage = "范围 0-100")]
    private int _value;
}
```

### 4.2 RelayCommand

```csharp
public partial class MyViewModel : ObservableObject
{
    // 无参命令
    [RelayCommand]
    private void Connect()
    {
        // 同步执行
    }

    // 带参数命令
    [RelayCommand]
    private void DeleteItem(RegisterItem item)
    {
        Items.Remove(item);
    }

    // 异步命令（自动处理 IsBusy）
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await Task.Delay(1000);
    }

    // 带 CanExecute 条件
    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Submit() { }

    private bool CanSubmit() => !string.IsNullOrEmpty(_name);

    // 手动刷新 CanExecute（属性变更时调用）
    partial void OnNameChanged(string value)
        => SubmitCommand.NotifyCanExecuteChanged();
}
```

```xml
<!-- 绑定命令 -->
<Button Content="连接" Command="{Binding ConnectCommand}"/>
<Button Content="删除" Command="{Binding DeleteItemCommand}"
        CommandParameter="{Binding SelectedItem}"/>
```

### 4.3 设置 DataContext 的三种方式

**方式一：代码后台（简单直接）**
```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
```

**方式二：XAML 声明（设计器可预览）**
```xml
<Window.DataContext>
    <vm:MainViewModel/>
</Window.DataContext>
```

**方式三：依赖注入（推荐生产用法）**
```csharp
// App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    var services = new ServiceCollection();
    services.AddSingleton<MainViewModel>();
    services.AddSingleton<MainWindow>();
    var provider = services.BuildServiceProvider();

    var window = provider.GetRequiredService<MainWindow>();
    window.DataContext = provider.GetRequiredService<MainViewModel>();
    window.Show();
}
```

---

## 五、样式与模板

### 5.1 Style

```xml
<Window.Resources>
    <!-- 有 Key：显式使用 -->
    <Style x:Key="DangerButton" TargetType="Button">
        <Setter Property="Background" Value="Red"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="FontWeight" Value="Bold"/>
    </Style>

    <!-- 无 Key：作用于当前范围内所有同类控件 -->
    <Style TargetType="TextBlock">
        <Setter Property="FontSize" Value="13"/>
    </Style>

    <!-- 继承样式 -->
    <Style x:Key="LargeButton" TargetType="Button"
           BasedOn="{StaticResource DangerButton}">
        <Setter Property="FontSize" Value="18"/>
    </Style>
</Window.Resources>

<Button Style="{StaticResource DangerButton}" Content="删除"/>
```

### 5.2 Trigger

```xml
<Style TargetType="Button">
    <Setter Property="Background" Value="LightGray"/>
    <Style.Triggers>
        <!-- 属性触发 -->
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="SteelBlue"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Trigger>
        <!-- 数据触发 -->
        <DataTrigger Binding="{Binding IsConnected}" Value="True">
            <Setter Property="Background" Value="Green"/>
            <Setter Property="Content" Value="已连接"/>
        </DataTrigger>
        <!-- 多条件触发 -->
        <MultiDataTrigger>
            <MultiDataTrigger.Conditions>
                <Condition Binding="{Binding IsAlarm}" Value="True"/>
                <Condition Binding="{Binding IsActive}" Value="True"/>
            </MultiDataTrigger.Conditions>
            <Setter Property="Foreground" Value="Red"/>
        </MultiDataTrigger>
    </Style.Triggers>
</Style>
```

### 5.3 ControlTemplate（完全重写控件外观）

```xml
<Style TargetType="Button">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}"
                        CornerRadius="4"
                        Padding="8,4">
                    <ContentPresenter HorizontalAlignment="Center"
                                      VerticalAlignment="Center"/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsPressed" Value="True">
                        <Setter Property="Opacity" Value="0.7"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### 5.4 DataTemplate（列表项外观）

```xml
<!-- ListBox 每一项的显示模板 -->
<ListBox ItemsSource="{Binding Devices}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Margin="4">
                <Ellipse Width="10" Height="10"
                         Fill="{Binding IsOnline, Converter={StaticResource BoolToColor}}"/>
                <TextBlock Text="{Binding Name}" Margin="6,0,0,0"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

---

## 六、资源字典与主题

### 6.1 App.xaml 合并资源字典

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Themes/Colors.xaml"/>
            <ResourceDictionary Source="Themes/Styles.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 6.2 StaticResource vs DynamicResource

| | StaticResource | DynamicResource |
|--|---|---|
| 解析时机 | XAML 加载时（一次） | 运行时每次引用 |
| 性能 | 更快 | 稍慢 |
| 支持运行时换主题 | 否 | 是 |
| 适用场景 | 固定颜色/尺寸 | 动态主题切换 |

---

## 七、事件与命令

### 7.1 事件转命令（EventToCommand）

```xml
<!-- 需要引用 Microsoft.Xaml.Behaviors.Wpf 包 -->
xmlns:b="http://schemas.microsoft.com/xaml/behaviors"

<ListBox ItemsSource="{Binding Items}">
    <b:Interaction.Triggers>
        <b:EventTrigger EventName="SelectionChanged">
            <b:InvokeCommandAction Command="{Binding SelectionChangedCommand}"
                                   PassEventArgsToCommand="True"/>
        </b:EventTrigger>
    </b:Interaction.Triggers>
</ListBox>
```

### 7.2 直接在代码后台处理（适合纯 View 逻辑）

```csharp
// 滚动到底部（UI 行为，不属于 ViewModel 职责）
private void LogListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (sender is ListBox lb && lb.Items.Count > 0)
        lb.ScrollIntoView(lb.Items[^1]);
}
```

---

## 八、线程安全与 UI 更新

WPF 控件只能在 **UI 线程**上更新，后台线程操作集合或控件会抛异常。

### 8.1 Dispatcher 调度

```csharp
// 方式一：直接调度
Application.Current.Dispatcher.Invoke(() =>
{
    StatusText = "已连接";
    Logs.Add("接收到数据");
});

// 方式二：异步调度（不阻塞后台线程）
Application.Current.Dispatcher.BeginInvoke(() =>
{
    Logs.Add(message);
});
```

### 8.2 ObservableCollection 线程安全

```csharp
// ObservableCollection 跨线程添加元素会报错
// 方案一：Dispatcher（推荐）
Application.Current.Dispatcher.Invoke(() => Logs.Add(msg));

// 方案二：启用集合绑定同步（App 启动时全局设置）
BindingOperations.EnableCollectionSynchronization(Logs, _lock);
// 之后可在任意线程操作 Logs
```

### 8.3 async/await 在 ViewModel 中的正确姿势

```csharp
[RelayCommand]
private async Task ConnectAsync()
{
    IsConnecting = true;
    try
    {
        // 异步操作在后台线程
        await _service.ConnectAsync();
        // await 之后自动回到 UI 线程（SynchronizationContext）
        StatusText = "连接成功";
    }
    catch (Exception ex)
    {
        StatusText = $"连接失败：{ex.Message}";
    }
    finally
    {
        IsConnecting = false;
    }
}
```

> **关键**：WPF 的 `SynchronizationContext` 确保 `await` 后的代码回到 UI 线程，所以 `async` 方法中 `await` 后直接赋值属性是安全的。

---

## 九、窗口与对话框

### 9.1 打开子窗口

```csharp
// 模态（阻塞，等待关闭）
var dialog = new SettingsWindow();
dialog.Owner = Application.Current.MainWindow;
bool? result = dialog.ShowDialog();
if (result == true) { /* 用户点了确认 */ }

// 非模态（不阻塞）
var win = new MonitorWindow();
win.Show();
```

### 9.2 关闭时返回结果

```csharp
// 子窗口代码后台
private void ConfirmButton_Click(object sender, RoutedEventArgs e)
{
    DialogResult = true;  // ShowDialog() 返回 true
    Close();
}
```

### 9.3 系统对话框

```csharp
// 文件选择
var dlg = new Microsoft.Win32.OpenFileDialog
{
    Filter = "Excel 文件|*.xlsx|所有文件|*.*",
    Multiselect = false
};
if (dlg.ShowDialog() == true)
    FilePath = dlg.FileName;

// 文件保存
var saveDlg = new Microsoft.Win32.SaveFileDialog
{
    Filter = "JSON 文件|*.json",
    DefaultExt = ".json"
};
if (saveDlg.ShowDialog() == true)
    await SaveAsync(saveDlg.FileName);
```

---

## 十、常用技巧与踩坑

### 10.1 DataGrid 列宽自适应

```xml
<DataGrid ColumnWidth="*">
    <!-- 所有列平分宽度 -->
</DataGrid>

<!-- 混合：固定 + 自适应 -->
<DataGridTextColumn Width="80"  Header="地址"/>
<DataGridTextColumn Width="*"   Header="名称"/>
<DataGridTextColumn Width="Auto" Header="单位"/>
```

### 10.2 虚拟化（大量数据性能优化）

```xml
<!-- ListBox/DataGrid 默认开启 UI 虚拟化，不要关闭 -->
<!-- 如果自定义 ItemsPanel 关闭了虚拟化，手动开启 -->
<ListBox VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling"
         ScrollViewer.IsDeferredScrollingEnabled="True"/>
```

### 10.3 绑定调试

```xml
<!-- 在绑定表达式加 PresentationTraceSources -->
xmlns:diag="clr-namespace:System.Diagnostics;assembly=WindowsBase"

<TextBlock Text="{Binding Name,
    diag:PresentationTraceSources.TraceLevel=High}"/>
```

```csharp
// 在 Output 窗口过滤 "System.Windows.Data"，查看绑定错误
// 常见错误：
// BindingExpression path error: 'XXX' property not found
// → 检查属性名拼写、DataContext 是否正确设置
```

### 10.4 常见踩坑

| 场景 | 问题 | 解决 |
|------|------|------|
| 列表不刷新 | 用 `List<T>` 而非 `ObservableCollection<T>` | 改用 `ObservableCollection<T>` |
| 命令不触发 | CanExecute 返回 false | 调用 `XxxCommand.NotifyCanExecuteChanged()` |
| 绑定不更新 | 属性没有 `PropertyChanged` 通知 | 使用 `[ObservableProperty]` 或手动 `OnPropertyChanged()` |
| 跨线程异常 | 后台线程操作 UI | 用 `Dispatcher.Invoke` 切回 UI 线程 |
| 内存泄漏 | 事件订阅未取消 | 用 `WeakEventManager` 或在 `Unloaded` 事件取消订阅 |
| DataGrid 编辑卡死 | 绑定源异常未处理 | 实现 `INotifyDataErrorInfo` 或用 try-catch 保护 Setter |
| 窗口最小化闪烁 | `AllowsTransparency=True` | 非必要不开透明，或改用 `WindowChrome` |

### 10.5 ViewModel 与 View 解耦通信

```csharp
// 方案一：CommunityToolkit.Mvvm 消息总线
// 发送
WeakReferenceMessenger.Default.Send(new ShowDialogMessage("确认删除？"));

// 接收（View 或其他 ViewModel）
WeakReferenceMessenger.Default.Register<ShowDialogMessage>(this, (r, msg) =>
{
    MessageBox.Show(msg.Text);
});

// 方案二：事件（简单场景）
public event Action<string>? RequestShowMessage;
// ViewModel 触发：RequestShowMessage?.Invoke("操作成功");
// View 订阅：vm.RequestShowMessage += msg => MessageBox.Show(msg);
```

---

## 十一、本项目约定速查

```csharp
// ✅ 正确：ObservableProperty + FlushToRegisters
[ObservableProperty]
private float _voltage;
partial void OnVoltageChanged(float value) => FlushToRegisters();

// ✅ 正确：命令异步，异常 catch 到属性
[RelayCommand]
private async Task ConnectAsync()
{
    try { await _tcpService.ConnectAsync(); }
    catch (Exception ex) { ErrorMessage = ex.Message; }
}

// ✅ 正确：集合操作回 UI 线程
Application.Current.Dispatcher.Invoke(() => Logs.Add(entry));

// ❌ 错误：直接跨线程修改集合
Task.Run(() => Logs.Add(entry));  // 会崩溃

// ❌ 错误：bitmask 用 +=
AlarmWord += AlarmItem.Bit;   // 错误，重复赋值会累加
AlarmWord |= AlarmItem.Bit;   // 正确
```

---

*最后更新：2026-04-22*
