CREATE TABLE [dbo].[AccessLogs] (
    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
[LogTime] datetime NOT NULL DEFAULT (getdate()),
[ProfileId] uniqueidentifier NULL,
[MinioLogImage] nvarchar(255) NULL,
[DeviceImpacted] nvarchar(100) NULL,
[RecognitionStatus] nvarchar(20) NULL,
[ConfidenceScore] float NULL,
[FullName] nvarchar(100) NULL,
[Similarity] float NULL,
[CreatedAt] datetime NOT NULL DEFAULT (getdate()),
[CreatedBy] nvarchar(100) NULL,
[UpdatedAt] datetime NOT NULL DEFAULT (getdate()),
[UpdatedBy] nvarchar(100) NULL
);
GO