CREATE OR ALTER PROCEDURE [dbo].[sp_AccessLog_Insert]
    @ProfileId UNIQUEIDENTIFIER = NULL, -- Cho phép NULL cho người lạ
    @FullName NVARCHAR(100) = NULL,
    @RecognitionStatus NVARCHAR(20),
    @Similarity FLOAT = NULL,
    @MinioLogImage NVARCHAR(500) = NULL,
    @DeviceImpacted NVARCHAR(100) = NULL,
    @CreatedBy NVARCHAR(100) = 'AI Engine'
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[AccessLogs] ([ProfileId], [FullName], [RecognitionStatus], [Similarity], [MinioLogImage], [DeviceImpacted], [LogTime], [CreatedBy], [UpdatedBy])
    VALUES (@ProfileId, @FullName, @RecognitionStatus, @Similarity, @MinioLogImage, @DeviceImpacted, GETDATE(), @CreatedBy, @CreatedBy);
END
GO
