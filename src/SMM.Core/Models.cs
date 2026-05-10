namespace SMM.Core;

public sealed record AuthUser(int UserId, string Email, string FullName, UserRole Role, bool IsActive);

public sealed record SocietyRow(
    int SocietyId,
    string Name,
    string? Description,
    int HeadUserId,
    SocietyStatus Status);

public sealed record MembershipStatusRow(
    int SocietyId,
    string SocietyName,
    SocietyStatus SocietyStatus,
    bool IsMember,
    string? RequestStatus);

public sealed record MembershipRequestRow(
    int RequestId,
    int SocietyId,
    string SocietyName,
    int StudentUserId,
    string StudentName,
    string StudentEmail,
    MembershipRequestStatus Status,
    DateTimeOffset RequestedAt);

public sealed record MemberRow(int UserId, string FullName, string Email, bool IsHead, DateTimeOffset JoinedAt, bool Active);

public sealed record EventRow(
    int EventId,
    int SocietyId,
    string SocietyName,
    string Title,
    string? Description,
    string? Venue,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    int? Capacity,
    EventAdminStatus AdminStatus,
    EventLifecycleStatus EventStatus);

public sealed record TicketRow(
    int RegistrationId,
    int EventId,
    string EventTitle,
    string SocietyName,
    DateTimeOffset StartsAt,
    string TicketCode,
    DateTimeOffset RegisteredAt);

public sealed record UserAdminRow(int UserId, string Email, string FullName, UserRole Role, bool IsActive, DateTimeOffset CreatedAt);

public sealed record TaskRow(
    int TaskId,
    int SocietyId,
    string Title,
    string? Description,
    int AssignedToUserId,
    string AssignedToName,
    DateTimeOffset? DueDate,
    SocietyTaskStatus Status);

public sealed record ActivityRow(long LogId, int? UserId, string ActionType, string? EntityType, int? EntityId, string? Details, DateTimeOffset CreatedAt);
