definition                                                                                                                                                                                                                                                      
CREATE   PROCEDURE [dbo].[sp_CameraEvent_Insert]
    @CameraId UNIQUEIDENTIFIER = NULL,
    @CameraName NVARCHAR(100) = NULL,
    @EventType NVARCHAR(50),
    @Description NVARCHAR(MAX),
    @CreatedBy NVARCHAR(100) = 'Camera S
GO