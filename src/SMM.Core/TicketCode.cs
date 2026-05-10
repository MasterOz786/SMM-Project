namespace SMM.Core;

public static class TicketCode
{
    public static string New() => Guid.NewGuid().ToString("N");
}
