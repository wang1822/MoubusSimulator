using SimulatorApp.Helpers;

namespace SimulatorApp.Services;

/// <summary>65536 个 Holding Register 线程安全内存池。</summary>
public class RegisterBank
{
    private readonly ushort[] _regs = new ushort[65536];
    private readonly object   _lock = new();

    public void Write(int address, ushort value)
    {
        lock (_lock) { _regs[address] = value; }
    }

    public void WriteFloat32(int address, float value)
    {
        var (hi, lo) = FloatRegisterHelper.ToRegisters(value);
        lock (_lock) { _regs[address] = hi; _regs[address + 1] = lo; }
    }

    public ushort Read(int address)
    {
        lock (_lock) { return _regs[address]; }
    }

    public ushort[] ReadRange(int startAddress, int count)
    {
        lock (_lock)
        {
            var result = new ushort[count];
            Array.Copy(_regs, startAddress, result, 0, count);
            return result;
        }
    }

    public void Clear(int startAddress, int count)
    {
        lock (_lock)
        {
            for (int i = startAddress; i < startAddress + count; i++)
                _regs[i] = 0;
        }
    }
}
