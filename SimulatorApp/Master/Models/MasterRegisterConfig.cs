namespace SimulatorApp.Master.Models;

/// <summary>
/// 主站寄存器字段配置（对应数据库 MasterRegisterConfigs 表）
/// </summary>
public class MasterRegisterConfig
{
    public int    Id               { get; set; }
    public int    StationId        { get; set; }
    public int    StartAddress     { get; set; }
    public int    Quantity         { get; set; } = 1;
    public string VariableName     { get; set; } = string.Empty;
    public string ChineseName      { get; set; } = string.Empty;
    /// <summary>"R" 只读 / "R/W" 可读写</summary>
    public string ReadWrite        { get; set; } = "R";
    public string Unit             { get; set; } = string.Empty;
    /// <summary>CLR 解析类型：uint16 / int16 / uint32 / int32 / float</summary>
    public string DataType         { get; set; } = "uint16";
    /// <summary>寄存器存储类型（同上，部分协议有区别）</summary>
    public string RegisterDataType { get; set; } = "uint16";
    public double ScaleFactor      { get; set; } = 1.0;
    public double Offset           { get; set; } = 0.0;
    public string ValueRange       { get; set; } = string.Empty;
    public string Description      { get; set; } = string.Empty;
    /// <summary>0=遥测 1=遥控</summary>
    public int    Category         { get; set; } = 0;
    public int    SortOrder        { get; set; } = 0;

    public List<MasterStatusMapping> StatusMappings { get; set; } = new();

    public bool IsVerified { get; set; } = false;
}
