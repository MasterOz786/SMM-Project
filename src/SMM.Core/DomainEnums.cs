namespace SMM.Core;

public enum SocietyStatus : byte
{
    Pending = 0,
    Approved = 1,
    Suspended = 2,
    RejectedOrDeleted = 3
}

public enum MembershipRequestStatus : byte
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum EventAdminStatus : byte
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum EventLifecycleStatus : byte
{
    Draft = 0,
    Published = 1,
    Cancelled = 2
}

public enum SocietyTaskStatus : byte
{
    Open = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3
}
