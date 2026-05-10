USE SocietiesManagement;
GO

-- Default admin: email admin@uni.edu / password Admin123
-- Hash from SMM.Core.Security.PasswordHasher (PBKDF2-SHA256, 100k iterations)
IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE Email = N'admin@uni.edu')
BEGIN
    INSERT INTO dbo.[User] (Email, PasswordHash, FullName, Role, IsActive)
    VALUES (
        N'admin@uni.edu',
        N'v1:100000:R4UD7E9h1FbBF3CILRDodw==:WWRlA1BaGNT7pojAi0cQWbXWZz+meVCAPSxaf3CRT6Y=',
        N'University Admin',
        4,
        1
    );
END
ELSE
BEGIN
    UPDATE dbo.[User]
    SET PasswordHash = N'v1:100000:R4UD7E9h1FbBF3CILRDodw==:WWRlA1BaGNT7pojAi0cQWbXWZz+meVCAPSxaf3CRT6Y='
    WHERE Email = N'admin@uni.edu';
END
GO
