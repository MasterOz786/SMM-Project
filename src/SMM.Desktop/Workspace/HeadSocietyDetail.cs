using SMM.Core;

namespace SMM.Desktop.Workspace;

public sealed record HeadSocietyDetail(
    SocietyRow? Summary,
    IReadOnlyList<MembershipRequestRow> PendingRequests,
    IReadOnlyList<MemberRow> Members,
    IReadOnlyList<EventRow> Events,
    IReadOnlyList<TaskRow> Tasks,
    IReadOnlyList<MemberRow> TaskAssigneeOptions);
