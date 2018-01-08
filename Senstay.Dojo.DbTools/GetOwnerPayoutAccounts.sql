CREATE PROCEDURE [dbo].[GetOwnerPayoutAccounts]
	@StartDate DateTime,
	@EndDate DateTime
AS
BEGIN
	SELECT c.[Account]
			,'ReservationCount' = (select count([ReservationId]) from [dbo].[Reservation] r
								   inner join [dbo].[OwnerPayout] p on r.[OwnerPayoutId] = p.[OwnerPayoutId] and p.[Source] = c.[Account]
																	   and (Convert(Date, p.[PayoutDate]) >= Convert(Date, @StartDate) and Convert(Date, p.[PayoutDate]) <= Convert(Date, @EndDate))
								   where r.[IsDeleted] = 0)
			,'ResolutionCount' = (select count([ResolutionId]) from [dbo].[Resolution] r
								   inner join [dbo].[OwnerPayout] p on r.[OwnerPayoutId] = p.[OwnerPayoutId] and p.[Source] = c.[Account]
																	   and (Convert(Date, p.[PayoutDate]) >= Convert(Date, @StartDate) and Convert(Date, p.[PayoutDate]) <= Convert(Date, @EndDate))
								   where r.[IsDeleted] = 0)
	INTO #Temp
	FROM [dbo].[CPL] c
	WHERE c.[Account] is not null and c.[Account] <> 'N/A'

	SELECT distinct 
			[Account]
		   ,'Count' = [ReservationCount] + [ResolutionCount]
	FROM #Temp

	UNION

	SELECT
		'Account' = 'VSP'
		,'Count' = 0

	ORDER BY [Account]
END  
