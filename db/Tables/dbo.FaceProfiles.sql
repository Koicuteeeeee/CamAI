CREATE TABLE [dbo].[FaceProfiles] (
    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
[FullName] nvarchar(100) NOT NULL,
[CreatedAt] datetime NOT NULL DEFAULT (getdate()),
[IsActive] bit NOT NULL DEFAULT ((1)),
[CreatedBy] nvarchar(100) NULL,
[UpdatedAt] datetime NOT NULL DEFAULT (getdate()),
[UpdatedBy] nvarchar(100) NULL,
[ExternalCode] nvarchar(50) NULL,
[ProfileType] nvarchar(20) NULL DEFAULT ('Resident')
);
GO