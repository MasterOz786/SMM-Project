using Microsoft.Data.SqlClient;
using SMM.Core;

namespace SMM.Data.Repositories;

public sealed class SocietyRepository
{
    public IReadOnlyList<SocietyRow> ListApprovedSocieties()
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT SocietyId, Name, Description, HeadUserId, Status
            FROM dbo.Society
            WHERE Status = @approved
            ORDER BY Name
            """;
        cmd.Parameters.AddWithValue("@approved", (byte)SocietyStatus.Approved);
        return ReadSocieties(cmd);
    }

    public IReadOnlyList<SocietyRow> ListSocietiesForHead(int headUserId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT SocietyId, Name, Description, HeadUserId, Status
            FROM dbo.Society
            WHERE HeadUserId = @h
            ORDER BY Name
            """;
        cmd.Parameters.AddWithValue("@h", headUserId);
        return ReadSocieties(cmd);
    }

    public IReadOnlyList<SocietyRow> ListAllSocietiesForAdmin()
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT SocietyId, Name, Description, HeadUserId, Status
            FROM dbo.Society
            ORDER BY Name
            """;
        return ReadSocieties(cmd);
    }

    private static List<SocietyRow> ReadSocieties(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader();
        var list = new List<SocietyRow>();
        while (r.Read())
        {
            list.Add(new SocietyRow(
                r.GetInt32(0),
                r.GetString(1),
                r.IsDBNull(2) ? null : r.GetString(2),
                r.GetInt32(3),
                (SocietyStatus)r.GetByte(4)));
        }

        return list;
    }

    public int CreateSociety(int headUserId, string name, string? description)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            int societyId;
            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO dbo.Society (Name, Description, HeadUserId, Status)
                    VALUES (@n, @d, @h, @pending);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                    """;
                cmd.Parameters.AddWithValue("@n", name.Trim());
                cmd.Parameters.AddWithValue("@d", (object?)description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@h", headUserId);
                cmd.Parameters.AddWithValue("@pending", (byte)SocietyStatus.Pending);
                societyId = (int)(cmd.ExecuteScalar() ?? throw new InvalidOperationException("Insert failed."));
            }

            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO dbo.SocietyMembership (SocietyId, UserId, IsHead, Status)
                    VALUES (@s, @u, 1, 1)
                    """;
                cmd.Parameters.AddWithValue("@s", societyId);
                cmd.Parameters.AddWithValue("@u", headUserId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            return societyId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void UpdateSocietyProfile(int societyId, int actingHeadUserId, string name, string? description)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.Society
            SET Name = @n, Description = @d
            WHERE SocietyId = @id AND HeadUserId = @h
            """;
        cmd.Parameters.AddWithValue("@n", name.Trim());
        cmd.Parameters.AddWithValue("@d", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", societyId);
        cmd.Parameters.AddWithValue("@h", actingHeadUserId);
        if (cmd.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Society not found or not authorized.");
    }

    public void SetSocietyStatus(int societyId, SocietyStatus status, int? adminUserId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.Society
            SET Status = @st,
                ApprovedByUserId = CASE WHEN @st = @approved THEN @admin ELSE ApprovedByUserId END,
                ApprovedAt = CASE WHEN @st = @approved THEN SYSUTCDATETIME() ELSE ApprovedAt END
            WHERE SocietyId = @id
            """;
        cmd.Parameters.AddWithValue("@st", (byte)status);
        cmd.Parameters.AddWithValue("@approved", (byte)SocietyStatus.Approved);
        cmd.Parameters.AddWithValue("@admin", (object?)adminUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", societyId);
        cmd.ExecuteNonQuery();
    }

    public void CreateMembershipRequest(int studentUserId, int societyId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using (var check = cn.CreateCommand())
        {
            check.CommandText = """
                SELECT 1 FROM dbo.SocietyMembership
                WHERE SocietyId = @s AND UserId = @u AND Status = 1
                """;
            check.Parameters.AddWithValue("@s", societyId);
            check.Parameters.AddWithValue("@u", studentUserId);
            if (check.ExecuteScalar() is not null)
                throw new InvalidOperationException("Already a member.");
        }

        using (var check = cn.CreateCommand())
        {
            check.CommandText = """
                SELECT 1 FROM dbo.MembershipRequest
                WHERE SocietyId = @s AND StudentUserId = @u AND Status = 0
                """;
            check.Parameters.AddWithValue("@s", societyId);
            check.Parameters.AddWithValue("@u", studentUserId);
            if (check.ExecuteScalar() is not null)
                throw new InvalidOperationException("A pending request already exists.");
        }

        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO dbo.MembershipRequest (SocietyId, StudentUserId, Status)
            VALUES (@s, @u, 0)
            """;
        cmd.Parameters.AddWithValue("@s", societyId);
        cmd.Parameters.AddWithValue("@u", studentUserId);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<MembershipStatusRow> ListMembershipOverviewForStudent(int studentUserId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT s.SocietyId, s.Name, s.Status,
                   CASE WHEN m.UserId IS NULL THEN 0 ELSE 1 END AS IsMember,
                   lr.Status AS LastRequestStatus
            FROM dbo.Society s
            INNER JOIN (
                SELECT SocietyId FROM dbo.SocietyMembership WHERE UserId = @u
                UNION
                SELECT SocietyId FROM dbo.MembershipRequest WHERE StudentUserId = @u
            ) x ON x.SocietyId = s.SocietyId
            LEFT JOIN dbo.SocietyMembership m ON m.SocietyId = s.SocietyId AND m.UserId = @u AND m.Status = 1
            OUTER APPLY (
                SELECT TOP 1 Status
                FROM dbo.MembershipRequest mr
                WHERE mr.SocietyId = s.SocietyId AND mr.StudentUserId = @u
                ORDER BY mr.RequestedAt DESC
            ) lr
            ORDER BY s.Name
            """;
        cmd.Parameters.AddWithValue("@u", studentUserId);
        using var r = cmd.ExecuteReader();
        var list = new List<MembershipStatusRow>();
        while (r.Read())
        {
            string? req = null;
            if (!r.IsDBNull(4))
            {
                req = ((MembershipRequestStatus)r.GetByte(4)) switch
                {
                    MembershipRequestStatus.Pending => "Pending",
                    MembershipRequestStatus.Approved => "Request approved",
                    MembershipRequestStatus.Rejected => "Rejected",
                    _ => null
                };
            }

            list.Add(new MembershipStatusRow(
                r.GetInt32(0),
                r.GetString(1),
                (SocietyStatus)r.GetByte(2),
                r.GetInt32(3) == 1,
                req));
        }

        return list;
    }

    public IReadOnlyList<MembershipRequestRow> ListPendingRequestsForSociety(int societyId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT mr.RequestId, mr.SocietyId, s.Name, mr.StudentUserId,
                   u.FullName, u.Email, mr.Status, mr.RequestedAt
            FROM dbo.MembershipRequest mr
            INNER JOIN dbo.Society s ON s.SocietyId = mr.SocietyId
            INNER JOIN dbo.[User] u ON u.UserId = mr.StudentUserId
            WHERE mr.SocietyId = @s AND mr.Status = 0
            ORDER BY mr.RequestedAt
            """;
        cmd.Parameters.AddWithValue("@s", societyId);
        using var r = cmd.ExecuteReader();
        var list = new List<MembershipRequestRow>();
        while (r.Read())
        {
            list.Add(new MembershipRequestRow(
                r.GetInt32(0),
                r.GetInt32(1),
                r.GetString(2),
                r.GetInt32(3),
                r.GetString(4),
                r.GetString(5),
                (MembershipRequestStatus)r.GetByte(6),
                SqlTime.Utc(r.GetDateTime(7))));
        }

        return list;
    }

    public void ApproveMembershipRequest(int requestId, int responderUserId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            int societyId;
            int studentUserId;
            using (var sel = cn.CreateCommand())
            {
                sel.Transaction = tx;
                sel.CommandText = """
                    SELECT SocietyId, StudentUserId FROM dbo.MembershipRequest
                    WHERE RequestId = @id AND Status = 0
                    """;
                sel.Parameters.AddWithValue("@id", requestId);
                using var r = sel.ExecuteReader();
                if (!r.Read())
                    throw new InvalidOperationException("Request not pending.");
                societyId = r.GetInt32(0);
                studentUserId = r.GetInt32(1);
            }

            using (var up = cn.CreateCommand())
            {
                up.Transaction = tx;
                up.CommandText = """
                    UPDATE dbo.MembershipRequest
                    SET Status = 1, RespondedAt = SYSUTCDATETIME(), RespondedByUserId = @r
                    WHERE RequestId = @id
                    """;
                up.Parameters.AddWithValue("@r", responderUserId);
                up.Parameters.AddWithValue("@id", requestId);
                up.ExecuteNonQuery();
            }

            using (var ins = cn.CreateCommand())
            {
                ins.Transaction = tx;
                ins.CommandText = """
                    IF NOT EXISTS (SELECT 1 FROM dbo.SocietyMembership WHERE SocietyId = @s AND UserId = @u)
                        INSERT INTO dbo.SocietyMembership (SocietyId, UserId, IsHead, Status)
                        VALUES (@s, @u, 0, 1)
                    ELSE
                        UPDATE dbo.SocietyMembership SET Status = 1 WHERE SocietyId = @s AND UserId = @u
                    """;
                ins.Parameters.AddWithValue("@s", societyId);
                ins.Parameters.AddWithValue("@u", studentUserId);
                ins.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void RejectMembershipRequest(int requestId, int responderUserId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.MembershipRequest
            SET Status = 2, RespondedAt = SYSUTCDATETIME(), RespondedByUserId = @r
            WHERE RequestId = @id AND Status = 0
            """;
        cmd.Parameters.AddWithValue("@r", responderUserId);
        cmd.Parameters.AddWithValue("@id", requestId);
        if (cmd.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Request not pending.");
    }

    public IReadOnlyList<MemberRow> ListMembers(int societyId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT m.UserId, u.FullName, u.Email, m.IsHead, m.JoinedAt, m.Status
            FROM dbo.SocietyMembership m
            INNER JOIN dbo.[User] u ON u.UserId = m.UserId
            WHERE m.SocietyId = @s
            ORDER BY m.IsHead DESC, u.FullName
            """;
        cmd.Parameters.AddWithValue("@s", societyId);
        using var r = cmd.ExecuteReader();
        var list = new List<MemberRow>();
        while (r.Read())
        {
            list.Add(new MemberRow(
                r.GetInt32(0),
                r.GetString(1),
                r.GetString(2),
                r.GetBoolean(3),
                SqlTime.Utc(r.GetDateTime(4)),
                r.GetByte(5) == 1));
        }

        return list;
    }

    public void SetMemberActive(int societyId, int headUserId, int memberUserId, bool active)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using (var auth = cn.CreateCommand())
        {
            auth.CommandText = "SELECT 1 FROM dbo.Society WHERE SocietyId = @s AND HeadUserId = @h";
            auth.Parameters.AddWithValue("@s", societyId);
            auth.Parameters.AddWithValue("@h", headUserId);
            if (auth.ExecuteScalar() is null)
                throw new InvalidOperationException("Not authorized.");
        }

        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.SocietyMembership
            SET Status = @st
            WHERE SocietyId = @s AND UserId = @u AND IsHead = 0
            """;
        cmd.Parameters.AddWithValue("@st", active ? (byte)1 : (byte)0);
        cmd.Parameters.AddWithValue("@s", societyId);
        cmd.Parameters.AddWithValue("@u", memberUserId);
        if (cmd.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Member not found or cannot change head.");
    }

    public (int Members, int Events, int PendingRequests) GetSocietyReport(int societyId)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT
              (SELECT COUNT(*) FROM dbo.SocietyMembership WHERE SocietyId = @s AND Status = 1),
              (SELECT COUNT(*) FROM dbo.[Event] WHERE SocietyId = @s),
              (SELECT COUNT(*) FROM dbo.MembershipRequest WHERE SocietyId = @s AND Status = 0)
            """;
        cmd.Parameters.AddWithValue("@s", societyId);
        using var r = cmd.ExecuteReader();
        r.Read();
        return (r.GetInt32(0), r.GetInt32(1), r.GetInt32(2));
    }

    public (int Students, int Societies, int Events, int OpenTasks) GetUniversityReport()
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT
              (SELECT COUNT(*) FROM dbo.[User] WHERE Role = 1 AND IsActive = 1),
              (SELECT COUNT(*) FROM dbo.Society WHERE Status = 1),
              (SELECT COUNT(*) FROM dbo.[Event] WHERE AdminStatus = 1 AND EventStatus = 1),
              (SELECT COUNT(*) FROM dbo.SocietyTask WHERE Status IN (0, 1))
            """;
        using var r = cmd.ExecuteReader();
        r.Read();
        return (r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3));
    }
}
