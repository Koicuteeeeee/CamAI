CREATE TABLE [dbo].[SystemEvents] (
    [Id] uniqueidentifier NOT NULL DEFAULT (newid()),
[EventTime] datetime NOT NULL DEFAULT (getdate()),
[Level] nvarchar(20) NOT NULL,
[Source] nvarchar(100) NOT NULL,
[Message] nvarchar(MAX) NOT NULL,
[Exception] nvarchar(MAX) NULL
);
GO