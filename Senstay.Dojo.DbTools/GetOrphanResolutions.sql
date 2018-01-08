CREATE PROCEDURE [dbo].[GetOrphanResolutions]
	@StartDate datetime,
	@EndDate datetime
AS
BEGIN

	SELECT * INTO #Temp FROM
	(
		-- the confirmation code and property code are both given
		SELECT count(v.[TransactionDate]) as [Count]
			  ,r.[ConfirmationCode]
			  ,r.[PropertyCode]
			  ,v.[TransactionDate]
		  FROM [dbo].[Resolution] r
		  LEFT JOIN [dbo].[Reservation] v on v.[PropertyCode] = r.[PropertyCode] and v.[ConfirmationCode] = r.[ConfirmationCode]
		  WHERE Convert(date, [ResolutionDate]) >= @StartDate and Convert(date, [ResolutionDate]) <= @EndDate and 
				r.[IncludeOnStatement] = 1 and 
				r.[PropertyCode] is not null and r.[PropertyCode] <> '' and 
				r.[ConfirmationCode] is not null and r.[ConfirmationCode] <> ''
		  GROUP BY r.[ConfirmationCode], r.[PropertyCode], v.[TransactionDate]

		UNION

		-- only the confirmation code is given
		SELECT count(v.[PropertyCode]) as [Count]
			  ,r.[ConfirmationCode]
			  ,v.[PropertyCode]
			  ,v.[TransactionDate]
		  FROM [dbo].[Resolution] r
		  LEFT JOIN [dbo].[Reservation] v on v.[ConfirmationCode] = r.[ConfirmationCode]
		  WHERE Convert(date, r.[ResolutionDate]) >= @StartDate and Convert(date, r.[ResolutionDate]) <= @EndDate and 
				r.[IncludeOnStatement] = 1 and r.[Impact] <> 'Advance Payout' and 
				(r.[ConfirmationCode] is not null and r.[ConfirmationCode] <> '') and (r.[PropertyCode] is null or r.[PropertyCode] = '')
		  GROUP BY r.[ConfirmationCode], v.[PropertyCode], v.[TransactionDate]

	) AS Orphans

	-- return those that do not have associated reservations
	SELECT [ConfirmationCode]
		,[PropertyCode]
	FROM #Temp
	WHERE [Count] = 0

END
