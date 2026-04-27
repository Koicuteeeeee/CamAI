CREATE TABLE [dbo].[Users] (
    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
[KeycloakId] uniqueidentifier NULL,
[Username] nvarchar(50) NOT NULL,
[Email] nvarchar(100) NULL,
[FullName] nvarchar(100) NULL,
[Role] nvarchar(20) NULL DEFAULT ('Staff'),
[IsActive] bit NULL DEFAULT ((1)),
[CreatedAt] datetime NOT NULL DEFAULT (getdate()),
[CreatedBy] nvarchar(100) NULL,
[UpdatedAt] datetime NOT NULL DEFAULT (getdate()),
[UpdatedBy] nvarchar(100) NULL
);
GO