CREATE OR ALTER PROCEDURE [dbo].[sp_UserFace_GetAll]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        uf.[Id], 
        uf.[UserId], 
        u.[FullName], 
        uf.[EmbeddingFront], 
        uf.[EmbeddingLeft], 
        uf.[EmbeddingRight], 
        uf.[MinioFront],
        uf.[MinioLeft],
        uf.[MinioRight]
    FROM [dbo].[UserFaces] uf
    INNER JOIN [dbo].[Users] u ON uf.[UserId] = u.[Id]
    WHERE u.[IsActive] = 1;
END
GO
