using SimulatorApp.Services;

namespace SimulatorApp.Models;

public abstract class DeviceModelBase
{
    public abstract string DeviceName   { get; }
    public abstract int    BaseAddress  { get; }
    public byte            SlaveId      { get; set; } = 1;

    public abstract void ToRegisters(RegisterBank bank);
    public abstract void FromRegisters(RegisterBank bank);
}
