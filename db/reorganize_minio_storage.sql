/* 
   MIGRATION: Organize MinIO storage for Faces and Logs
   - Add MinioFront, MinioLeft, MinioRight to UserFaces
   - Update sp_User_Register to support 3 images
*/

-- 1. Cap nhat bang UserFaces
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[UserFaces]') AND name = 'MinioFront')
BEGIN
    ALTER TABLE [dbo].[UserFaces] ADD [MinioFront] NVARCHAR(500) NULL;
    ALTER TABLE [dbo].[UserFaces] ADD [MinioLeft] NVARCHAR(500) NULL;
    ALTER TABLE [dbo].[UserFaces] ADD [MinioRight] NVARCHAR(500) NULL;
END
GO

-- 2. Cap nhat Stored Procedure dang ky
CREATE OR ALTER PROCEDURE [dbo].[sp_User_Register]
    @Username NVARCHAR(50),
    @FullName NVARCHAR(100),
    @EmbeddingFront VARBINARY(MAX),
    @EmbeddingLeft VARBINARY(MAX),
    @EmbeddingRight VARBINARY(MAX),
    @MinioFront NVARCHAR(500),
    @MinioLeft NVARCHAR(500),
    @MinioRight NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NewUserId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Them User
        INSERT INTO [dbo].[Users] ([Id], [Username], [FullName], [IsActive])
        VALUES (@NewUserId, @Username, @FullName, 1);

        -- Them Khuon mat voi 3 goc anh
        INSERT INTO [dbo].[UserFaces] ([UserId], [MinioFront], [MinioLeft], [MinioRight], [EmbeddingFront], [EmbeddingLeft], [EmbeddingRight])
        VALUES (@NewUserId, @MinioFront, @MinioLeft, @MinioRight, @EmbeddingFront, @EmbeddingLeft, @EmbeddingRight);

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
