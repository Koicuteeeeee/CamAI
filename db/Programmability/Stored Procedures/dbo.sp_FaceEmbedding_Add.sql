CREATE OR ALTER PROCEDURE [dbo].[sp_FaceEmbedding_Add]
    @ProfileId UNIQUEIDENTIFIER,
    @AngleLabel NVARCHAR(30),
    @AngleDegree FLOAT = NULL,
    @Embedding VARBINARY(MAX),
    @MinioImageUrl NVARCHAR(500) = NULL,
    @CaptureQuality FLOAT = NULL,
    @CreatedBy NVARCHAR(100) = 'System'
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra Profile tồn tại
    IF NOT EXISTS (SELECT 1 FROM [dbo].[FaceProfiles] WHERE [Id] = @ProfileId)
    BEGIN
        RAISERROR('ProfileId không tồn tại.', 16, 1);
        RETURN;
    END

    INSERT INTO [dbo].[FaceEmbeddings] ([ProfileId], [AngleLabel], [AngleDegree], [Embedding], [MinioImageUrl], [CaptureQuality], [CreatedBy])
    VALUES (@ProfileId, @AngleLabel, @AngleDegree, @Embedding, @MinioImageUrl, @CaptureQuality, @CreatedBy);

    SELECT SCOPE_IDENTITY() AS NewEmbeddingId;
END
GO
