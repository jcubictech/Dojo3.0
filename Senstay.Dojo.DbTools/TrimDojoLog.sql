CREATE PROCEDURE [dbo].[TrimDojoLog]
AS
BEGIN
	DELETE FROM [dbo].[DojoLog]
	WHERE [EventDateTime] < DateAdd(Day, -30, Convert(Date, [EventDateTime]))
END
