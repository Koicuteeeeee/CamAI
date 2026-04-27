CREATE OR ALTER PROCEDURE [dbo].[sp_FaceProfile_Register]
    @FullName NVARCHAR(100),
    @ExternalCode NVARCHAR(50) = NULL,
    @ProfileType NVARCHAR(20) = 'Resident',
    @EmbeddingFront VARBINARY(MAX),
    @EmbeddingLeft VARBINARY(MAX),
    @EmbeddingRight VARBINARY(MAX),
    @MinioFront NVARCHAR(500),
    @MinioLeft NVARCHAR(500),
    @MinioRight NVARCHAR(500),
    @CreatedBy NVARCHAR(100) = 'System'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NewProfileId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Thêm Profile
        INSERT INTO [dbo].[FaceProfiles] ([Id], [FullName], [ExternalCode], [ProfileType], [CreatedBy], [UpdatedBy])
        VALUES (@NewProfileId, @FullName, @ExternalCode, @ProfileType, @CreatedBy, @CreatedBy);

        -- Thêm Khuôn mặt
        INSERT INTO [dbo].[UserFaces] ([ProfileId], [MinioFront], [MinioLeft], [MinioRight], [EmbeddingFront], [EmbeddingLeft], [EmbeddingRight], [CreatedBy], [UpdatedBy])
        VALUES (@NewProfileId, @MinioFront, @MinioLeft, @MinioRight, @EmbeddingFront, @EmbeddingLeft, @EmbeddingRight, @CreatedBy, @CreatedBy);

        COMMIT TRANSACTION;
        SELECT @NewProfileId AS NewProfileId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
