namespace SMM.Data;

internal static class SqlTime
{
    internal static DateTimeOffset Utc(DateTime sqlValue) =>
        new(DateTime.SpecifyKind(sqlValue, DateTimeKind.Utc));

    internal static DateTimeOffset? UtcNullable(DateTime? sqlValue) =>
        sqlValue is { } v ? Utc(v) : null;
}
