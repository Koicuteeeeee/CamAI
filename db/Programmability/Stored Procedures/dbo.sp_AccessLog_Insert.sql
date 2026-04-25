CREATE OR ALTER PROCEDURE [dbo].[sp_AccessLog_Insert]
    @UserId UNIQUEIDENTIFIER,
    @ActionTaken NVARCHAR(100),
    @MinioLogImage NVARCHAR(255),
    @DeviceImpacted NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[AccessLogs] ([UserId], [ActionTaken], [MinioLogImage], [DeviceImpacted], [LogTime])
    VALUES (@UserId, @ActionTaken, @MinioLogImage, @DeviceImpacted, GETDATE());
END
GO
