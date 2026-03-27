namespace SimulatorApp.Models;

public class SlaveEndpoint
{
    public string   Host         { get; set; } = "127.0.0.1";
    public int      Port         { get; set; } = 502;
    public string   ComPort      { get; set; } = "COM1";
    public int      BaudRate     { get; set; } = 9600;
    public byte     SlaveId      { get; set; } = 1;
    public int      StartAddress { get; set; } = 0;
    public int      RegisterCount{ get; set; } = 100;
    public ProtocolType Protocol { get; set; } = ProtocolType.Tcp;
}
