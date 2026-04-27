CREATE TABLE [dbo].[CameraEvents] (
    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
[EventTime] datetime NOT NULL DEFAULT (getdate()),
[CameraId] uniqueidentifier NULL,
[CameraName] nvarchar(100) NULL,
[EventType] nvarchar(50) NOT NULL,
[Description] nvarchar(MAX) NOT NULL,
[CreatedAt] datetime NOT NULL DEFAULT (getdate()),
[CreatedBy] nvarchar(100) NULL,
[UpdatedAt] datetime NOT NULL DEFAULT (getdate()),
[UpdatedBy] nvarchar(100) NULL
);
GO