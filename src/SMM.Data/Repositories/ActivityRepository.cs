using Microsoft.Data.SqlClient;
using SMM.Core;

namespace SMM.Data.Repositories;

public sealed class ActivityRepository
{
    public void Log(int? userId, string actionType, string? entityType, int? entityId, string? details)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO dbo.ActivityLog (UserId, ActionType, EntityType, EntityId, Details)
            VALUES (@u, @a, @et, @eid, @d)
            """;
        cmd.Parameters.AddWithValue("@u", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@a", actionType);
        cmd.Parameters.AddWithValue("@et", (object?)entityType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@eid", (object?)entityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@d", (object?)details ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<ActivityRow> ListRecent(int take = 200)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT TOP (@t) LogId, UserId, ActionType, EntityType, EntityId, Details, CreatedAt
            FROM dbo.ActivityLog
            ORDER BY CreatedAt DESC
            """;
        cmd.Parameters.AddWithValue("@t", take);
        using var r = cmd.ExecuteReader();
        var list = new List<ActivityRow>();
        while (r.Read())
        {
            list.Add(new ActivityRow(
                r.GetInt64(0),
                r.IsDBNull(1) ? null : r.GetInt32(1),
                r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3),
                r.IsDBNull(4) ? null : r.GetInt32(4),
                r.IsDBNull(5) ? null : r.GetString(5),
                SqlTime.Utc(r.GetDateTime(6))));
        }

        return list;
    }
}
