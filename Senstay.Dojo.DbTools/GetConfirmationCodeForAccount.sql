CREATE PROCEDURE [dbo].[GetConfirmationCodeForAccount]
	@Account Nvarchar(50)
AS
BEGIN

	SELECT 
		'Text' = [ConfirmationCode] + ' | ' + [PropertyCode]
		,'Value' = [ConfirmationCode]
	FROM [dbo].[Reservation]
	WHERE [ReservationId] > 0 and [PropertyCode] <> 'PropertyPlaceholder' and [Source] like @Account + '%'
	ORDER BY [ConfirmationCode]

END
