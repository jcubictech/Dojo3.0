CREATE PROCEDURE [dbo].[GetResolutionTotal]
	@StartDate datetime ,
	@EndDate datetime ,
	@PropertyCode nvarchar(50)
AS
BEGIN

	SELECT 'Amount' = SUM([ResolutionAmount])
	FROM [dbo].[Resolution] r
	INNER JOIN [dbo].[Reservation] v on v.[ConfirmationCode] = r.[ConfirmationCode] and v.[PropertyCode] = @PropertyCode
	WHERE Convert(date, [ResolutionDate]) >= @StartDate and Convert(date, [ResolutionDate]) <= @EndDate and
		  r.[IncludeOnStatement] = 1 and
		  r.[IsDeleted] = 0
END
