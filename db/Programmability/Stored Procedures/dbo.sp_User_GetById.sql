CREATE OR ALTER PROCEDURE [dbo].[sp_User_GetById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [Id], [Username], [FullName], [CreatedAt], [IsActive]
    FROM [dbo].[Users]
    WHERE [Id] = @Id AND [IsActive] = 1;
END
GO
