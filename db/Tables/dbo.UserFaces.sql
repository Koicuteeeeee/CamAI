CREATE TABLE [dbo].[UserFaces] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ProfileId] UNIQUEIDENTIFIER NOT NULL,
    [MinioFront] NVARCHAR(500) NULL,
    [MinioLeft] NVARCHAR(500) NULL,
    [MinioRight] NVARCHAR(500) NULL,
    [EmbeddingFront] VARBINARY(MAX) NULL,
    [EmbeddingLeft] VARBINARY(MAX) NULL,
    [EmbeddingRight] VARBINARY(MAX) NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedBy] NVARCHAR(100) NULL,
    CONSTRAINT [FK_UserFaces_FaceProfiles] FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[FaceProfiles]([Id]) ON DELETE CASCADE
);
GO
