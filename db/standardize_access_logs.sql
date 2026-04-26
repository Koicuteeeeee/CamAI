USE [CamAI];
GO

-- 1. Chuẩn hóa bảng AccessLogs
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccessLogs]') AND name = N'PersonName')
BEGIN
    ALTER TABLE [dbo].[AccessLogs] DROP COLUMN [PersonName];
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccessLogs]') AND name = N'Status')
BEGIN
    EXEC sp_rename 'dbo.AccessLogs.Status', 'RecognitionStatus', 'COLUMN';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccessLogs]') AND name = N'Similarity')
BEGIN
    EXEC sp_rename 'dbo.AccessLogs.Similarity', 'ConfidenceScore', 'COLUMN';
END
GO

-- 2. Cập nhật Stored Procedure Insert
CREATE OR ALTER PROCEDURE [dbo].[sp_AccessLog_Insert]
    @UserId UNIQUEIDENTIFIER = NULL,
    @ActionTaken NVARCHAR(100) = NULL,
    @MinioLogImage NVARCHAR(255) = NULL,
    @DeviceImpacted NVARCHAR(100) = NULL,
    @RecognitionStatus NVARCHAR(20) = NULL,
    @ConfidenceScore FLOAT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[AccessLogs] 
        ([UserId], [ActionTaken], [MinioLogImage], [DeviceImpacted], [RecognitionStatus], [ConfidenceScore])
    VALUES 
        (@UserId, @ActionTaken, @MinioLogImage, @DeviceImpacted, @RecognitionStatus, @ConfidenceScore);
END
GO
