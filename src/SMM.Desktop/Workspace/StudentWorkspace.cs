using SMM.Data.Repositories;

namespace SMM.Desktop.Workspace;

public sealed class StudentWorkspace : IStudentWorkspace
{
    private readonly int _studentUserId;
    private readonly SocietyRepository _society;
    private readonly EventRepository _events;
    private readonly ActivityRepository _activity;

    public StudentWorkspace(int studentUserId, SocietyRepository society, EventRepository events, ActivityRepository activity)
    {
        _studentUserId = studentUserId;
        _society = society;
        _events = events;
        _activity = activity;
    }

    public StudentDashboard LoadDashboard() => new(
        _society.ListApprovedSocieties().ToList(),
        _society.ListMembershipOverviewForStudent(_studentUserId).ToList(),
        _events.ListPublishedForStudents().ToList(),
        _events.ListTicketsForStudent(_studentUserId).ToList());

    public void ApplyToSociety(int societyId)
    {
        _society.CreateMembershipRequest(_studentUserId, societyId);
        _activity.Log(_studentUserId, "MembershipApply", "Society", societyId, null);
    }

    public string RegisterForEvent(int eventId)
    {
        var code = _events.RegisterStudent(eventId, _studentUserId);
        _activity.Log(_studentUserId, "EventRegister", "Event", eventId, code);
        return code;
    }
}
