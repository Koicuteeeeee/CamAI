CREATE OR ALTER PROCEDURE [dbo].[sp_User_Register]
    @KeycloakId UNIQUEIDENTIFIER = NULL,
    @Username NVARCHAR(50),
    @Email NVARCHAR(100) = NULL,
    @FullName NVARCHAR(100) = NULL,
    @Role NVARCHAR(20) = 'Staff',
    @CreatedBy NVARCHAR(100) = 'System'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NewUserId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO [dbo].[Users] ([Id], [KeycloakId], [Username], [Email], [FullName], [Role], [CreatedBy], [UpdatedBy])
    VALUES (@NewUserId, @KeycloakId, @Username, @Email, @FullName, @Role, @CreatedBy, @CreatedBy);

    SELECT @NewUserId AS NewUserId;
END
GO
