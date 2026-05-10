using SMM.Core;

namespace SMM.Desktop.Workspace;

public sealed record AdminDashboard(
    IReadOnlyList<UserAdminRow> Users,
    IReadOnlyList<SocietyRow> Societies,
    IReadOnlyList<EventRow> PendingEvents,
    IReadOnlyList<ActivityRow> Activity);
