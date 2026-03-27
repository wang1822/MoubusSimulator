using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SimulatorApp.Views.Controls;

public partial class FieldRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FieldRow),
            new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit), typeof(string), typeof(FieldRow),
            new PropertyMetadata(string.Empty, OnUnitChanged));

    public static readonly DependencyProperty ValuePathProperty =
        DependencyProperty.Register(nameof(ValuePath), typeof(string), typeof(FieldRow),
            new PropertyMetadata(string.Empty, OnValuePathChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public string ValuePath
    {
        get => (string)GetValue(ValuePathProperty);
        set => SetValue(ValuePathProperty, value);
    }

    public FieldRow()
    {
        InitializeComponent();
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((FieldRow)d).LabelText.Text = (string)e.NewValue;

    private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((FieldRow)d).UnitText.Text = (string)e.NewValue;

    private static void OnValuePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var row = (FieldRow)d;
        var path = (string)e.NewValue;
        if (!string.IsNullOrEmpty(path))
        {
            var binding = new Binding(path) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            row.ValueBox.SetBinding(TextBox.TextProperty, binding);
        }
    }
}
