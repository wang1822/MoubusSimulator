using SimulatorApp.Master.Models;
using SimulatorApp.Master.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SimulatorApp.Master.Views;

/// <summary>
/// 主站面板 code-behind — DataContext 为 MasterViewModel
/// </summary>
public partial class MasterPanel : UserControl
{
    public MasterPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MasterViewModel vm)
            vm.ScrollRequested += OnScrollRequested;
    }

    /// <summary>
    /// 响应 ViewModel 的滚动请求：选中行并 ScrollIntoView，焦点跟随。
    /// </summary>
    private void OnScrollRequested(RegisterDisplayRow row)
    {
        if (DataContext is not MasterViewModel vm) return;
        var grid = vm.ActiveTabIndex == 0 ? TelGrid : CtrlGrid;
        grid.SelectedItem = row;
        grid.ScrollIntoView(row);
        grid.Focus();
    }
}
