namespace SimulatorApp.Helpers;

/// <summary>float32 ↔ 两个 uint16，AB CD 字序（Big-Endian 字）。</summary>
public static class FloatRegisterHelper
{
    public static (ushort High, ushort Low) ToRegisters(float value)
    {
        byte[] b = BitConverter.GetBytes(value);
        ushort high = (ushort)((b[3] << 8) | b[2]);
        ushort low  = (ushort)((b[1] << 8) | b[0]);
        return (high, low);
    }

    public static float FromRegisters(ushort high, ushort low)
    {
        byte[] b =
        {
            (byte)(low  & 0xFF), (byte)(low  >> 8),
            (byte)(high & 0xFF), (byte)(high >> 8)
        };
        return BitConverter.ToSingle(b, 0);
    }
}
