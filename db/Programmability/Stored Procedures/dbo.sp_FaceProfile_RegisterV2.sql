CREATE OR ALTER PROCEDURE [dbo].[sp_FaceProfile_RegisterV2]
    @FullName NVARCHAR(100),
    @ExternalCode NVARCHAR(50) = NULL,
    @ProfileType NVARCHAR(20) = 'Resident',
    @CreatedBy NVARCHAR(100) = 'System'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NewProfileId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Thêm Profile (Không kèm embedding, embedding sẽ được thêm riêng qua sp_FaceEmbedding_Add)
        INSERT INTO [dbo].[FaceProfiles] ([Id], [FullName], [ExternalCode], [ProfileType], [CreatedBy], [UpdatedBy])
        VALUES (@NewProfileId, @FullName, @ExternalCode, @ProfileType, @CreatedBy, @CreatedBy);

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
