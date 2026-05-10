using SMM.Core;
using SMM.Data.Repositories;

namespace SMM.Desktop.Workspace;

public sealed class SocietyHeadWorkspace : ISocietyHeadWorkspace
{
    private readonly int _headUserId;
    private readonly SocietyRepository _society;
    private readonly EventRepository _events;
    private readonly TaskRepository _tasks;
    private readonly ActivityRepository _activity;

    public SocietyHeadWorkspace(
        int headUserId,
        SocietyRepository society,
        EventRepository events,
        TaskRepository tasks,
        ActivityRepository activity)
    {
        _headUserId = headUserId;
        _society = society;
        _events = events;
        _tasks = tasks;
        _activity = activity;
    }

    public IReadOnlyList<SocietyRow> ListMySocieties() =>
        _society.ListSocietiesForHead(_headUserId).ToList();

    public HeadSocietyDetail LoadDetail(int societyId)
    {
        var summary = _society.ListSocietiesForHead(_headUserId).FirstOrDefault(s => s.SocietyId == societyId);
        var members = _society.ListMembers(societyId).ToList();
        return new HeadSocietyDetail(
            summary,
            _society.ListPendingRequestsForSociety(societyId).ToList(),
            members,
            _events.ListForSociety(societyId).ToList(),
            _tasks.ListForSociety(societyId).ToList(),
            members.Where(m => m.Active && !m.IsHead).ToList());
    }

    public void SaveProfile(int societyId, string name, string? description)
    {
        _society.UpdateSocietyProfile(societyId, _headUserId, name, description);
        _activity.Log(_headUserId, "SocietyUpdate", "Society", societyId, null);
    }

    public int RequestNewSociety(string name)
    {
        var id = _society.CreateSociety(_headUserId, name, null);
        _activity.Log(_headUserId, "SocietyCreate", "Society", id, name);
        return id;
    }

    public void ApproveOrRejectRequest(int requestId, bool approve)
    {
        if (approve)
            _society.ApproveMembershipRequest(requestId, _headUserId);
        else
            _society.RejectMembershipRequest(requestId, _headUserId);
        _activity.Log(_headUserId, approve ? "MembershipApprove" : "MembershipReject", "MembershipRequest", requestId, null);
    }

    public void DeactivateMember(int societyId, int memberUserId)
    {
        _society.SetMemberActive(societyId, _headUserId, memberUserId, false);
        _activity.Log(_headUserId, "MemberDeactivate", "Society", societyId, memberUserId.ToString());
    }

    public int CreateDraftEvent(int societyId, string title, string? venue, DateTimeOffset startsAt, int? capacity)
    {
        var id = _events.CreateEvent(societyId, _headUserId, title, null, venue, startsAt, null, capacity);
        _activity.Log(_headUserId, "EventCreate", "Event", id, title);
        return id;
    }

    public void PublishEvent(int societyId, int eventId)
    {
        var row = _events.ListForSociety(societyId).First(ev => ev.EventId == eventId);
        if (row.AdminStatus != EventAdminStatus.Approved)
            throw new InvalidOperationException("Admin must approve the event first.");
        _events.UpdateEvent(eventId, societyId, _headUserId, row.Title, row.Description, row.Venue, row.StartsAt, row.EndsAt, row.Capacity, EventLifecycleStatus.Published);
        _activity.Log(_headUserId, "EventPublish", "Event", eventId, null);
    }

    public void CancelEvent(int societyId, int eventId)
    {
        var row = _events.ListForSociety(societyId).First(ev => ev.EventId == eventId);
        _events.UpdateEvent(eventId, societyId, _headUserId, row.Title, row.Description, row.Venue, row.StartsAt, row.EndsAt, row.Capacity, EventLifecycleStatus.Cancelled);
        _activity.Log(_headUserId, "EventCancel", "Event", eventId, null);
    }

    public int AssignTask(int societyId, int assigneeUserId, string title)
    {
        var id = _tasks.CreateTask(societyId, _headUserId, assigneeUserId, title, null, null);
        _activity.Log(_headUserId, "TaskCreate", "SocietyTask", id, null);
        return id;
    }

    public (int Members, int Events, int PendingRequests) SocietyReport(int societyId) =>
        _society.GetSocietyReport(societyId);
}
