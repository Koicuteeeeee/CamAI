CREATE OR ALTER PROCEDURE [dbo].[sp_User_GetAllActive]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [Id], [Username], [FullName], [CreatedAt], [IsActive]
    FROM [dbo].[Users]
    WHERE [IsActive] = 1
    ORDER BY [Id] DESC;
END
GO
