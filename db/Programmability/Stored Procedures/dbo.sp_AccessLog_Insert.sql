definition                                                                                                                                                                                                                                                      
CREATE   PROCEDURE [dbo].[sp_AccessLog_Insert]
    @ProfileId UNIQUEIDENTIFIER = NULL, -- Cho phAcp NULL cho ng’ø ¯?i l §­
    @FullName NVARCHAR(100) = NULL,
    @RecognitionStatus NVARCHAR(20),
    @Similarity FLOAT = NULL,
    @MinioLogImage NVARCHAR(50
GO