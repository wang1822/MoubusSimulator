using System.Windows;
using SimulatorApp.ViewModels;

namespace SimulatorApp.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
