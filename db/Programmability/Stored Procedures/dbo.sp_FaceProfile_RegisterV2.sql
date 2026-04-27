definition                                                                                                                                                                                                                                                      
CREATE   PROCEDURE [dbo].[sp_FaceProfile_RegisterV2]
    @FullName NVARCHAR(100),
    @ExternalCode NVARCHAR(50) = NULL,
    @ProfileType NVARCHAR(20) = 'Resident',
    @CreatedBy NVARCHAR(100) = 'System'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NewProfil
GO