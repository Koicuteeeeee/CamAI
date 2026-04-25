CREATE OR ALTER PROCEDURE [dbo].[sp_AccessLog_GetHistory]
    @Page INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    SELECT 
        l.[Id], 
        l.[LogTime], 
        l.[UserId], 
        u.[FullName], 
        l.[ActionTaken], 
        l.[MinioLogImage], 
        l.[DeviceImpacted]
    FROM [dbo].[AccessLogs] l
    LEFT JOIN [dbo].[Users] u ON l.[UserId] = u.[Id]
    ORDER BY l.[LogTime] DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
