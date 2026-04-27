USE [CamAI];
GO

-- 1. Tạo bảng CameraEvents
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CameraEvents]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CameraEvents] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [EventTime] DATETIME NOT NULL DEFAULT GETDATE(),
        [CameraId] UNIQUEIDENTIFIER NULL,
        [CameraName] NVARCHAR(100) NULL,
        [EventType] NVARCHAR(50) NOT NULL, -- DISCONNECTED, CONNECTED, RECOVERY, ENROLL_START, ENROLL_COMPLETE
        [Description] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedBy] NVARCHAR(100) NULL
    );
END
GO

-- 2. Procedure chèn Event
CREATE OR ALTER PROCEDURE [dbo].[sp_CameraEvent_Insert]
    @CameraId UNIQUEIDENTIFIER = NULL,
    @CameraName NVARCHAR(100) = NULL,
    @EventType NVARCHAR(50),
    @Description NVARCHAR(MAX),
    @CreatedBy NVARCHAR(100) = 'Camera Service'
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[CameraEvents] ([CameraId], [CameraName], [EventType], [Description], [CreatedBy], [UpdatedBy])
    VALUES (@CameraId, @CameraName, @EventType, @Description, @CreatedBy, @CreatedBy);
END
GO
