using SMM.Core;

namespace SMM.Desktop.Workspace;

public interface ISocietyHeadWorkspace
{
    IReadOnlyList<SocietyRow> ListMySocieties();
    HeadSocietyDetail LoadDetail(int societyId);
    void SaveProfile(int societyId, string name, string? description);
    int RequestNewSociety(string name);
    void ApproveOrRejectRequest(int requestId, bool approve);
    void DeactivateMember(int societyId, int memberUserId);
    int CreateDraftEvent(int societyId, string title, string? venue, DateTimeOffset startsAt, int? capacity);
    void PublishEvent(int societyId, int eventId);
    void CancelEvent(int societyId, int eventId);
    int AssignTask(int societyId, int assigneeUserId, string title);
    (int Members, int Events, int PendingRequests) SocietyReport(int societyId);
}
