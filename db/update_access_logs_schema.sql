USE [CamAI];
GO

-- 1. Sửa bảng AccessLogs (Thêm cột mới, giữ cột cũ để tránh mất data nếu có)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccessLogs]') AND name = N'Status')
BEGIN
    ALTER TABLE [dbo].[AccessLogs] ADD [Status] NVARCHAR(20) NULL;
    ALTER TABLE [dbo].[AccessLogs] ADD [PersonName] NVARCHAR(100) NULL;
    ALTER TABLE [dbo].[AccessLogs] ADD [Similarity] FLOAT NULL;
END
GO

-- 2. Cập nhật Stored Procedure Insert
CREATE OR ALTER PROCEDURE [dbo].[sp_AccessLog_Insert]
    @UserId UNIQUEIDENTIFIER = NULL,
    @ActionTaken NVARCHAR(100) = NULL,
    @MinioLogImage NVARCHAR(255) = NULL,
    @DeviceImpacted NVARCHAR(100) = NULL,
    @Status NVARCHAR(20) = NULL,
    @PersonName NVARCHAR(100) = NULL,
    @Similarity FLOAT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[AccessLogs] 
        ([UserId], [ActionTaken], [MinioLogImage], [DeviceImpacted], [Status], [PersonName], [Similarity])
    VALUES 
        (@UserId, @ActionTaken, @MinioLogImage, @DeviceImpacted, @Status, @PersonName, @Similarity);
END
GO
