using Microsoft.Data.SqlClient;
using SMM.Core;
using SMM.Core.Security;

namespace SMM.Data.Repositories;

public sealed class UserRepository
{
    public AuthUser? TryLogin(string email, string password)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT UserId, Email, FullName, Role, PasswordHash, IsActive
            FROM dbo.[User]
            WHERE Email = @e
            """;
        cmd.Parameters.AddWithValue("@e", email.Trim());
        using var r = cmd.ExecuteReader();
        if (!r.Read())
            return null;

        var userId = r.GetInt32(0);
        var em = r.GetString(1);
        var fullName = r.GetString(2);
        var role = (UserRole)r.GetByte(3);
        var hash = r.GetString(4);
        var active = r.GetBoolean(5);
        r.Close();

        if (!active || !PasswordHasher.Verify(password, hash))
            return null;

        using (var up = cn.CreateCommand())
        {
            up.CommandText = "UPDATE dbo.[User] SET LastLoginAt = SYSUTCDATETIME() WHERE UserId = @id";
            up.Parameters.AddWithValue("@id", userId);
            up.ExecuteNonQuery();
        }

        return new AuthUser(userId, em, fullName, role, active);
    }

    public void RegisterStudent(string fullName, string email, string password, string? studentNumber, string? program, byte? yearOfStudy)
    {
        var hash = PasswordHasher.Hash(password);
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            int userId;
            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO dbo.[User] (Email, PasswordHash, FullName, Role, IsActive)
                    VALUES (@e, @p, @f, @r, 1);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                    """;
                cmd.Parameters.AddWithValue("@e", email.Trim());
                cmd.Parameters.AddWithValue("@p", hash);
                cmd.Parameters.AddWithValue("@f", fullName.Trim());
                cmd.Parameters.AddWithValue("@r", (byte)UserRole.Student);
                userId = (int)(cmd.ExecuteScalar() ?? throw new InvalidOperationException("Insert failed."));
            }

            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO dbo.StudentProfile (UserId, StudentNumber, Program, YearOfStudy)
                    VALUES (@id, @sn, @prog, @yr)
                    """;
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.Parameters.AddWithValue("@sn", (object?)studentNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@prog", (object?)program ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@yr", (object?)yearOfStudy ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public IReadOnlyList<UserAdminRow> ListUsers()
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT UserId, Email, FullName, Role, IsActive, CreatedAt
            FROM dbo.[User]
            ORDER BY CreatedAt DESC
            """;
        using var r = cmd.ExecuteReader();
        var list = new List<UserAdminRow>();
        while (r.Read())
        {
            list.Add(new UserAdminRow(
                r.GetInt32(0),
                r.GetString(1),
                r.GetString(2),
                (UserRole)r.GetByte(3),
                r.GetBoolean(4),
                SqlTime.Utc(r.GetDateTime(5))));
        }

        return list;
    }

    public void SetUserActive(int userId, bool active)
    {
        using var cn = new SqlConnection(Database.ConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "UPDATE dbo.[User] SET IsActive = @a WHERE UserId = @id";
        cmd.Parameters.AddWithValue("@a", active);
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.ExecuteNonQuery();
    }
}
