CREATE OR ALTER PROCEDURE [dbo].[sp_FaceProfile_GetAllEmbeddings]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        f.ProfileId,
        p.FullName,
        f.EmbeddingFront,
        f.EmbeddingLeft,
        f.EmbeddingRight,
        f.MinioFront,
        f.MinioLeft,
        f.MinioRight
    FROM [dbo].[UserFaces] f
    JOIN [dbo].[FaceProfiles] p ON f.ProfileId = p.Id;
END
GO
