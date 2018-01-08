CREATE PROCEDURE [dbo].[GetConfirmationCode]
	@Account NVarChar(50)
AS
BEGIN
	SELECT
		'Text' = [ConfirmationCode] + ' | ' + [PropertyCode]
		,'Value' = [ConfirmationCode]
	FROM Reservation r
	WHERE [Source] like @Account + '%'
END
