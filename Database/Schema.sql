-- Societies Management System - SQL Server schema
-- Maps to student, society, and admin functional requirements.

IF DB_ID(N'SocietiesManagement') IS NULL
    CREATE DATABASE SocietiesManagement;
GO

USE SocietiesManagement;
GO

-- Roles: 1=Student, 2=SocietyHead, 3=SocietyMember (can overlap with Student), 4=Admin
CREATE TABLE dbo.[User] (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    Email           NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(512) NOT NULL,
    FullName        NVARCHAR(200) NOT NULL,
    Role            TINYINT NOT NULL CHECK (Role IN (1, 2, 3, 4)),
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    LastLoginAt     DATETIME2 NULL
);

CREATE TABLE dbo.StudentProfile (
    UserId          INT NOT NULL PRIMARY KEY
        REFERENCES dbo.[User](UserId) ON DELETE CASCADE,
    StudentNumber   NVARCHAR(50) NULL UNIQUE,
    Program         NVARCHAR(100) NULL,
    YearOfStudy     TINYINT NULL
);

CREATE TABLE dbo.Society (
    SocietyId       INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(MAX) NULL,
    HeadUserId      INT NOT NULL
        REFERENCES dbo.[User](UserId),
    Status          TINYINT NOT NULL DEFAULT 0, -- 0=Pending, 1=Approved, 2=Suspended, 3=Rejected/Deleted
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ApprovedByUserId INT NULL REFERENCES dbo.[User](UserId),
    ApprovedAt      DATETIME2 NULL,
    CONSTRAINT UQ_Society_Name UNIQUE (Name)
);

CREATE TABLE dbo.MembershipRequest (
    RequestId       INT IDENTITY(1,1) PRIMARY KEY,
    SocietyId       INT NOT NULL REFERENCES dbo.Society(SocietyId),
    StudentUserId   INT NOT NULL REFERENCES dbo.[User](UserId),
    Status          TINYINT NOT NULL DEFAULT 0, -- 0=Pending, 1=Approved, 2=Rejected
    RequestedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    RespondedAt     DATETIME2 NULL,
    RespondedByUserId INT NULL REFERENCES dbo.[User](UserId)
);
-- Application layer: optionally restrict one open Pending request per student/society.

CREATE TABLE dbo.SocietyMembership (
    SocietyId       INT NOT NULL REFERENCES dbo.Society(SocietyId),
    UserId          INT NOT NULL REFERENCES dbo.[User](UserId),
    IsHead          BIT NOT NULL DEFAULT 0,
    JoinedAt        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Status          TINYINT NOT NULL DEFAULT 1, -- 1=Active, 0=Left/Removed
    PRIMARY KEY (SocietyId, UserId)
);

CREATE TABLE dbo.[Event] (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    SocietyId       INT NOT NULL REFERENCES dbo.Society(SocietyId),
    Title           NVARCHAR(300) NOT NULL,
    Description     NVARCHAR(MAX) NULL,
    Venue           NVARCHAR(300) NULL,
    StartsAt        DATETIME2 NOT NULL,
    EndsAt          DATETIME2 NULL,
    Capacity        INT NULL,
    AdminStatus     TINYINT NOT NULL DEFAULT 0, -- 0=Pending admin, 1=Approved, 2=Rejected
    EventStatus     TINYINT NOT NULL DEFAULT 0, -- 0=Draft, 1=Published, 2=Cancelled
    CreatedByUserId INT NOT NULL REFERENCES dbo.[User](UserId),
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ApprovedByUserId INT NULL REFERENCES dbo.[User](UserId),
    ApprovedAt      DATETIME2 NULL
);

CREATE TABLE dbo.EventRegistration (
    RegistrationId  INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT NOT NULL REFERENCES dbo.[Event](EventId),
    UserId          INT NOT NULL REFERENCES dbo.[User](UserId),
    RegisteredAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TicketCode      NVARCHAR(64) NOT NULL UNIQUE,
    CONSTRAINT UQ_EventRegistration_User UNIQUE (EventId, UserId)
);

CREATE TABLE dbo.SocietyTask (
    TaskId          INT IDENTITY(1,1) PRIMARY KEY,
    SocietyId       INT NOT NULL REFERENCES dbo.Society(SocietyId),
    Title           NVARCHAR(300) NOT NULL,
    Description     NVARCHAR(MAX) NULL,
    AssignedToUserId INT NOT NULL REFERENCES dbo.[User](UserId),
    AssignedByUserId INT NOT NULL REFERENCES dbo.[User](UserId),
    DueDate         DATETIME2 NULL,
    Status          TINYINT NOT NULL DEFAULT 0, -- 0=Open, 1=InProgress, 2=Done, 3=Cancelled
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Admin monitoring / audit trail
CREATE TABLE dbo.ActivityLog (
    LogId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT NULL REFERENCES dbo.[User](UserId),
    ActionType      NVARCHAR(100) NOT NULL,
    EntityType      NVARCHAR(50) NULL,
    EntityId        INT NULL,
    Details         NVARCHAR(MAX) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE INDEX IX_Society_Status ON dbo.Society(Status);
CREATE INDEX IX_MembershipRequest_Society_Status ON dbo.MembershipRequest(SocietyId, Status);
CREATE INDEX IX_MembershipRequest_Student ON dbo.MembershipRequest(StudentUserId);
CREATE INDEX IX_Event_Society_Starts ON dbo.[Event](SocietyId, StartsAt);
CREATE INDEX IX_Event_AdminStatus ON dbo.[Event](AdminStatus);
CREATE INDEX IX_ActivityLog_CreatedAt ON dbo.ActivityLog(CreatedAt);

GO
