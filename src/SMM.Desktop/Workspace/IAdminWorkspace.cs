using SMM.Core;

namespace SMM.Desktop.Workspace;

public interface IAdminWorkspace
{
    AdminDashboard LoadDashboard();
    void SetUserActive(int targetUserId, bool active);
    void SetSocietyStatus(int societyId, SocietyStatus status);
    void SetEventAdminStatus(int eventId, EventAdminStatus status);
    (int Students, int Societies, int Events, int OpenTasks) UniversityReport();
}
