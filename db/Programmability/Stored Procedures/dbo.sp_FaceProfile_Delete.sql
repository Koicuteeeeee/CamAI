CREATE OR ALTER PROCEDURE [dbo].[sp_FaceProfile_Delete]
    @ProfileId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM [dbo].[FaceProfiles] WHERE [Id] = @ProfileId;
END
GO
