using SMM.Core;

namespace SMM.Desktop.Workspace;

public sealed record StudentDashboard(
    IReadOnlyList<SocietyRow> ApprovedSocieties,
    IReadOnlyList<MembershipStatusRow> MembershipOverview,
    IReadOnlyList<EventRow> PublishedEvents,
    IReadOnlyList<TicketRow> Tickets);
