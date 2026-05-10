namespace SMM.Desktop.Workspace;

public interface IStudentWorkspace
{
    StudentDashboard LoadDashboard();
    void ApplyToSociety(int societyId);
    string RegisterForEvent(int eventId);
}
