CREATE PROCEDURE [dbo].[GetImportStatistics]
	@StartDate DateTime = '2017-07-01', -- start date the import data to be retrieved
	@Account nvarchar(50) = '' -- retrieve all from the @StartDate
AS
BEGIN
	DECLARE @ImportStartDate nvarchar(20) = convert(nvarchar(20), @StartDate, 23) -- yyyy-MM-dd format

	SELECT [Email]
		,'ImportDate' = substring([InputSource], 1, 10)
	INTO #Temp1
	FROM [dbo].[AirbnbAccount] a
	LEFT JOIN [dbo].[OwnerPayout] p ON P.[Source] = a.[Email] and [InputSource] > @ImportStartDate and 
									   [InputSource] not like '%Off-Airbnb%' and [InputSource] not like '%manual%' and [InputSource] <> '' and [IsDeleted] = 0
	WHERE a.[Status] like 'Active%' and (@Account = '' or a.[Email] = @Account)

	SELECT count([ImportDate]) as [Count]
		,[ImportDate]
		,[Account] = [Email]
	INTO #Temp4
	FROM #Temp1
	GROUP BY [Email], [ImportDate]

	SELECT [Email]
		,'ImportDate' = substring([InputSource], 1, 10)
	INTO #Temp2
	FROM [dbo].[AirbnbAccount] a
	LEFT JOIN [dbo].[Reservation] p ON P.[Source] = a.[Email] and [InputSource] > @ImportStartDate and 
									   [InputSource] not like '%Off-Airbnb%' and [InputSource] not like '%manual%' and [InputSource] <> '' and [IsDeleted] = 0
	WHERE a.[Status] like 'Active%' and (@Account = '' or a.[Email] = @Account)

	SELECT count([ImportDate]) as [Count]
		,[ImportDate]
		,[Account] = [Email]
	INTO #Temp5
	FROM #Temp2
	GROUP BY [Email], [ImportDate]

	SELECT [Email]
		,'ImportDate' = substring([InputSource], 1, 10)
	INTO #Temp3
	FROM [dbo].[AirbnbAccount] a
	LEFT JOIN [dbo].[Resolution] p ON P.[InputSource] like '%' + a.[Email] + '%' and [InputSource] <> '' and [InputSource] > @ImportStartDate and [IsDeleted] = 0
	WHERE a.[Status] like 'Active%' and (@Account = '' or a.[Email] = @Account)

	SELECT count([ImportDate]) as [Count]
		,[ImportDate]
		,[Account] = [Email]
	INTO #Temp6
	FROM #Temp3
	GROUP BY [Email], [ImportDate]

	SELECT
		 [Account]
		,[ImportDate] = case when [ImportDate] is null then '2017-07-01' else [ImportDate] end
		,[PayoutCount] = [Count]
		,[ReservationCount] = (select [Count] from #Temp5 t5 where t5.[Account] = t4.[Account] and t5.[ImportDate] = t4.[ImportDate])
		,[ResolutionCount] = (select [Count] from #Temp6 t6 where t6.[Account] = t4.[Account] and t6.[ImportDate] = t4.[ImportDate])
	INTO #Temp
	FROM #Temp4 t4
	ORDER BY [ImportDate] desc

	SELECT
		 [Account]
		,[ImportDate]
		,[MonthDay] = Convert(int, substring([ImportDate], 9, 2))
		,[PayoutCount] = case when [PayoutCount] is null then 0 else [PayoutCount] end
		,[ReservationCount] = case when [ReservationCount] is null then 0 else [ReservationCount] end
		,[ResolutionCount] = case when [ResolutionCount] is null then 0 else [ResolutionCount] end
	FROM #Temp
	ORDER BY [Account], [ImportDate]

END
