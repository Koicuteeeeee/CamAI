CREATE OR ALTER PROCEDURE [dbo].[sp_User_Register]
    @Username NVARCHAR(50),
    @FullName NVARCHAR(100),
    @EmbeddingFront VARBINARY(MAX),
    @EmbeddingLeft VARBINARY(MAX),
    @EmbeddingRight VARBINARY(MAX),
    @MinioObjectName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NewUserId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Thêm User
        INSERT INTO [dbo].[Users] ([Id], [Username], [FullName], [IsActive])
        VALUES (@NewUserId, @Username, @FullName, 1);

        -- Thêm Khuôn mặt
        INSERT INTO [dbo].[UserFaces] ([UserId], [MinioObjectName], [EmbeddingFront], [EmbeddingLeft], [EmbeddingRight])
        VALUES (@NewUserId, @MinioObjectName, @EmbeddingFront, @EmbeddingLeft, @EmbeddingRight);

        COMMIT TRANSACTION;
        SELECT @NewUserId AS NewUserId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
