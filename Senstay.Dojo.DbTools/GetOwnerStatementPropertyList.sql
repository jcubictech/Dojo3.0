CREATE PROCEDURE [dbo].[GetOwnerStatementPropertyList]
	@StartDate DateTime,
	@EndDate DateTime 
AS
BEGIN

	Declare @Month int, @Year int, @LastMonth int, @LastYear int
	SET @Month = Month(@StartDate)
	SET @Year = Year(@StartDate)
	SET @LastMonth = Month(DateAdd(Month, -1, @StartDate))
	SET @LastYear = Year(DateAdd(Month, -1, @StartDate))

	SELECT [PayoutMethodId]
		,m.[PayoutMethodName]
		,ROW_NUMBER() OVER(PARTITION BY m.[EffectiveDate], m.[PayoutMethodName] ORDER BY m.[EffectiveDate] desc, m.[PayoutMethodName]) 'rank'
	INTO #Temp
	FROM [dbo].[PayoutMethod] m 
	WHERE m.[IsDeleted] = 0 and Convert(Date, m.[EffectiveDate]) <= @EndDate and Convert(Date, m.[ExpiryDate]) >= @StartDate

	SELECT distinct
		p.[PropertyCode]
		,p.[PropertyStatus]
		,p.[Vertical]
		,'OwnerPayout' = t.[PayoutMethodName]
		,'Owner' = (select top 1 [OwnerName] from [dbo].[PropertyAccount] a
					inner join [dbo].[PropertyAccountPayoutMethod] pa on pa.[PayoutMethodId] = ppm.[PayoutMethodId] and pa.[PropertyAccountId] = a.[PropertyAccountId])
		,'Address' = case when p.[Address] is not null then p.[Address] else '' end
		,'Finalized' = case
							when (select Count([OwnerStatementId]) from [dbo].[OwnerStatement] where [PropertyCode] = p.[PropertyCode] and [Month] = @MOnth and [Year] = @Year and [StatementStatus] = 2) > 0
							then 
								1
							else 
								0
					   end
		,'ReservationApproved' = case
									when (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
											where p.[PropertyCode] = r.[PropertyCode] and  r.[ApprovalStatus] < 2 and r.[IsDeleted] = 0 and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) > 0 and
											(select count(r.[PropertyCode]) from [dbo].[Reservation] r 
											where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) > 0
									then
										0
									else
										1
									end
		,'ResolutionApproved' = case
									when (select count(r.[PropertyCode]) from [dbo].[Resolution] r 
											where p.[PropertyCode] = r.[PropertyCode] and  r.[ApprovalStatus] < 2 and r.[IsDeleted] = 0 and Convert(date,r.[ResolutionDate]) >= @StartDate and Convert(date,r.[ResolutionDate]) <= @EndDate) > 0 and
											(select count(r.[PropertyCode]) from [dbo].[Resolution] r 
											where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[ResolutionDate]) >= @StartDate and Convert(date,r.[ResolutionDate]) <= @EndDate) > 0
									then
										0
									else
										1
									end
		,'ExpenseApproved' = case
								when (select count(e.[PropertyCode]) from [dbo].[Expense] e 
										where p.[PropertyCode] = e.[PropertyCode] and  e.[ApprovalStatus] < 2 and e.[IsDeleted] = 0 and e.[ParentId] = e.[ExpenseId] and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0 and
										(select count(e.[PropertyCode]) from [dbo].[Expense] e 
										where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0
								then
									0
								else
									1
								end
		,'OtherRevenueApproved' = case
									when (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
											where p.[PropertyCode] = r.[PropertyCode] and  r.[ApprovalStatus] < 2 and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0 and
											(select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
											where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0
									then 
										0
									else
										1
									end
		,'Empty' = case
						when (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
								where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) = 0 and
							 (select count(s.[PropertyCode]) from [dbo].[Resolution] s 
								where p.[PropertyCode] = s.[PropertyCode] and s.[Impact] = 'Advance Payout' and s.[IsDeleted] = 0 and Convert(date,s.[ResolutionDate]) >= @StartDate and Convert(date, s.[ResolutionDate]) <= @EndDate) = 0 and
							 (select count(e.[PropertyCode]) from [dbo].[Expense] e 
								where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) = 0 and
							 (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
								where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) = 0
						then 
							1
						else 
							0
					end
		,'Balance' = (select Top 1 b.[AdjustedBalance] from [dbo].[PropertyBalance] b 
					  where p.[PropertyCode] = b.[PropertyCode] and b.[Month] = @LastMonth and b.[Year] = @LastYear)
		,'StatementBalance' = (select Top 1 [BeginBalance] from [dbo].[OwnerStatement] s
					  where p.[PropertyCode] = s.[PropertyCode] and s.[Month] = @Month and s.[Year] = @Year)
	INTO #Temp2
	FROM [dbo].[CPL] p
	INNER JOIN [dbo].[PropertyPayoutMethod] ppm on ppm.[PropertyCode] = p.[PropertyCode]
	INNER JOIN #Temp t on t.[PayoutMethodId] = ppm.[PayoutMethodId] and t.[Rank] = 1
	--INNER JOIN [dbo].[PropertyAccountPayoutMethod] pap on pap.[PayoutMethodId] = t.[PayoutMethodId]
	--INNER JOIN [dbo].[PropertyAccount] pa on pa.[PropertyAccountId] = pap.[PropertyAccountId]
	WHERE (p.[PropertyStatus] like 'Active%' or p.[PropertyStatus] = 'Inactive' or p.[PropertyStatus] = 'Pending-Onboarding') and
		   p.[PropertyCode] <> 'PropertyPlaceholder' 

	SELECT [PropertyCode]
		,'PropertyCodeWithPayoutMethod' = case when [Owner] is null then [OwnerPayout] + ' | ' + [PropertyCode] + '-' + [Vertical] + ' | '
											   else [OwnerPayout] + ' | ' + [PropertyCode] + '-' + [Vertical] + ' | ' + [Owner] end
		,[Finalized]
		,[ReservationApproved]
		,[ResolutionApproved]
		,[ExpenseApproved]
		,[OtherRevenueApproved] = 1  -- do not need workflow anymore
		,[Empty] = case when ([Balance] is not null and [Balance] <> 0 and [StatementBalance] is not null and [StatementBalance] <> 0) or ([StatementBalance] is not null and [StatementBalance] <> 0) then 0 else [Empty] end
	FROM #Temp2
	ORDER BY [OwnerPayout], [PropertyCode], [Owner]

END