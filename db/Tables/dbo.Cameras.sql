CREATE TABLE [dbo].[Cameras] (
    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
[CameraName] nvarchar(200) NOT NULL,
[StreamUrl] nvarchar(500) NOT NULL,
[RecognitionThreshold] float NULL DEFAULT ((0.4)),
[IsActive] bit NULL DEFAULT ((1)),
[CreatedAt] datetime NULL DEFAULT (getdate()),
[CreatedBy] nvarchar(100) NULL,
[UpdatedAt] datetime NOT NULL DEFAULT (getdate()),
[UpdatedBy] nvarchar(100) NULL
);
GO