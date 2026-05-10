using Microsoft.Data.SqlClient;
using SMM.Core;

namespace SMM.Data.Repositories;

public sealed class TaskRepository
{
    public int CreateTask(int societyId, int assignedByUserId, int assignedToUserId, string title, string? description, DateTimeOffset? dueDate)
    {
        EnsureHead(societyId, assignedByUserId);
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO dbo.SocietyTask (SocietyId, Title, Description, AssignedToUserId, AssignedByUserId, DueDate, Status)
            VALUES (@s, @t, @d, @to, @by, @due, 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        cmd.Parameters.AddWithValue("@s", societyId);
        cmd.Parameters.AddWithValue("@t", title.Trim());
        cmd.Parameters.AddWithValue("@d", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@to", assignedToUserId);
        cmd.Parameters.AddWithValue("@by", assignedByUserId);
        cmd.Parameters.AddWithValue("@due", (object?)dueDate?.UtcDateTime ?? DBNull.Value);
        return (int)(cmd.ExecuteScalar() ?? throw new InvalidOperationException("Insert failed."));
    }

    public IReadOnlyList<TaskRow> ListForSociety(int societyId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT t.TaskId, t.SocietyId, t.Title, t.Description, t.AssignedToUserId,
                   u.FullName, t.DueDate, t.Status
            FROM dbo.SocietyTask t
            INNER JOIN dbo.[User] u ON u.UserId = t.AssignedToUserId
            WHERE t.SocietyId = @s
            ORDER BY t.CreatedAt DESC
            """;
        cmd.Parameters.AddWithValue("@s", societyId);
        using var r = cmd.ExecuteReader();
        var list = new List<TaskRow>();
        while (r.Read())
        {
            list.Add(new TaskRow(
                r.GetInt32(0),
                r.GetInt32(1),
                r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3),
                r.GetInt32(4),
                r.GetString(5),
                r.IsDBNull(6) ? null : SqlTime.Utc(r.GetDateTime(6)),
                (SocietyTaskStatus)r.GetByte(7)));
        }

        return list;
    }

    public void SetStatus(int taskId, int societyId, int headUserId, SocietyTaskStatus status)
    {
        EnsureHead(societyId, headUserId);
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.SocietyTask SET Status = @st
            WHERE TaskId = @id AND SocietyId = @s
            """;
        cmd.Parameters.AddWithValue("@st", (byte)status);
        cmd.Parameters.AddWithValue("@id", taskId);
        cmd.Parameters.AddWithValue("@s", societyId);
        if (cmd.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Task not found.");
    }

    private static void EnsureHead(int societyId, int headUserId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM dbo.Society WHERE SocietyId = @s AND HeadUserId = @h";
        cmd.Parameters.AddWithValue("@s", societyId);
        cmd.Parameters.AddWithValue("@h", headUserId);
        if (cmd.ExecuteScalar() is null)
            throw new InvalidOperationException("Not authorized.");
    }
}
