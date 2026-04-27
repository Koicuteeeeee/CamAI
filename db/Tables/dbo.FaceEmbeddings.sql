CREATE TABLE [dbo].[FaceEmbeddings] (
    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
[ProfileId] uniqueidentifier NOT NULL,
[AngleLabel] nvarchar(30) NOT NULL,
[AngleDegree] float NULL,
[Embedding] varbinary(MAX) NOT NULL,
[MinioImageUrl] nvarchar(500) NULL,
[CaptureQuality] float NULL DEFAULT ((0)),
[CreatedAt] datetime NOT NULL DEFAULT (getdate()),
[CreatedBy] nvarchar(100) NULL
);
GO