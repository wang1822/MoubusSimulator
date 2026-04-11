namespace SimulatorApp.Master.Models;

/// <summary>
/// 状态/故障码与中文文本的映射（对应数据库 MasterStatusMappings 表）
/// </summary>
public class MasterStatusMapping
{
    public int    Id               { get; set; }
    public int    RegisterConfigId { get; set; }
    public int    StatusValue      { get; set; }
    public string StatusText       { get; set; } = string.Empty;
}
