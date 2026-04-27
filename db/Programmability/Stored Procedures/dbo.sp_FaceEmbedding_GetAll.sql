CREATE OR ALTER PROCEDURE [dbo].[sp_FaceEmbedding_GetAll]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        e.Id,
        e.ProfileId,
        p.FullName,
        e.AngleLabel,
        e.AngleDegree,
        e.Embedding,
        e.MinioImageUrl,
        e.CaptureQuality
    FROM [dbo].[FaceEmbeddings] e
    JOIN [dbo].[FaceProfiles] p ON e.ProfileId = p.Id
    ORDER BY e.ProfileId, e.AngleLabel;
END
GO
