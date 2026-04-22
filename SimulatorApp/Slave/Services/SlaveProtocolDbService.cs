using Microsoft.Data.SqlClient;
using SimulatorApp.Shared.Logging;

namespace SimulatorApp.Slave.Services;

public interface ISlaveProtocolDbService
{
    Task InitializeAsync();
    Task UpsertDeviceAsync(string deviceName,
        IEnumerable<(string ChineseName, string EnglishName, int Address, string ReadWrite, string Range, string Unit, string Note)> rows);
    Task<List<(string DeviceName, List<(string ChineseName, string EnglishName, int Address, string ReadWrite, string Range, string Unit, string Note)> Rows)>>
        GetAllDevicesAsync();
}

/// <summary>
/// 从站协议导入数据持久化到 SQL Server。
/// 表：SlaveProtocolRows（DeviceName, Address, ChineseName, EnglishName, ReadWrite, Unit, Note）
/// </summary>
public class SlaveProtocolDbService : ISlaveProtocolDbService
{
    private readonly string _cs;
    public SlaveProtocolDbService(string connectionString) => _cs = connectionString;

    public async Task InitializeAsync()
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync();
        const string ddl = """
            IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name='SlaveProtocolRows' AND type='U')
            CREATE TABLE SlaveProtocolRows (
                Id          INT IDENTITY(1,1) PRIMARY KEY,
                DeviceName  NVARCHAR(200) NOT NULL,
                SortOrder   INT           NOT NULL DEFAULT 0,
                Address     INT           NOT NULL,
                ChineseName NVARCHAR(200) NOT NULL DEFAULT '',
                EnglishName NVARCHAR(200) NOT NULL DEFAULT '',
                ReadWrite   NVARCHAR(20)  NOT NULL DEFAULT '',
                Range       NVARCHAR(200) NOT NULL DEFAULT '',
                Unit        NVARCHAR(50)  NOT NULL DEFAULT '',
                Note        NVARCHAR(500) NOT NULL DEFAULT '',
                CreatedAt   DATETIME2     NOT NULL DEFAULT GETDATE()
            );
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('SlaveProtocolRows') AND name='Range')
                ALTER TABLE SlaveProtocolRows ADD [Range] NVARCHAR(200) NOT NULL DEFAULT '';
            """;
        await using var cmd = new SqlCommand(ddl, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>覆盖保存：先删除该设备名下所有旧行，再插入新行</summary>
    public async Task UpsertDeviceAsync(string deviceName,
        IEnumerable<(string ChineseName, string EnglishName, int Address, string ReadWrite, string Range, string Unit, string Note)> rows)
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            const string del = "DELETE FROM SlaveProtocolRows WHERE DeviceName = @dn";
            await using (var cmd = new SqlCommand(del, conn, tx))
            {
                cmd.Parameters.AddWithValue("@dn", deviceName);
                await cmd.ExecuteNonQueryAsync();
            }

            int order = 0;
            const string ins = """
                INSERT INTO SlaveProtocolRows
                    (DeviceName, SortOrder, Address, ChineseName, EnglishName, ReadWrite, Range, Unit, Note)
                VALUES (@dn, @so, @addr, @cn, @en, @rw, @range, @unit, @note)
                """;
            foreach (var (cn, en, addr, rw, range, unit, note) in rows)
            {
                await using var cmd = new SqlCommand(ins, conn, tx);
                cmd.Parameters.AddWithValue("@dn",   deviceName);
                cmd.Parameters.AddWithValue("@so",   order++);
                cmd.Parameters.AddWithValue("@addr", addr);
                cmd.Parameters.AddWithValue("@cn",   cn);
                cmd.Parameters.AddWithValue("@en",   en);
                cmd.Parameters.AddWithValue("@rw",   rw);
                cmd.Parameters.AddWithValue("@range", range);
                cmd.Parameters.AddWithValue("@unit", unit);
                cmd.Parameters.AddWithValue("@note", note);
                await cmd.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<(string DeviceName, List<(string ChineseName, string EnglishName, int Address, string ReadWrite, string Range, string Unit, string Note)> Rows)>>
        GetAllDevicesAsync()
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync();
        const string sql = """
            SELECT DeviceName, Address, ChineseName, EnglishName, ReadWrite, Range, Unit, Note
            FROM SlaveProtocolRows
            ORDER BY DeviceName, SortOrder
            """;
        await using var cmd = new SqlCommand(sql, conn);
        await using var rdr = await cmd.ExecuteReaderAsync();

        var dict = new Dictionary<string, List<(string, string, int, string, string, string, string)>>(StringComparer.Ordinal);
        var order = new List<string>();
        while (await rdr.ReadAsync())
        {
            var dn  = rdr.GetString(0);
            var row = (rdr.GetString(2), rdr.GetString(3), rdr.GetInt32(1),
                       rdr.GetString(4), rdr.GetString(5), rdr.GetString(6), rdr.GetString(7));
            if (!dict.ContainsKey(dn)) { dict[dn] = new(); order.Add(dn); }
            dict[dn].Add(row);
        }
        return order.Select(dn => (dn, dict[dn])).ToList();
    }
}
