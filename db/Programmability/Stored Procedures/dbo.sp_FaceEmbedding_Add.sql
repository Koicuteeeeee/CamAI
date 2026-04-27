definition                                                                                                                                                                                                                                                      
CREATE   PROCEDURE [dbo].[sp_FaceEmbedding_Add]
    @ProfileId UNIQUEIDENTIFIER,
    @AngleLabel NVARCHAR(30),
    @AngleDegree FLOAT = NULL,
    @Embedding VARBINARY(MAX),
    @MinioImageUrl NVARCHAR(500) = NULL,
    @CaptureQuality FLOAT = NULL,
    @Cre
GO