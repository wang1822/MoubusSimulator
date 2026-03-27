namespace SimulatorApp.Services;

public interface ISlaveService : IModbusService
{
    byte   SlaveId     { get; set; }
    int    Port        { get; set; }
    string BindAddress { get; set; }
    string ComPort     { get; set; }
    int    BaudRate    { get; set; }
}
