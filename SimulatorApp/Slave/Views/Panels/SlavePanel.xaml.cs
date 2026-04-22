using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimulatorApp.Slave.Views.Panels;

/// <summary>
/// 从站主面板 — 顶部连接配置 + 中间设备切换 + 底部日志
/// DataContext 为 SlaveViewModel
/// </summary>
public partial class SlavePanel : UserControl
{
    public SlavePanel() => InitializeComponent();

    // 阻止 CheckBox 的点击事件冒泡到 ListBoxItem，避免意外切换选中设备
    private void OnSimCheckBoxClick(object sender, RoutedEventArgs e)
        => e.Handled = true;

    // PreviewMouseWheel 拦截：阻止 ListBox 内部 ScrollViewer 吞掉滚轮事件，交由外层 ScrollViewer 处理
    private void OnDeviceListPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scv = (ScrollViewer)sender;
        scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta / 3.0);
        e.Handled = true;
    }
}
