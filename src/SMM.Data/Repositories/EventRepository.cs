using Microsoft.Data.SqlClient;
using SMM.Core;

namespace SMM.Data.Repositories;

public sealed class EventRepository
{
    public IReadOnlyList<EventRow> ListPublishedForStudents()
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT e.EventId, e.SocietyId, s.Name, e.Title, e.Description, e.Venue, e.StartsAt, e.EndsAt,
                   e.Capacity, e.AdminStatus, e.EventStatus
            FROM dbo.[Event] e
            INNER JOIN dbo.Society s ON s.SocietyId = e.SocietyId
            WHERE e.AdminStatus = @a AND e.EventStatus = @pub AND e.StartsAt > SYSUTCDATETIME()
            ORDER BY e.StartsAt
            """;
        cmd.Parameters.AddWithValue("@a", (byte)EventAdminStatus.Approved);
        cmd.Parameters.AddWithValue("@pub", (byte)EventLifecycleStatus.Published);
        return ReadEvents(cmd);
    }

    public IReadOnlyList<EventRow> ListForSociety(int societyId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT e.EventId, e.SocietyId, s.Name, e.Title, e.Description, e.Venue, e.StartsAt, e.EndsAt,
                   e.Capacity, e.AdminStatus, e.EventStatus
            FROM dbo.[Event] e
            INNER JOIN dbo.Society s ON s.SocietyId = e.SocietyId
            WHERE e.SocietyId = @s
            ORDER BY e.StartsAt DESC
            """;
        cmd.Parameters.AddWithValue("@s", societyId);
        return ReadEvents(cmd);
    }

    public IReadOnlyList<EventRow> ListPendingAdminApproval()
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT e.EventId, e.SocietyId, s.Name, e.Title, e.Description, e.Venue, e.StartsAt, e.EndsAt,
                   e.Capacity, e.AdminStatus, e.EventStatus
            FROM dbo.[Event] e
            INNER JOIN dbo.Society s ON s.SocietyId = e.SocietyId
            WHERE e.AdminStatus = @p
            ORDER BY e.CreatedAt
            """;
        cmd.Parameters.AddWithValue("@p", (byte)EventAdminStatus.Pending);
        return ReadEvents(cmd);
    }

    private static List<EventRow> ReadEvents(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader();
        var list = new List<EventRow>();
        while (r.Read())
        {
            list.Add(new EventRow(
                r.GetInt32(0),
                r.GetInt32(1),
                r.GetString(2),
                r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5),
                SqlTime.Utc(r.GetDateTime(6)),
                r.IsDBNull(7) ? null : SqlTime.Utc(r.GetDateTime(7)),
                r.IsDBNull(8) ? null : r.GetInt32(8),
                (EventAdminStatus)r.GetByte(9),
                (EventLifecycleStatus)r.GetByte(10)));
        }

        return list;
    }

    public int CreateEvent(int societyId, int createdByUserId, string title, string? description, string? venue, DateTimeOffset startsAt, DateTimeOffset? endsAt, int? capacity)
    {
        EnsureSocietyApproved(societyId);
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO dbo.[Event] (SocietyId, Title, Description, Venue, StartsAt, EndsAt, Capacity, AdminStatus, EventStatus, CreatedByUserId)
            VALUES (@s, @t, @d, @v, @st, @en, @c, @ap, @ev, @u);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        cmd.Parameters.AddWithValue("@s", societyId);
        cmd.Parameters.AddWithValue("@t", title.Trim());
        cmd.Parameters.AddWithValue("@d", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@v", (object?)venue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@st", startsAt.UtcDateTime);
        cmd.Parameters.AddWithValue("@en", (object?)endsAt?.UtcDateTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@c", (object?)capacity ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ap", (byte)EventAdminStatus.Pending);
        cmd.Parameters.AddWithValue("@ev", (byte)EventLifecycleStatus.Draft);
        cmd.Parameters.AddWithValue("@u", createdByUserId);
        return (int)(cmd.ExecuteScalar() ?? throw new InvalidOperationException("Insert failed."));
    }

    public void UpdateEvent(int eventId, int societyId, int headUserId, string title, string? description, string? venue, DateTimeOffset startsAt, DateTimeOffset? endsAt, int? capacity, EventLifecycleStatus lifecycle)
    {
        EnsureHead(societyId, headUserId);
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.[Event]
            SET Title = @t, Description = @d, Venue = @v, StartsAt = @st, EndsAt = @en, Capacity = @c, EventStatus = @ev
            WHERE EventId = @id AND SocietyId = @s
            """;
        cmd.Parameters.AddWithValue("@t", title.Trim());
        cmd.Parameters.AddWithValue("@d", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@v", (object?)venue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@st", startsAt.UtcDateTime);
        cmd.Parameters.AddWithValue("@en", (object?)endsAt?.UtcDateTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@c", (object?)capacity ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ev", (byte)lifecycle);
        cmd.Parameters.AddWithValue("@id", eventId);
        cmd.Parameters.AddWithValue("@s", societyId);
        if (cmd.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Event not found.");
    }

    public void SetAdminStatus(int eventId, EventAdminStatus status, int adminUserId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.[Event]
            SET AdminStatus = @st,
                ApprovedByUserId = CASE WHEN @st = @ap THEN @admin ELSE ApprovedByUserId END,
                ApprovedAt = CASE WHEN @st = @ap THEN SYSUTCDATETIME() ELSE ApprovedAt END
            WHERE EventId = @id
            """;
        cmd.Parameters.AddWithValue("@st", (byte)status);
        cmd.Parameters.AddWithValue("@ap", (byte)EventAdminStatus.Approved);
        cmd.Parameters.AddWithValue("@admin", adminUserId);
        cmd.Parameters.AddWithValue("@id", eventId);
        cmd.ExecuteNonQuery();
    }

    public string RegisterStudent(int eventId, int userId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            byte adminStatus;
            byte eventStatus;
            DateTime startsAt;
            int? capacity;
            using (var sel = cn.CreateCommand())
            {
                sel.Transaction = tx;
                sel.CommandText = """
                    SELECT AdminStatus, EventStatus, StartsAt, Capacity
                    FROM dbo.[Event] WITH (UPDLOCK, HOLDLOCK)
                    WHERE EventId = @id
                    """;
                sel.Parameters.AddWithValue("@id", eventId);
                using var r = sel.ExecuteReader();
                if (!r.Read())
                    throw new InvalidOperationException("Event not found.");
                adminStatus = r.GetByte(0);
                eventStatus = r.GetByte(1);
                startsAt = r.GetDateTime(2);
                capacity = r.IsDBNull(3) ? null : r.GetInt32(3);
            }

            if (adminStatus != (byte)EventAdminStatus.Approved || eventStatus != (byte)EventLifecycleStatus.Published)
                throw new InvalidOperationException("Event is not open for registration.");
            if (startsAt <= DateTime.UtcNow)
                throw new InvalidOperationException("Event has already started.");

            if (capacity is int cap)
            {
                using var cnt = cn.CreateCommand();
                cnt.Transaction = tx;
                cnt.CommandText = "SELECT COUNT(*) FROM dbo.EventRegistration WHERE EventId = @id";
                cnt.Parameters.AddWithValue("@id", eventId);
                var n = (int)cnt.ExecuteScalar()!;
                if (n >= cap)
                    throw new InvalidOperationException("Event is full.");
            }

            var ticket = TicketCode.New();
            using (var ins = cn.CreateCommand())
            {
                ins.Transaction = tx;
                ins.CommandText = """
                    INSERT INTO dbo.EventRegistration (EventId, UserId, TicketCode)
                    VALUES (@e, @u, @tc)
                    """;
                ins.Parameters.AddWithValue("@e", eventId);
                ins.Parameters.AddWithValue("@u", userId);
                ins.Parameters.AddWithValue("@tc", ticket);
                ins.ExecuteNonQuery();
            }

            tx.Commit();
            return ticket;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public IReadOnlyList<TicketRow> ListTicketsForStudent(int userId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT er.RegistrationId, e.EventId, e.Title, s.Name, e.StartsAt, er.TicketCode, er.RegisteredAt
            FROM dbo.EventRegistration er
            INNER JOIN dbo.[Event] e ON e.EventId = er.EventId
            INNER JOIN dbo.Society s ON s.SocietyId = e.SocietyId
            WHERE er.UserId = @u
            ORDER BY e.StartsAt DESC
            """;
        cmd.Parameters.AddWithValue("@u", userId);
        using var r = cmd.ExecuteReader();
        var list = new List<TicketRow>();
        while (r.Read())
        {
            list.Add(new TicketRow(
                r.GetInt32(0),
                r.GetInt32(1),
                r.GetString(2),
                r.GetString(3),
                SqlTime.Utc(r.GetDateTime(4)),
                r.GetString(5),
                SqlTime.Utc(r.GetDateTime(6))));
        }

        return list;
    }

    private static void EnsureSocietyApproved(int societyId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Status FROM dbo.Society WHERE SocietyId = @s";
        cmd.Parameters.AddWithValue("@s", societyId);
        var o = cmd.ExecuteScalar();
        if (o is null || (byte)o != (byte)SocietyStatus.Approved)
            throw new InvalidOperationException("Society must be approved to manage events.");
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
