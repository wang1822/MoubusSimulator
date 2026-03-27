using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace SimulatorApp.Views.Controls;

public partial class AlarmCheckList : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(AlarmCheckList),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public AlarmCheckList() => InitializeComponent();

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AlarmCheckList)d).AlarmItems.ItemsSource = (IEnumerable?)e.NewValue;
}
