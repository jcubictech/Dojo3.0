CREATE PROCEDURE [dbo].[RetrieveStatementReport]
	@StartDate DateTime,
	@EndDate DateTime,
	@ReportType int
AS
BEGIN
	DECLARE @Year int,
			@Month int,
			@ReportDate DateTime,
			@SeedNumber int

	SET @Year = Year(@EndDate)
	SET @Month = Month(@EndDate)
	SET @ReportDate = DateAdd(hour, 11, Cast(EoMonth(@EndDate) as DateTime))

	-- invoice seed number
	SET @SeedNumber = ((@Year - 2000)  * 100 + @Month) * 1000

	-- get row # for each property
	SELECT [PropertyCode], ROW_NUMBER() OVER (ORDER BY [PropertyCode]) AS PropertyIndex
	INTO #Temp
	FROM [dbo].[CPL]

	-- get report type for each property
	SELECT s.[PropertyCode], p.[Vertical], [Balance]
	INTO #Temp1
	FROM [dbo].[OwnerStatement] s
	INNER JOIN [dbo].[CPL] p on p.[PropertyCode] = s.[PropertyCode] and p.[PropertyStatus] <> 'Dead'
	WHERE s.[Year] = @Year and s.[Month] = @Month and s.[IsSummary] = 0 and s.[StatementStatus] = 2 -- finalized only

	SELECT * INTO #Temp2 FROM
	(
		SELECT s.[PropertyCode]
			   ,[Vertical]
			   ,[ReportItemType] = 1  -- airbnb revenue item including reservations and resolutions
			   ,[CustomerJob] = (select top 1 m.[CustomerJob] from [dbo].[ReportMapping] m where m.[PropertyCode] = s.[PropertyCode])
			   ,[ClassName] = (select top 1 m.[Class] from [dbo].[ReportMapping] m where m.[PropertyCode] = s.[PropertyCode])
			   ,[ReportDate] = @ReportDate
			   ,[InvoiceNumber] =  @SeedNumber + (select [PropertyIndex] from #Temp t where t.[PropertyCode] = s.[PropertyCode])
			   ,[ItemName] = 'Airbnb Contra Account'
			   ,[Quantity] = 1
			   ,[Rate] = (select sum([TotalRevenue]) from [dbo].[Reservation] r 
						  where (CONVERT(date, r.[CheckinDate]) >= @StartDate and CONVERT(date, r.[CheckinDate]) <= @EndDate) and
								  r.[TotalRevenue] <> 0 and r.[IncludeOnStatement] = 1 and r.[IsDeleted] = 0 and
								  r.[PropertyCode] = s.[PropertyCode] and r.[Channel] = 'Airbnb')
			   ,[Amount] = 0
			   ,'Balance' = (select top 1 [Balance] from #Temp1 t where t.[PropertyCode] = s.[PropertyCode] and t.[Vertical] <> 'CO')
		FROM [dbo].[OwnerStatement] s
		INNER JOIN [dbo].[CPL] p on p.[PropertyCode] = s.[PropertyCode]
		WHERE s.[Year] = @Year and s.[Month] = @Month and 
			  s.[TotalRevenue] <> 0 and s.[IsSummary] = 0 and -- property code with non-zero only
			  s.[StatementStatus] = 2 -- finalized only

		UNION

		SELECT s.[PropertyCode]
			   ,[Vertical]
			   ,[ReportItemType] = 2 -- non-airbnb revenue item
			   ,[CustomerJob] = (select top 1 m.[CustomerJob] from [dbo].[ReportMapping] m where m.[PropertyCode] = s.[PropertyCode])
			   ,[ClassName] = (select top 1 m.[Class] from [dbo].[ReportMapping] m where m.[PropertyCode] = s.[PropertyCode])
			   ,[ReportDate] = @ReportDate
			   ,[InvoiceNumber] =  @SeedNumber + (select [PropertyIndex] from #Temp t where t.[PropertyCode] = s.[PropertyCode])
			   ,[ItemName] = ''
			   ,[Quantity] = 1
			   ,[Rate] = (select sum([TotalRevenue]) from [dbo].[Reservation] r 
						  where (CONVERT(date, r.[CheckinDate]) >= @StartDate and CONVERT(date, r.[CheckinDate]) <= @EndDate) and
								  r.[TotalRevenue] <> 0 and r.[IncludeOnStatement] = 1 and r.[IsDeleted] = 0 and
								  r.[PropertyCode] = s.[PropertyCode] and r.[Channel] <> 'Airbnb')
			   ,[Amount] = 0
			   ,'Balance' = (select top 1 [Balance] from #Temp1 t where t.[PropertyCode] = s.[PropertyCode] and t.[Vertical] <> 'CO')
		FROM [dbo].[OwnerStatement] s
		INNER JOIN [dbo].[CPL] p on p.[PropertyCode] = s.[PropertyCode]
		WHERE s.[Year] = @Year and s.[Month] = @Month and 
			  s.[Balance] <> 0 and s.[IsSummary] = 0 and -- property code with non-zero only
			  s.[StatementStatus] = 2 -- finalized only

		UNION

		SELECT  s.[PropertyCode]
			   ,[Vertical]
			   ,[ReportItemType] = 3  -- management fee item
			   ,[CustomerJob] = (select top 1 r.[CustomerJob] from [dbo].[ReportMapping] r where r.[PropertyCode] = s.[PropertyCode])
			   ,[ClassName] = (select top 1 r.[Class] from [dbo].[ReportMapping] r where r.[PropertyCode] = s.[PropertyCode])
			   ,[ReportDate] = @ReportDate
			   ,[InvoiceNumber] =  @SeedNumber + (select [PropertyIndex] from #Temp t where t.[PropertyCode] = s.[PropertyCode])
			   ,[ItemName] = case when p.[Vertical] = 'RS' or p.[Vertical] = 'FS' then p.[Vertical] + ' Management Fee' else '' end
			   ,[Quantity] = 1
			   ,[Rate] = [ManagementFees]
			   ,[Amount] = 0
			   ,'Balance' = (select top 1 [Balance] from #Temp1 t where t.[PropertyCode] = s.[PropertyCode] and t.[Vertical] <> 'CO')
		FROM [dbo].[OwnerStatement] s
		INNER JOIN [dbo].[CPL] p on p.[PropertyCode] = s.[PropertyCode]
		WHERE s.[Year] = @Year and s.[Month] = @Month and 
			  s.[ManagementFees] <> 0 and s.[IsSummary] = 0 and -- property code with non-zero only
			  s.[StatementStatus] = 2 -- finalized only

		UNION

		SELECT e.[PropertyCode]
			   ,[Vertical]
			   ,[ReportItemType] = 4 -- expense item
			   ,[CustomerJob] = (select top 1 r.[CustomerJob] from [dbo].[ReportMapping] r where r.[PropertyCode] = e.[PropertyCode])
			   ,[ClassName] = (select top 1 r.[Class] from [dbo].[ReportMapping] r where r.[PropertyCode] = e.[PropertyCode])
			   ,[ReportDate] = @ReportDate
			   ,[InvoiceNumber] =  @SeedNumber + (select [PropertyIndex] from #Temp t where t.[PropertyCode] = e.[PropertyCode])
			   ,[ItemName] = case when e.[Category] like 'Clean%' and p.[Vertical] = 'RS' then 'Cleaning RS Refund'
								  when e.[Category] like 'Clean%' and p.[Vertical] = 'FS' then 'Cleaning FS Refund'
								  else e.[Category]
							 end
			   ,[Quantity] = 1
			   ,[Rate] = [ExpenseAmount]
			   ,[Amount] = 0
			   ,'Balance' = (select top 1 [Balance] from #Temp1 t where t.[PropertyCode] = e.[PropertyCode] and t.[Vertical] <> 'CO')
		FROM [dbo].[Expense] e
		INNER JOIN [dbo].[CPL] p on p.[PropertyCode] = e.[PropertyCode]
		WHERE (CONVERT(date, e.[ExpenseDate]) >= @StartDate and CONVERT(date, e.[ExpenseDate]) <= @EndDate) and
			  e.[ExpenseId] = e.[ParentId] and e.[IncludeOnStatement] = 1 and e.[IsDeleted] = 0

		UNION

		SELECT s.[PropertyCode]
			   ,[Vertical]
			   ,[ReportItemType] = 5  -- tax collected item
			   ,[CustomerJob] = (select top 1 r.[CustomerJob] from [dbo].[ReportMapping] r where r.[PropertyCode] = s.[PropertyCode])
			   ,[ClassName] = (select top 1 r.[Class] from [dbo].[ReportMapping] r where r.[PropertyCode] = s.[PropertyCode])
			   ,[ReportDate] = @ReportDate
			   ,[InvoiceNumber] =  @SeedNumber + (select [PropertyIndex] from #Temp t where t.[PropertyCode] = s.[PropertyCode])
			   ,[ItemName] = 'Tax Collected'
			   ,[Quantity] = 1
			   ,[Rate] = [TaxCollected]
			   ,[Amount] = 0
			   ,'Balance' = (select top 1 [Balance] from #Temp1 t where t.[PropertyCode] = s.[PropertyCode] and t.[Vertical] <> 'CO')
		FROM [dbo].[OwnerStatement] s
		INNER JOIN [dbo].[CPL] p on p.[PropertyCode] = s.[PropertyCode]
		WHERE s.[Year] = @Year and s.[Month] = @Month and 
			  s.[TaxCollected] <> 0 and s.[IsSummary] = 0 and -- property code with non-zero only
			  s.[StatementStatus] = 2 -- finalized only

	) AS StatementReport

	SELECT [PropertyCode]
		   ,[Vertical]
		   ,[ReportItemType]
		   ,[CustomerJob]
		   ,[ClassName]
		   ,[ReportDate]
		   ,[InvoiceNumber]
		   ,[ItemName]
		   ,[Quantity]
		   ,[Rate] = case when [Rate] is null then 0 else Cast(Cast([Rate] as Decimal(18,2)) as float) end
		   ,[Amount] = case when [Rate] is null then 0 else Cast(Cast(([Rate] * [Quantity]) as Decimal(18,2)) as float) end
	FROM #Temp2
	WHERE ([Rate] is not null and [Rate] <> 0) and  -- display only non-zero item
		  ((@ReportType = 1 and [Vertical] = 'CO') or  -- journal report
		   (@ReportType = 2 and [Vertical] <> 'CO' and [Balance] is not null and [Balance] > 0) or  -- credit memo report
		   (@ReportType = 3 and [Vertical] <> 'CO' and [Balance] is not null and [Balance] < 0)) -- debt report
	ORDER BY [CustomerJob], [PropertyCode], [ReportItemType]

END