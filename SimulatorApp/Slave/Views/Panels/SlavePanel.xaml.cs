using System.Windows;
using System.Windows.Controls;

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
}
