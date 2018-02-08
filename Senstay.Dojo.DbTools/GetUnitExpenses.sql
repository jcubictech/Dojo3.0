CREATE PROCEDURE [dbo].[GetUnitExpenses]
	@StartDate datetime,
	@EndDate datetime,
	@PropertyCode nvarchar(50),
	@FixedCostCategory nvarchar(50) = ''
AS
BEGIN

	IF (@FixedCostCategory = '')
	BEGIN
		SELECT * INTO #Temp FROM
		(
			SELECT 
				 'Category' = Replace(Replace(Replace([Category], 'COMBO:', ''), 'ROOM:', ''), 'MULTI:', '')
				,'Amount' = [ExpenseAmount]
			FROM [dbo].[Expense]
			WHERE Convert(date, [ExpenseDate]) >= @StartDate and Convert(date, [ExpenseDate]) <= @EndDate and
				  [PropertyCode] = @PropertyCode and
				  [ExpenseId] = [ParentId] and
				  [Category] not like '%Cleaning%' and
				  [IncludeOnStatement] = 1 and
				  [IsDeleted] = 0

			UNION

			SELECT 
				 'Category' = [OtherRevenueDescription]
				,'Amount' = [OtherRevenueAmount]
			FROM [dbo].[OtherRevenue]
			WHERE Convert(date, [OtherRevenueDate]) >= @StartDate and Convert(date, [OtherRevenueDate]) <= @EndDate and
				  [PropertyCode] = @PropertyCode and
				  [IncludeOnStatement] = 1 and
				  [IsDeleted] = 0

		) AS UnitExpense

		SELECT * from #Temp ORDER BY [Amount]
	END
	ELSE
	BEGIN
		SELECT * INTO #Temp1 FROM
		(
			SELECT Distinct
				'Category' = Replace(Replace(Replace([Category], 'COMBO:', ''), 'ROOM:', ''), 'MULTI:', '')
				,'Amount' = [ExpenseAmount]
			FROM [dbo].[Expense]
			WHERE Convert(date, [ExpenseDate]) >= @StartDate and Convert(date, [ExpenseDate]) <= @EndDate and
					[PropertyCode] = @PropertyCode and
					[ExpenseId] = [ParentId] and
					([Category] not like '%Maint%' and [ApprovedNote] not like '516%') and 
					[IncludeOnStatement] = 1 and
					[IsDeleted] = 0

			UNION

			SELECT Distinct
				'Category' = @FixedCostCategory
				,'Amount' = [ExpenseAmount]
			FROM [dbo].[Expense]
			WHERE Convert(date, [ExpenseDate]) >= @StartDate and Convert(date, [ExpenseDate]) <= @EndDate and
					[PropertyCode] = @PropertyCode and
					[ExpenseId] = [ParentId] and
					([Category] like '%Maint%' or [ApprovedNote] like '516%') and
					[IncludeOnStatement] = 1 and
					[IsDeleted] = 0
		) as UnitExpense1

		SELECT * from #Temp1 ORDER BY [Amount]
	END
END
