definition                                                                                                                                                                                                                                                      
CREATE   PROCEDURE [dbo].[sp_FaceProfile_GetAll]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM [dbo].[FaceProfiles] ORDER BY [FullName];
END
GO