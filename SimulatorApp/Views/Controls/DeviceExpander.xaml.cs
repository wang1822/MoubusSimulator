using System.Windows;
using System.Windows.Controls;

namespace SimulatorApp.Views.Controls;

public partial class DeviceExpander : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(DeviceExpander),
            new PropertyMetadata(string.Empty, (d, e) => ((DeviceExpander)d).TitleText.Text = (string)e.NewValue));

    public static readonly DependencyProperty BodyContentProperty =
        DependencyProperty.Register(nameof(BodyContent), typeof(object), typeof(DeviceExpander),
            new PropertyMetadata(null, (d, e) => ((DeviceExpander)d).Body.Content = e.NewValue));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(DeviceExpander),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, e) => ((DeviceExpander)d).Root.IsExpanded = (bool)e.NewValue));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? BodyContent
    {
        get => GetValue(BodyContentProperty);
        set => SetValue(BodyContentProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public DeviceExpander() => InitializeComponent();
}
