using ClosedXML.Excel;
using SimulatorApp.Slave.Models;
using SimulatorApp.Slave.Services;
using SimulatorApp.Slave.ViewModels;

namespace SimulatorApp.Shared.Helpers;

/// <summary>
/// Excel 导入/导出工具类（使用 ClosedXML，MIT License）。
/// 支持：
///   1. 导出设备数据为 Excel（用于人工查看/分析）
///   2. 导入设备数据 Excel（恢复寄存器值）
///   3. 生成导入模板（供用户填写后导入）
/// </summary>
public static class ExcelHelper
{
    // ----------------------------------------------------------------
    // 导出：设备数据 → Excel
    // ----------------------------------------------------------------

    /// <summary>
    /// 将设备寄存器数据导出为 Excel 文件。
    /// 每行包含：地址(Dec)、地址(Hex)、值(Dec)、值(Hex)、备注
    /// </summary>
    /// <param name="filePath">目标文件路径（.xlsx）</param>
    /// <param name="deviceName">设备名称（用作 Sheet 名）</param>
    /// <param name="rows">行数据列表，每项为 (地址, 值, 备注)</param>
    public static void ExportDeviceData(string filePath, string deviceName,
        IEnumerable<(int Address, ushort Value, string Remark)> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(deviceName.Length > 31 ? deviceName[..31] : deviceName);

        // 表头
        ws.Cell(1, 1).Value = "地址(Dec)";
        ws.Cell(1, 2).Value = "地址(Hex)";
        ws.Cell(1, 3).Value = "值(Dec)";
        ws.Cell(1, 4).Value = "值(Hex)";
        ws.Cell(1, 5).Value = "备注";

        // 表头样式
        var headerRange = ws.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
        headerRange.Style.Font.FontColor = XLColor.White;

        // 数据行
        int row = 2;
        foreach (var (addr, val, remark) in rows)
        {
            ws.Cell(row, 1).Value = addr;
            ws.Cell(row, 2).Value = $"0x{addr:X4}";
            ws.Cell(row, 3).Value = val;
            ws.Cell(row, 4).Value = $"0x{val:X4}";
            ws.Cell(row, 5).Value = remark;
            row++;
        }

        // 自动列宽
        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    /// <summary>
    /// 导出带字段说明的快照（包含字段名、物理值、单位，便于人工阅读）
    /// </summary>
    /// <param name="filePath">目标文件路径（.xlsx）</param>
    /// <param name="sheets">多设备数据，key=设备名，value=字段列表</param>
    public static void ExportSnapshot(string filePath,
        Dictionary<string, IEnumerable<(string FieldName, string ChineseName, double PhysicalValue, string Unit, string Remark)>> sheets)
    {
        using var wb = new XLWorkbook();

        foreach (var (deviceName, fields) in sheets)
        {
            var sheetName = deviceName.Length > 31 ? deviceName[..31] : deviceName;
            var ws = wb.AddWorksheet(sheetName);

            // 表头
            string[] headers = { "变量名", "中文名", "物理值", "单位", "备注" };
            for (int col = 1; col <= headers.Length; col++)
            {
                ws.Cell(1, col).Value = headers[col - 1];
            }
            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1C2033");
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var (fieldName, chineseName, physicalValue, unit, remark) in fields)
            {
                ws.Cell(row, 1).Value = fieldName;
                ws.Cell(row, 2).Value = chineseName;
                ws.Cell(row, 3).Value = physicalValue;
                ws.Cell(row, 4).Value = unit;
                ws.Cell(row, 5).Value = remark;
                row++;
            }
            ws.Columns().AdjustToContents();
        }

        wb.SaveAs(filePath);
    }

    // ----------------------------------------------------------------
    // 导入：Excel → 设备数据
    // ----------------------------------------------------------------

    /// <summary>
    /// 从 Excel 文件导入设备数据（格式须与 ExportDeviceData 导出格式一致）。
    /// 返回 (地址, 值) 列表。
    /// </summary>
    /// <param name="filePath">源文件路径（.xlsx）</param>
    /// <param name="sheetName">Sheet 名称，null 表示取第一个 Sheet</param>
    public static List<(int Address, ushort Value)> ImportDeviceData(string filePath, string? sheetName = null)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = sheetName != null ? wb.Worksheet(sheetName) : wb.Worksheet(1);

        var result = new List<(int, ushort)>();
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        // 从第 2 行开始（跳过表头）
        for (int row = 2; row <= lastRow; row++)
        {
            var addrCell = ws.Cell(row, 1);
            var valCell  = ws.Cell(row, 3);

            if (addrCell.IsEmpty()) continue;

            if (int.TryParse(addrCell.GetValue<string>(), out int addr) &&
                ushort.TryParse(valCell.GetValue<string>(), out ushort val))
            {
                result.Add((addr, val));
            }
        }
        return result;
    }

    // ----------------------------------------------------------------
    // 导出 ViewModel 数据（供从站"导出当前设备Excel"按钮使用）
    // ----------------------------------------------------------------

    /// <summary>
    /// 将设备 ViewModel 的当前寄存器值（通过 RegisterMapService 获取）导出为 Excel。
    /// </summary>
    public static void ExportDeviceViewModel(string filePath, DeviceViewModelBase vm)
    {
        // 导出设备名和类型信息
        using var wb = new XLWorkbook();
        var sheetName = vm.DeviceName.Length > 31 ? vm.DeviceName[..31] : vm.DeviceName;
        var ws = wb.AddWorksheet(sheetName);

        ws.Cell(1, 1).Value = "设备名";
        ws.Cell(1, 2).Value = vm.DeviceName;
        ws.Cell(1, 1).Style.Font.Bold = true;

        ws.Cell(2, 1).Value = "导出时间";
        ws.Cell(2, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var headerRange = ws.Range(4, 1, 4, 3);
        ws.Cell(4, 1).Value = "属性名";
        ws.Cell(4, 2).Value = "当前值";
        ws.Cell(4, 3).Value = "备注";
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
        headerRange.Style.Font.FontColor = XLColor.White;

        // 通过反射获取 ViewModel 的所有公共 double/int/string 属性
        int row = 5;
        foreach (var prop in vm.GetType().GetProperties()
            .Where(p => p.CanRead && (p.PropertyType == typeof(double)
                                   || p.PropertyType == typeof(int)
                                   || p.PropertyType == typeof(float)
                                   || p.PropertyType == typeof(ushort))))
        {
            ws.Cell(row, 1).Value = prop.Name;
            ws.Cell(row, 2).Value = prop.GetValue(vm)?.ToString() ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    /// <summary>
    /// 供主站"导出轮询数据"使用：接受多列元组列表
    /// </summary>
    public static void ExportDeviceData(string filePath,
        IEnumerable<(int Address, string Description, ushort RawValue, string HexValue, string PhysicalValue, string LastUpdated)> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("轮询数据");

        string[] headers = { "地址", "说明", "原始值", "十六进制", "物理值", "更新时间" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var (addr, desc, raw, hex, phys, updated) in rows)
        {
            ws.Cell(row, 1).Value = addr;
            ws.Cell(row, 2).Value = desc;
            ws.Cell(row, 3).Value = raw;
            ws.Cell(row, 4).Value = hex;
            ws.Cell(row, 5).Value = phys;
            ws.Cell(row, 6).Value = updated;
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    /// <summary>
    /// 从主站导出的 Excel 重新导入轮询数据（地址、描述、原始值均读取）。
    /// 格式与 ExportDeviceData(6列) 一致：地址|说明|原始值|十六进制|物理值|更新时间。
    /// </summary>
    public static List<(int Address, string Description, ushort RawValue)> ImportPollData(string filePath)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet(1);

        var result = new List<(int, string, ushort)>();
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var addrCell = ws.Cell(row, 1);
            if (addrCell.IsEmpty()) continue;
            if (!int.TryParse(addrCell.GetValue<string>(), out int addr)) continue;

            var desc   = ws.Cell(row, 2).GetValue<string>();
            ushort.TryParse(ws.Cell(row, 3).GetValue<string>(), out ushort rawVal);
            result.Add((addr, desc, rawVal));
        }
        return result;
    }

    /// <summary>
    /// 将从站导出的设备 Excel 重新导入回对应 ViewModel（按属性名反射写值）。
    /// 格式与 ExportDeviceViewModel 一致：第5行起，属性名|值。
    /// </summary>
    public static void ImportDeviceViewModel(string filePath, DeviceViewModelBase vm)
    {
        using var wb = new XLWorkbook(filePath);
        // 优先按设备名找 Sheet，找不到取第一个
        var sheetName = vm.DeviceName.Length > 31 ? vm.DeviceName[..31] : vm.DeviceName;
        IXLWorksheet ws;
        try { ws = wb.Worksheet(sheetName); }
        catch { ws = wb.Worksheet(1); }

        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 4;
        var props = vm.GetType().GetProperties()
            .Where(p => p.CanWrite && (p.PropertyType == typeof(double)
                                    || p.PropertyType == typeof(float)
                                    || p.PropertyType == typeof(int)
                                    || p.PropertyType == typeof(ushort)))
            .ToDictionary(p => p.Name);

        // 数据从第 5 行开始（行1=设备名，行2=导出时间，行3=空，行4=表头）
        for (int row = 5; row <= lastRow; row++)
        {
            var nameCell = ws.Cell(row, 1);
            var valCell  = ws.Cell(row, 2);
            if (nameCell.IsEmpty()) continue;

            var propName = nameCell.GetValue<string>();
            if (!props.TryGetValue(propName, out var prop)) continue;

            var valStr = valCell.GetValue<string>();
            try
            {
                object? value = prop.PropertyType == typeof(double)
                    ? (object)(double.TryParse(valStr, System.Globalization.NumberStyles.Any,
                                               System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0.0)
                    : prop.PropertyType == typeof(float)
                    ? (float.TryParse(valStr, System.Globalization.NumberStyles.Any,
                                      System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0f)
                    : prop.PropertyType == typeof(int)
                    ? (int.TryParse(valStr, out var i) ? i : 0)
                    : (ushort.TryParse(valStr, out var u) ? u : (ushort)0);
                if (value != null) prop.SetValue(vm, value);
            }
            catch { /* 类型不匹配时跳过 */ }
        }
        vm.FlushToRegisters();
    }

    // ----------------------------------------------------------------
    // 自定义设备导入（Excel → CustomDeviceModel → CustomDeviceViewModel）
    // ----------------------------------------------------------------

    /// <summary>
    /// 从 Excel 文件导入自定义设备配置，生成 CustomDeviceViewModel。
    /// Excel 格式：行1=表头（地址/字段名/单位/默认值），行2起为数据。
    /// </summary>
    public static CustomDeviceViewModel ImportCustomDevice(string filePath,
        Services.RegisterBank bank)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet(1);

        // 从 Sheet 名推断设备名
        var deviceName = ws.Name ?? System.IO.Path.GetFileNameWithoutExtension(filePath);
        var model = new CustomDeviceModel(deviceName);

        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (int row = 2; row <= lastRow; row++)
        {
            var addrCell  = ws.Cell(row, 1);
            if (addrCell.IsEmpty()) continue;

            if (!int.TryParse(addrCell.GetValue<string>(), out int addr)) continue;
            model.Rows.Add(new CustomRegisterDef
            {
                Address      = addr,
                FieldName    = ws.Cell(row, 2).GetValue<string>(),
                Unit         = ws.Cell(row, 3).GetValue<string>(),
                DefaultValue = ushort.TryParse(ws.Cell(row, 4).GetValue<string>(), out var dv) ? dv : (ushort)0
            });
        }

        var mapSvc = new RegisterMapService(bank);
        return new CustomDeviceViewModel(model, bank, mapSvc);
    }

    // ----------------------------------------------------------------
    // 生成模板
    // ----------------------------------------------------------------

    /// <summary>
    /// 生成设备 Excel 导入模板（含地址和字段说明，值列留空供用户填写）
    /// </summary>
    public static void GenerateTemplate(string filePath, string deviceName,
        IEnumerable<(int Address, string ChineseName, string Unit, string DefaultValue)> fields)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(deviceName);

        // 说明行
        ws.Cell(1, 1).Value = $"【{deviceName}】数据导入模板 - 请在[值(Dec)]列填入十进制数值后导入";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.Red;
        ws.Range(1, 1, 1, 5).Merge();

        // 表头
        ws.Cell(2, 1).Value = "地址(Dec)";
        ws.Cell(2, 2).Value = "中文名";
        ws.Cell(2, 3).Value = "值(Dec)";
        ws.Cell(2, 4).Value = "单位";
        ws.Cell(2, 5).Value = "参考默认值";

        var headerRange = ws.Range(2, 1, 2, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
        headerRange.Style.Font.FontColor = XLColor.White;

        // 锁定地址列（只允许修改值列）
        int row = 3;
        foreach (var (addr, chineseName, unit, defaultValue) in fields)
        {
            ws.Cell(row, 1).Value = addr;
            ws.Cell(row, 2).Value = chineseName;
            ws.Cell(row, 3).Value = string.Empty; // 用户填写
            ws.Cell(row, 4).Value = unit;
            ws.Cell(row, 5).Value = defaultValue;
            row++;
        }

        ws.Columns().AdjustToContents();
        // 高亮"值"列提示用户
        ws.Column(3).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF9C4");
        wb.SaveAs(filePath);
    }
}
