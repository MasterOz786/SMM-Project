using SMM.Core;
using SMM.Data.Repositories;

namespace SMM.Desktop.Workspace;

public sealed class AdminWorkspace : IAdminWorkspace
{
    private readonly int _adminUserId;
    private readonly UserRepository _users;
    private readonly SocietyRepository _society;
    private readonly EventRepository _events;
    private readonly ActivityRepository _activity;

    public AdminWorkspace(
        int adminUserId,
        UserRepository users,
        SocietyRepository society,
        EventRepository events,
        ActivityRepository activity)
    {
        _adminUserId = adminUserId;
        _users = users;
        _society = society;
        _events = events;
        _activity = activity;
    }

    public AdminDashboard LoadDashboard() => new(
        _users.ListUsers().ToList(),
        _society.ListAllSocietiesForAdmin().ToList(),
        _events.ListPendingAdminApproval().ToList(),
        _activity.ListRecent(300).ToList());

    public void SetUserActive(int targetUserId, bool active)
    {
        _users.SetUserActive(targetUserId, active);
        _activity.Log(_adminUserId, active ? "UserActivate" : "UserSuspend", "User", targetUserId, null);
    }

    public void SetSocietyStatus(int societyId, SocietyStatus status)
    {
        _society.SetSocietyStatus(societyId, status, _adminUserId);
        _activity.Log(_adminUserId, "SocietyStatus", "Society", societyId, status.ToString());
    }

    public void SetEventAdminStatus(int eventId, EventAdminStatus status)
    {
        _events.SetAdminStatus(eventId, status, _adminUserId);
        _activity.Log(_adminUserId, "EventAdmin", "Event", eventId, status.ToString());
    }

    public (int Students, int Societies, int Events, int OpenTasks) UniversityReport() =>
        _society.GetUniversityReport();
}
