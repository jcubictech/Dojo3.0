CREATE PROCEDURE [dbo].[GetResolutionStatement]
	@StartDate datetime,
	@EndDate datetime,
	@PropertyCode nvarchar(50)
AS
BEGIN

	SELECT distinct
		'Type' = r.[ResolutionType]
		,'Guest' = [GuestName]
		,'Arrival' = Convert(date, [CheckinDate])
		,'Departure' = Convert(date, [CheckoutDate])
		,[Nights]
		,'TotalRevenue' = [ResolutionAmount]
		,'ApprovedNote' = case when r.[ApprovedNote] is null then '' else r.[ApprovedNote] end
		,r.[ResolutionDescription]
	FROM [dbo].[Resolution] r
	INNER JOIN [dbo].[Reservation] s on s.[ConfirmationCode] = r.[ConfirmationCode] and s.[PropertyCode] = @PropertyCode and s.[IsDeleted] = 0
	WHERE Convert(date, [ResolutionDate]) >= @StartDate and Convert(date, [ResolutionDate]) <= @EndDate and
		  r.[IncludeOnStatement] = 1 and 
		  r.[IsDeleted] = 0 
	ORDER BY 'Arrival', [Nights], 'Guest', 'Type'
END
