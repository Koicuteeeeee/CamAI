-- Bảng mới: Lưu N embedding đa góc độ cho mỗi Profile
-- Thay thế cho bảng UserFaces cũ (chỉ hỗ trợ 3 góc cố định)
CREATE TABLE [dbo].[FaceEmbeddings] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ProfileId] UNIQUEIDENTIFIER NOT NULL,
    [AngleLabel] NVARCHAR(30) NOT NULL,         -- 'front', 'left', 'right', 'up', 'down', 'tilt_left', 'tilt_right', ...
    [AngleDegree] FLOAT NULL,                   -- Góc ước lượng (độ) từ Landmark, nullable
    [Embedding] VARBINARY(MAX) NOT NULL,         -- Vector 128-dim (SFace) dưới dạng byte[]
    [MinioImageUrl] NVARCHAR(500) NULL,          -- Đường dẫn ảnh trên MinIO
    [CaptureQuality] FLOAT NULL DEFAULT 0,       -- Confidence từ YuNet (0..1)
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    CONSTRAINT [FK_FaceEmbeddings_FaceProfiles] FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[FaceProfiles]([Id]) ON DELETE CASCADE
);
GO

-- Index để query nhanh theo ProfileId
CREATE NONCLUSTERED INDEX [IX_FaceEmbeddings_ProfileId] ON [dbo].[FaceEmbeddings] ([ProfileId]);
GO
