USE [CamAI];
GO

-- 1. Xóa cột ActionTaken vì đã được tách ra
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccessLogs]') AND name = N'ActionTaken')
BEGIN
    ALTER TABLE [dbo].[AccessLogs] DROP COLUMN [ActionTaken];
END
GO

-- 2. Cập nhật Stored Procedure Insert (Loại bỏ ActionTaken)
CREATE OR ALTER PROCEDURE [dbo].[sp_AccessLog_Insert]
    @UserId UNIQUEIDENTIFIER = NULL,
    @MinioLogImage NVARCHAR(255) = NULL,
    @DeviceImpacted NVARCHAR(100) = NULL,
    @RecognitionStatus NVARCHAR(20) = NULL,
    @ConfidenceScore FLOAT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[AccessLogs] 
        ([UserId], [MinioLogImage], [DeviceImpacted], [RecognitionStatus], [ConfidenceScore])
    VALUES 
        (@UserId, @MinioLogImage, @DeviceImpacted, @RecognitionStatus, @ConfidenceScore);
END
GO
