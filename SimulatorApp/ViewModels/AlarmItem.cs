using CommunityToolkit.Mvvm.ComponentModel;

namespace SimulatorApp.ViewModels;

/// <summary>告警/故障 bitmask 条目，供 CheckBox 列表绑定。</summary>
public partial class AlarmItem : ObservableObject
{
    [ObservableProperty]
    private bool _isChecked;

    public string Label   { get; }
    public int    BitMask { get; }

    public AlarmItem(string label, int bitMask)
    {
        Label   = label;
        BitMask = bitMask;
    }
}
