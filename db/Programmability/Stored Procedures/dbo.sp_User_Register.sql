definition                                                                                                                                                                                                                                                      
CREATE   PROCEDURE [dbo].[sp_User_Register]
    @KeycloakId UNIQUEIDENTIFIER = NULL,
    @Username NVARCHAR(50),
    @Email NVARCHAR(100) = NULL,
    @FullName NVARCHAR(100) = NULL,
    @Role NVARCHAR(20) = 'Staff',
    @CreatedBy NVARCHAR(100) = 'System'
GO