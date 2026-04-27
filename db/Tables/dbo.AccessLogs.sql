CREATE TABLE [dbo].[AccessLogs] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [LogTime] DATETIME NOT NULL DEFAULT GETDATE(),
    [ProfileId] UNIQUEIDENTIFIER NULL,
    [FullName] NVARCHAR(100) NULL,
    [RecognitionStatus] NVARCHAR(20) NULL,
    [Similarity] FLOAT NULL,
    [MinioLogImage] NVARCHAR(500) NULL,
    [DeviceImpacted] NVARCHAR(100) NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedBy] NVARCHAR(100) NULL,
    CONSTRAINT [FK_AccessLogs_FaceProfiles] FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[FaceProfiles]([Id]) ON DELETE SET NULL
);
GO
