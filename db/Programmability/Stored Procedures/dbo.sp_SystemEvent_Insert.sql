definition                                                                                                                                                                                                                                                      
CREATE   PROCEDURE [dbo].[sp_SystemEvent_Insert]
    @Level NVARCHAR(20),
    @Source NVARCHAR(100),
    @Message NVARCHAR(MAX),
    @Exception NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].
GO