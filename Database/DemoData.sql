/*
  Demo / realistic dataset for SocietiesManagement.
  Run AFTER: Schema.sql and Seed.sql (admin must exist).

  All student/head demo accounts use password: Student123
  Admin remains: admin@uni.edu / Admin123 (from Seed.sql)
*/
USE SocietiesManagement;
GO

SET NOCOUNT ON;

DECLARE @AdminId INT = (SELECT TOP 1 UserId FROM dbo.[User] WHERE Email = N'admin@uni.edu');
IF @AdminId IS NULL
BEGIN
    RAISERROR('Run Seed.sql first so admin@uni.edu exists.', 16, 1);
    RETURN;
END

/* --- Users (PBKDF2 hashes for password Student123, one per user) --- */
IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE Email = N'alice.chen@campus.edu')
INSERT INTO dbo.[User] (Email, PasswordHash, FullName, Role, IsActive) VALUES
(N'alice.chen@campus.edu', N'v1:100000:2ScODfSgdPSzs9lAqqincA==:MGGLuhmpMOtrc/ma8kGkaAlhU998c6gKQTP8FVhH298=', N'Alice Chen', 1, 1),
(N'bob.okeefe@campus.edu', N'v1:100000:CTDY+GwpHE7iebh0IcK04g==:7CURPBvSjFh4gAt0Gy/MPBarDb/FvsT7cB2parC0Rzs=', N'Bob O''Keefe', 1, 1),
(N'carol.dias@campus.edu', N'v1:100000:6gEORoRLwJ1JfgZXbg0ckw==:EwaFcJYx1Jwa65h86v4hZW7warp6EvI/Msn+2M5hJ0I=', N'Carol Dias', 1, 1),
(N'dave.patel@campus.edu', N'v1:100000:+3qNpKMqSnka5Hxr6UBw1A==:zcHQZibkS987ijbzBJq85+qLb0ozWu2J3ZQp29UcDlk=', N'Dave Patel', 1, 1),
(N'eve.martin@campus.edu', N'v1:100000:OOsTRYyyYCrGwRh2RWmy9w==:EzE9mMhzyFrhUyHByhPJZlt48uOz6wvRA8tE/9T1jE4=', N'Eve Martin', 1, 1),
(N'frank.nguyen@campus.edu', N'v1:100000:QHV97HzZwS8ZP3//LOJZdw==:Uza1yo1xse1Q4lcvYvklIPGfvlcTfuAD1QaRECK2PAQ=', N'Frank Nguyen', 1, 1),
(N'grace.ali@campus.edu', N'v1:100000:4Ir5mRu9nv+1NPHemUegRQ==:jYUf1UHpgrwICBmqW2qmLm33hqcUPW0UmM33MlxfL90=', N'Grace Ali', 1, 1),
(N'henry.kim@campus.edu', N'v1:100000:ErsqZhdEgaRVyb04gYvvsg==:c/xO0IRXSO/VE/HT06a74YpRy4pnbg6Jwh2ANXg3E/o=', N'Henry Kim', 1, 1),
(N'iris.brown@campus.edu', N'v1:100000:nDcszGIdBJizq/lVCaPpaw==:1kbDZYsSzqN/qRoZJocy/GY7KU0Y/EzNf7zgZttmCZ0=', N'Iris Brown', 1, 1),
(N'james.wilson@campus.edu', N'v1:100000:gx5Qoi1uPpoI5zuKzR0jzg==:styOmn8HVfYWgP5cL6qUsRAtO18gZYPyxhiNQpyJH4E=', N'James Wilson', 1, 1);

DECLARE
    @Alice INT = (SELECT UserId FROM dbo.[User] WHERE Email = N'alice.chen@campus.edu'),
    @Bob   INT = (SELECT UserId FROM dbo.[User] WHERE Email = N'bob.okeefe@campus.edu'),
    @Carol INT = (SELECT UserId FROM dbo.[User] WHERE Email = N'carol.dias@campus.edu'),
    @Dave  INT = (SELECT UserId FROM dbo.[User] WHERE Email = N'dave.patel@campus.edu'),
    @Eve   INT = (SELECT UserId FROM dbo.[User] WHERE Email = N'eve.martin@campus.edu'),
    @Frank INT = (SELECT UserId FROM dbo.[User] WHERE Email = N'frank.nguyen@campus.edu'),
    @Grace INT = (SELECT UserId FROM dbo.[User] WHERE Email = N'grace.ali@campus.edu'),
    @Henry INT = (SELECT UserId FROM dbo.[User] WHERE Email = N'henry.kim@campus.edu'),
    @Iris  INT = (SELECT UserId FROM dbo.[User] WHERE Email = N'iris.brown@campus.edu'),
    @James INT = (SELECT UserId FROM dbo.[User] WHERE Email = N'james.wilson@campus.edu');

/* Student profiles */
MERGE dbo.StudentProfile AS t
USING (VALUES
    (@Alice, N'U10001', N'Computer Science', 3),
    (@Bob,   N'U10002', N'Music Performance', 2),
    (@Carol, N'U10003', N'English Literature', 4),
    (@Dave,  N'U10004', N'Film Studies', 2),
    (@Eve,   N'U10005', N'Information Systems', 1),
    (@Frank, N'U10006', N'Electrical Engineering', 3),
    (@Grace, N'U10007', N'Biology', 2),
    (@Henry, N'U10008', N'Mathematics', 4),
    (@Iris,  N'U10009', N'Political Science', 2),
    (@James, N'U10010', N'Economics', 3)
) AS s(UserId, StudentNumber, Program, YearOfStudy)
ON t.UserId = s.UserId
WHEN NOT MATCHED THEN INSERT (UserId, StudentNumber, Program, YearOfStudy) VALUES (s.UserId, s.StudentNumber, s.Program, s.YearOfStudy);

/* Societies */
IF NOT EXISTS (SELECT 1 FROM dbo.Society WHERE Name = N'IEEE Robotics & Software Guild')
INSERT INTO dbo.Society (Name, Description, HeadUserId, Status, ApprovedByUserId, ApprovedAt)
VALUES (N'IEEE Robotics & Software Guild', N'Build nights, microcontroller workshops, and an annual internal hackathon.', @Alice, 1, @AdminId, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Society WHERE Name = N'Campus Jazz & Soul Collective')
INSERT INTO dbo.Society (Name, Description, HeadUserId, Status, ApprovedByUserId, ApprovedAt)
VALUES (N'Campus Jazz & Soul Collective', N'Big band rehearsals, small combos, and outdoor quad concerts each term.', @Bob, 1, @AdminId, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Society WHERE Name = N'Literary Debate Union')
INSERT INTO dbo.Society (Name, Description, HeadUserId, Status, ApprovedByUserId, ApprovedAt)
VALUES (N'Literary Debate Union', N'Parliamentary debate, poetry slams, and inter-university fixtures.', @Carol, 0, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.Society WHERE Name = N'Vintage Cinema Society')
INSERT INTO dbo.Society (Name, Description, HeadUserId, Status, ApprovedByUserId, ApprovedAt)
VALUES (N'Vintage Cinema Society', N'35mm screenings and guest lectures — currently on administrative hiatus.', @Dave, 2, @AdminId, SYSUTCDATETIME());

DECLARE
    @SocTech  INT = (SELECT SocietyId FROM dbo.Society WHERE Name = N'IEEE Robotics & Software Guild'),
    @SocMusic INT = (SELECT SocietyId FROM dbo.Society WHERE Name = N'Campus Jazz & Soul Collective'),
    @SocDebate INT = (SELECT SocietyId FROM dbo.Society WHERE Name = N'Literary Debate Union'),
    @SocFilm INT = (SELECT SocietyId FROM dbo.Society WHERE Name = N'Vintage Cinema Society');

/* Head memberships */
INSERT INTO dbo.SocietyMembership (SocietyId, UserId, IsHead, Status)
SELECT @SocTech, @Alice, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM dbo.SocietyMembership WHERE SocietyId = @SocTech AND UserId = @Alice);
INSERT INTO dbo.SocietyMembership (SocietyId, UserId, IsHead, Status)
SELECT @SocMusic, @Bob, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM dbo.SocietyMembership WHERE SocietyId = @SocMusic AND UserId = @Bob);
INSERT INTO dbo.SocietyMembership (SocietyId, UserId, IsHead, Status)
SELECT @SocDebate, @Carol, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM dbo.SocietyMembership WHERE SocietyId = @SocDebate AND UserId = @Carol);
INSERT INTO dbo.SocietyMembership (SocietyId, UserId, IsHead, Status)
SELECT @SocFilm, @Dave, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM dbo.SocietyMembership WHERE SocietyId = @SocFilm AND UserId = @Dave);

/* Regular members */
INSERT INTO dbo.SocietyMembership (SocietyId, UserId, IsHead, Status)
SELECT @SocTech, @Eve, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM dbo.SocietyMembership WHERE SocietyId = @SocTech AND UserId = @Eve);
INSERT INTO dbo.SocietyMembership (SocietyId, UserId, IsHead, Status)
SELECT @SocTech, @Frank, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM dbo.SocietyMembership WHERE SocietyId = @SocTech AND UserId = @Frank);
INSERT INTO dbo.SocietyMembership (SocietyId, UserId, IsHead, Status)
SELECT @SocMusic, @Eve, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM dbo.SocietyMembership WHERE SocietyId = @SocMusic AND UserId = @Eve);
INSERT INTO dbo.SocietyMembership (SocietyId, UserId, IsHead, Status)
SELECT @SocMusic, @Iris, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM dbo.SocietyMembership WHERE SocietyId = @SocMusic AND UserId = @Iris);

/* Pending membership request */
IF NOT EXISTS (SELECT 1 FROM dbo.MembershipRequest WHERE SocietyId = @SocTech AND StudentUserId = @Grace AND Status = 0)
INSERT INTO dbo.MembershipRequest (SocietyId, StudentUserId, Status)
VALUES (@SocTech, @Grace, 0);

/* Rejected request (history) */
IF NOT EXISTS (SELECT 1 FROM dbo.MembershipRequest WHERE SocietyId = @SocMusic AND StudentUserId = @Henry AND Status = 2)
INSERT INTO dbo.MembershipRequest (SocietyId, StudentUserId, Status, RespondedAt, RespondedByUserId)
VALUES (@SocMusic, @Henry, 2, SYSUTCDATETIME(), @Bob);

/* Events */
IF NOT EXISTS (SELECT 1 FROM dbo.[Event] WHERE SocietyId = @SocTech AND Title = N'Spring Hack Night 2026')
INSERT INTO dbo.[Event] (SocietyId, Title, Description, Venue, StartsAt, EndsAt, Capacity, AdminStatus, EventStatus, CreatedByUserId, ApprovedByUserId, ApprovedAt)
VALUES (@SocTech, N'Spring Hack Night 2026', N'24h build sprint; meals provided. Teams of up to 4.', N'SEAS Lab Wing B', DATEADD(DAY, 30, SYSUTCDATETIME()), DATEADD(HOUR, 26, DATEADD(DAY, 30, SYSUTCDATETIME())), 80, 1, 1, @Alice, @AdminId, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.[Event] WHERE SocietyId = @SocTech AND Title = N'Rust Systems Study Lab')
INSERT INTO dbo.[Event] (SocietyId, Title, Description, Venue, StartsAt, EndsAt, Capacity, AdminStatus, EventStatus, CreatedByUserId)
VALUES (@SocTech, N'Rust Systems Study Lab', N'Hands-on ownership & borrowing exercises.', N'Library Room 204', DATEADD(DAY, 14, SYSUTCDATETIME()), DATEADD(HOUR, 2, DATEADD(DAY, 14, SYSUTCDATETIME())), 25, 0, 0, @Alice);

IF NOT EXISTS (SELECT 1 FROM dbo.[Event] WHERE SocietyId = @SocMusic AND Title = N'Quad Lawn Spring Concert')
INSERT INTO dbo.[Event] (SocietyId, Title, Description, Venue, StartsAt, EndsAt, Capacity, AdminStatus, EventStatus, CreatedByUserId, ApprovedByUserId, ApprovedAt)
VALUES (@SocMusic, N'Quad Lawn Spring Concert', N'Big band + guest vocalist; bring blankets.', N'Main Quad', DATEADD(DAY, 45, SYSUTCDATETIME()), DATEADD(HOUR, 3, DATEADD(DAY, 45, SYSUTCDATETIME())), 500, 1, 1, @Bob, @AdminId, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.[Event] WHERE SocietyId = @SocMusic AND Title = N'Cancelled: Midnight Jam Session')
INSERT INTO dbo.[Event] (SocietyId, Title, Description, Venue, StartsAt, EndsAt, Capacity, AdminStatus, EventStatus, CreatedByUserId, ApprovedByUserId, ApprovedAt)
VALUES (@SocMusic, N'Cancelled: Midnight Jam Session', N'Cancelled due to noise curfew.', N'Arts Annex', DATEADD(DAY, 7, SYSUTCDATETIME()), NULL, 40, 1, 2, @Bob, @AdminId, SYSUTCDATETIME());

DECLARE @EvHack INT = (SELECT EventId FROM dbo.[Event] WHERE SocietyId = @SocTech AND Title = N'Spring Hack Night 2026');
DECLARE @EvConcert INT = (SELECT EventId FROM dbo.[Event] WHERE SocietyId = @SocMusic AND Title = N'Quad Lawn Spring Concert');

/* Registrations (ticket codes fixed for demo reproducibility) */
IF @EvHack IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.EventRegistration WHERE EventId = @EvHack AND UserId = @Eve)
INSERT INTO dbo.EventRegistration (EventId, UserId, TicketCode) VALUES (@EvHack, @Eve, N'a1b2c3d4e5f6789012345678abcdef01');
IF @EvHack IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.EventRegistration WHERE EventId = @EvHack AND UserId = @Frank)
INSERT INTO dbo.EventRegistration (EventId, UserId, TicketCode) VALUES (@EvHack, @Frank, N'b2c3d4e5f6789012345678abcdef012');
IF @EvConcert IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.EventRegistration WHERE EventId = @EvConcert AND UserId = @Eve)
INSERT INTO dbo.EventRegistration (EventId, UserId, TicketCode) VALUES (@EvConcert, @Eve, N'c3d4e5f6789012345678abcdef01234');

/* Tasks */
IF NOT EXISTS (SELECT 1 FROM dbo.SocietyTask WHERE SocietyId = @SocMusic AND Title = N'Book quad power distribution')
INSERT INTO dbo.SocietyTask (SocietyId, Title, Description, AssignedToUserId, AssignedByUserId, DueDate, Status)
VALUES (@SocMusic, N'Book quad power distribution', N'Confirm amperage with facilities office.', @Iris, @Bob, DATEADD(DAY, 10, SYSUTCDATETIME()), 1);

IF NOT EXISTS (SELECT 1 FROM dbo.SocietyTask WHERE SocietyId = @SocTech AND Title = N'Order pizza vouchers for hack night')
INSERT INTO dbo.SocietyTask (SocietyId, Title, Description, AssignedToUserId, AssignedByUserId, DueDate, Status)
VALUES (@SocTech, N'Order pizza vouchers for hack night', N'Budget cap USD 400; need three vendors.', @Frank, @Alice, DATEADD(DAY, 5, SYSUTCDATETIME()), 0);

/* Activity log sample */
IF NOT EXISTS (SELECT 1 FROM dbo.ActivityLog WHERE ActionType = N'DemoSeed' AND Details = N'Initial realistic dataset')
INSERT INTO dbo.ActivityLog (UserId, ActionType, EntityType, EntityId, Details)
VALUES
(@AdminId, N'DemoSeed', N'Database', NULL, N'Initial realistic dataset'),
(@Alice, N'SocietyUpdate', N'Society', @SocTech, N'Updated workshop blurb'),
(@Bob, N'EventPublish', N'Event', @EvConcert, N'Published after admin approval');

GO

PRINT N'DemoData.sql completed. Demo password for all student emails: Student123';
GO
