CREATE PROCEDURE [dbo].[ConvertJobCostsToExpenses]
AS
BEGIN
	DECLARE 
		@InsertCount int,
		@ErrorCount int,
		@JobCostId int,
		@PropertyCode nvarchar(50),
		@JobCostPayoutTo nvarchar(100),
		@JobCostType nvarchar(100),
		@JobCostDate datetime,
		@JobCostNumber nvarchar(50),
		@JobCostSource nvarchar(100),
		@JobCostMemo nvarchar(100),
		@JobCostAccount nvarchar(100),
		@JobCostClass nvarchar(100),
		@JobCostAmount float,
		@JobCostBalance float,
		@ExpenseId int,
		@ParentId int = 0,
		@ExpenseDate datetime,
		@Category nvarchar(100),
		@ExpenseAmount float,
		@ConfirmationCode  nvarchar(100) = '',
		@ReservationId int = 0,
		@CreatedDate datetime,
		@CreatedBy  nvarchar(100) = 'system',
		@ModifiedDate datetime,
		@ModifiedBy  nvarchar(100) = 'system',
		@ApprovalStatus int = 0,
		@ReviewedBy nvarchar(100) = null,
		@ReviewedDate datetime = null,
		@ApprovedBy nvarchar(100) = null,
		@ApprovedDate datetime = null,
		@IsDeleted int = 0

	-- initialize data
	SET @ErrorCount = 0
	SET @InsertCount = 0
 	SET @CreatedDate = Convert(date, getdate())
	SET @ModifiedDate = @CreatedDate

	DECLARE ConvertCursor CURSOR FOR 
		SELECT [JobCostId], [PropertyCode], [JobCostPayoutTo], [JobCostType], [JobCostDate], [JobCostNumber],
			   [JobCostSource], [JobCostMemo], [JobCostAccount], [JobCostClass], [JobCostAmount], [JobCostBalance]
		FROM [dbo].[JobCost]
		WHERE [PropertyCode] = [OriginalPropertyCode]

	OPEN ConvertCursor
		FETCH NEXT FROM ConvertCursor INTO
			@JobCostId, @PropertyCode, @JobCostPayoutTo, @JobCostType, @JobCostDate, @JobCostNumber, 
			@JobCostSource, @JobCostMemo, @JobCostAccount, @JobCostClass, @JobCostAmount, @JobCostBalance

	WHILE @@FETCH_STATUS = 0
	BEGIN

		BEGIN TRY

			SET @Category = @JobCostAccount
			SET @ExpenseDate = @JobCostDate
			SET @ExpenseAmount = @JobCostAmount

			INSERT INTO [dbo].[Expense]
				([JobCostId], [ParentId], [PropertyCode], [ExpenseDate], [Category], [ExpenseAmount],
				 [ConfirmationCode], [ApprovalStatus], [ReservationId], [ReviewedBy], [ReviewedDate],
				 [ApprovedBy], [ApprovedDate], [IsDeleted], [CreatedDate], [CreatedBy], [ModifiedDate], [ModifiedBy])
			VALUES
				(@JobCostId, @ParentId, @PropertyCode, @ExpenseDate, @Category, @ExpenseAmount,
				 @ConfirmationCode, @ApprovalStatus, @ReservationId, @ReviewedBy, @ReviewedDate,
				 @ApprovedBy, @ApprovedDate, @IsDeleted, @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy)

			SET @InsertCount = @InsertCount + 1	

		END TRY

		BEGIN CATCH
			SET @ErrorCount = @ErrorCount + 1
		END CATCH

		FETCH NEXT FROM ConvertCursor INTO
			@JobCostId, @PropertyCode, @JobCostPayoutTo, @JobCostType, @JobCostDate, @JobCostNumber, 
			@JobCostSource, @JobCostMemo, @JobCostAccount, @JobCostClass, @JobCostAmount, @JobCostBalance

	END

	CLOSE ConvertCursor 
	DEALLOCATE ConvertCursor

	-- set ParentId to ExpenseId to have no grouping for expenses
	UPDATE [dbo].[Expense] SET [ParentId] = [ExpenseId]

	SELECT 
		'Converted Count' = Convert(NVARCHAR(10), @InsertCount),
		'Conversion Error Count' = Convert(NVARCHAR(10), @ErrorCount)
END
GO

CREATE PROCEDURE [dbo].[GetAdvancePaymentStatement]
	@StartDate datetime ,
	@EndDate datetime ,
	@PropertyCode nvarchar(50)
AS
BEGIN

	SELECT
		'Guest' = r.[GuestName]
		,'PaymentDate' = r.[TransactionDate]
		,'Amount' = r.[TotalRevenue]
	FROM [dbo].[OwnerPayout] p
	INNER JOIN [dbo].[Reservation] r ON r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[PropertyCode] = @PropertyCode and r.[includeOnStatement] = 1 and r.[IsDeleted] = 0
	WHERE Convert(date, [PayoutDate]) >= @StartDate and Convert(date, [PayoutDate]) <= @EndDate and
		  p.[AccountNumber] not like '%9146' and 
		  p.[IsDeleted] = 0
	ORDER BY 'PaymentDate'
END
GO

CREATE PROCEDURE [dbo].[GetCleaningFee]
	@StartDate datetime ,
	@EndDate datetime ,
	@PropertyCode nvarchar(50)
AS
BEGIN

	SELECT 'Amount' = SUM([ExpenseAmount]) * 1.1
	FROM [dbo].[Expense]
	WHERE Convert(date, [ExpenseDate]) >= @StartDate and Convert(date, [ExpenseDate]) <= @EndDate and
		  [PropertyCode] = @PropertyCode and
		  [ExpenseId] = [ParentId] and
		  [Category] like '%Cleaning%' and
		  [IncludeOnStatement] = 1 and
		  [IsDeleted] = 0
END
GO

CREATE PROCEDURE [dbo].[GetOwnerPayoutDiscrepancy]
	@OwnerPayoutId int
AS
BEGIN

	SELECT * INTO #Temp FROM
	(
		SELECT 
			'Discrepancy' = [PayoutAmount]  - sum(r.[TotalRevenue])
		FROM [dbo].[OwnerPayout] p
		INNER JOIN [dbo].[Reservation] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		WHERE p.[OwnerPayoutId] = @OwnerPayoutId
		GROUP BY p.[OwnerPayoutId], [PayoutAmount]

		UNION

		SELECT 
			'Discrepancy' = [PayoutAmount]  - sum(r.[ResolutionAmount])
		FROM [dbo].[OwnerPayout] p
		INNER JOIN [dbo].[Resolution] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		WHERE p.[OwnerPayoutId] = @OwnerPayoutId
		GROUP BY p.[OwnerPayoutId], [PayoutAmount]
	) As Payout

	SELECT [Discrepancy] FROM #Temp

END
GO

CREATE PROCEDURE [dbo].[GetPayoutMethodWithStatus]
	@Year int,
	@Month int 
AS
BEGIN

	SELECT distinct
		 'PayoutMethod' = p.[OwnerPayout]
		,'Finalized' = case
							when (select count(h.[PayoutMethod]) from [dbo].[PayoutHistory] h
								  where p.[OwnerPayout] = h.[PayoutMethod] and h.[IsFinalized] = 1 and h.[Year] = @Year and h.[Month] = @Month) > 0
							then
								1
							else
								0
						end
		,'Empty' = case
						when (select count(h.[PayoutMethod]) from [dbo].[PayoutHistory] h
								where p.[OwnerPayout] = h.[PayoutMethod] and h.[IsFinalized] = 1 and h.[Year] = @Year and h.[Month] = @Month) = 0
						then
							1
						else
							0
					end
	FROM [dbo].[CPL] p
	WHERE (p.[PropertyStatus] = 'Active' or p.[PropertyStatus] = 'Inactive' or p.[PropertyStatus] = 'Pending-Onboarding') and
			p.[OwnerPayout] is not null and p.[OwnerPayout] <> ''
	ORDER BY p.[OwnerPayout]

END
GO

CREATE PROCEDURE [dbo].[GetProeprtyCodeWithAddress]
	@StartDate DateTime,
	@EndDate DateTime 
AS
BEGIN

	SELECT distinct
		p.[PropertyCode]
		,'PropertyCodeWithAddress' = p.[PropertyCode] + '-' + p.[Vertical] + ' | ' +  p.[Address]
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
								(select count(e.[PropertyCode]) from [dbo].[Expense] e 
								where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) = 0 and
								(select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
								where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) = 0 
						then 
							1
						else 
							0
					end
	FROM [dbo].[CPL] p
	WHERE (p.[PropertyStatus] = 'Active' or p.[PropertyStatus] = 'Inactive' or p.[PropertyStatus] = 'Pending-Onboarding') and
			p.[PropertyCode] <> 'PropertyPlaceholder' and p.[Address] is not null
	ORDER BY [PropertyCode]

END
GO

CREATE PROCEDURE [dbo].[GetProeprtyCodeWithStatus]
	@TableName nvarchar(50),
	@StartDate DateTime = '2016-01-01',
	@EndDate DateTime = '2050-12-31'
AS
BEGIN

	IF @TableName = 'Reservation'

	BEGIN
		SELECT distinct
			p.[PropertyCode]
			,'AllReviewed' = case
								when (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[ApprovalStatus] < 1 and r.[IsDeleted] = 0 and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) > 0
								then 0
							 else
								1
							 end
			,'AllApproved' = case
								when (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and  r.[ApprovalStatus] < 2 and r.[IsDeleted] = 0 and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) > 0
								then 0
							 else
								1
							 end
			,'Finalized' = case
								when (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[ApprovalStatus] < 3 and r.[IsDeleted] = 0 and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) > 0
								then 0
							 else
								1
							 end
			,'Empty' = case
							when (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
								  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) = 0 
							then 
								1
							else 
								0
					   end
		FROM [dbo].[CPL] p
		WHERE (p.[PropertyStatus] = 'Active' or p.[PropertyStatus] = 'Inactive' or p.[PropertyStatus] = 'Pending-Onboarding') and
			  p.[PropertyCode] <> 'PropertyPlaceholder'
		ORDER BY [PropertyCode]
	END

	ELSE IF (@TableName = 'Expense')
	BEGIN
		SELECT distinct
			p.[PropertyCode]
			,'AllReviewed' = case
								when (select count(e.[PropertyCode]) from [dbo].[Expense] e 
									  where p.[PropertyCode] = e.[PropertyCode] and e.[ApprovalStatus] < 1 and e.[IsDeleted] = 0 and e.[ParentId] = e.[ExpenseId] and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0 and
									 (select count(e.[PropertyCode]) from [dbo].[Expense] e
									  where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0
								then 
									0
								else
									1
							 end
			,'AllApproved' = case
								when (select count(e.[PropertyCode]) from [dbo].[Expense] e 
									  where p.[PropertyCode] = e.[PropertyCode] and  e.[ApprovalStatus] < 2 and e.[IsDeleted] = 0 and e.[ParentId] = e.[ExpenseId] and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0 and
									 (select count(e.[PropertyCode]) from [dbo].[Expense] e 
									  where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0
								then
									0
								else
									1
							 end
			,'Finalized' = case
								when (select count(e.[PropertyCode]) from [dbo].[Expense] e 
									  where p.[PropertyCode] = e.[PropertyCode] and e.[ApprovalStatus] < 3 and e.[IsDeleted] = 0 and e.[ParentId] = e.[ExpenseId] and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0 and
									 (select count(e.[PropertyCode]) from [dbo].[Expense] e 
									  where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0
								then 
									0
								else
									1
						   end
			,'Empty' = case
							when (select count(e.[PropertyCode]) from [dbo].[Expense] e 
								  where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) = 0 
							then 
								1
							else 
								0
					   end
		FROM [dbo].[CPL] p
		WHERE (p.[PropertyStatus] = 'Active' or p.[PropertyStatus] = 'Inactive' or p.[PropertyStatus] = 'Pending-Onboarding') and
				p.[PropertyCode] <> 'PropertyPlaceholder'
		ORDER BY [PropertyCode]
	END

	ELSE IF (@TableName = 'OtherRevenue')
	BEGIN
		SELECT distinct
			p.[PropertyCode]
			,'AllReviewed' = case
								when (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[ApprovalStatus] < 1 and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0
								then 
									0
								else
									1
							 end
			,'AllApproved' = case
								when (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and  r.[ApprovalStatus] < 2 and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0
								then 
									0
								else
									1
							 end
			,'Finalized' = case
								when (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[ApprovalStatus] < 3 and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0
								then 
									0
								else
									1
						   end
			,'Empty' = case
							when (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
								  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) = 0 
							then 
								1
							else 
								0
					   end
		FROM [dbo].[CPL] p
		WHERE (p.[PropertyStatus] = 'Active' or p.[PropertyStatus] = 'Inactive' or p.[PropertyStatus] = 'Pending-Onboarding') and
				p.[PropertyCode] <> 'PropertyPlaceholder'
		ORDER BY [PropertyCode]
	END

END
GO

CREATE PROCEDURE [dbo].[GetPropertiesForOwnerSummary]
	@PaymentMethod nvarchar(50)
AS
BEGIN

	SELECT 
		 [PropertyCode]
		,'OwnerName' = [Owner]
		,[Vertical]
		,[Address]
	FROM [dbo].[CPL]
	WHERE [OwnerPayout] = @PaymentMethod and 
		  ([PropertyStatus] = 'Active' or [PropertyStatus] = 'Incactive' or [PropertyStatus] = 'Pending-Onboarding')
	ORDER BY [PropertyCode]
END
GO

CREATE PROCEDURE [dbo].[GetReservationStatement]
	@StartDate datetime ,
	@EndDate datetime ,
	@PropertyCode nvarchar(50)
AS
BEGIN

	SELECT
		'Type' = case when r.[Channel] like 'Owner' then 'Owner' else 'Reservation' end
		,'Guest' = [GuestName]
		,'Arrival' = Convert(date, [CheckinDate])
		,'Departure' = Convert(date, [CheckoutDate])
		,[Nights]
		,[TotalRevenue]
		,'ApprovedNote' = case when [ApprovedNote] is null then '' else [ApprovedNote] end
		,r.[Channel]
		,'TaxRate' = f.[CityTax]
	FROM [dbo].[Reservation] r
	LEFT JOIN [dbo].[PropertyFee] f on f.[PropertyCode] = r.[PropertyCode]
	WHERE ((r.[Source] like 'Off-Airbnb' and Convert(date, [CheckinDate]) >= @StartDate and Convert(date, [CheckinDate]) <= @EndDate) or
		   (r.[Source] not like 'Off-Airbnb' and Convert(date, [TransactionDate]) >= @StartDate and Convert(date, [TransactionDate]) <= @EndDate)) and  		  
		  r.[PropertyCode] = @PropertyCode and 
		  [IsDeleted] = 0 and 
		  [IncludeOnStatement] = 1
	ORDER BY [CheckinDate]
END
GO

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
	FROM [dbo].[Resolution] r
	INNER JOIN [dbo].[Reservation] s on s.[ConfirmationCode] = r.[ConfirmationCode] and s.[PropertyCode] = @PropertyCode
	WHERE Convert(date, [ResolutionDate]) >= @StartDate and Convert(date, [ResolutionDate]) <= @EndDate and
		  r.[IncludeOnStatement] = 1 and 
		  r.[IsDeleted] = 0 
	ORDER BY 'Arrival'
END
GO

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
GO

CREATE PROCEDURE [dbo].[GetUnitExpenses]
	@StartDate datetime ,
	@EndDate datetime ,
	@PropertyCode nvarchar(50)
AS
BEGIN

	SELECT * INTO #Temp FROM
	(
		SELECT 
			 [Category]
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
GO

CREATE PROCEDURE [dbo].[GroupExpenses]
	@GroupDate DateTime,
	@MonthName nvarchar(20) -- name of the month
AS
BEGIN
	DECLARE 
		@InsertCount int,
		@ErrorCount int,

		@PropertyCode nvarchar(50),
		@ExpenseId int,
		@JobCostId int = null,
		@ParentId int = 0,
		@ExpenseDate datetime = @GroupDate,
		@Category nvarchar(100),
		@ExpenseAmount float = 0,
		@ConfirmationCode  nvarchar(100) = '',
		@ReservationId int = 0,
		@CreatedDate datetime = Convert(date, getdate()),
		@CreatedBy  nvarchar(100) = 'system',
		@ModifiedDate datetime,
		@ModifiedBy  nvarchar(100) = 'system',
		@ApprovalStatus int = 0,
		@ReviewedBy nvarchar(100) = null,
		@ReviewedDate datetime = null,
		@ApprovedBy nvarchar(100) = null,
		@ApprovedDate datetime = null,
		@IsDeleted int = 0,
		@AccountCode nvarchar(50),
		@GroupCategory nvarchar(100),
		@GroupAmount float,
		@AccountNumber int

	-- initialize data
	SET @ErrorCount = 0
	SET @InsertCount = 0
	SET @ModifiedDate = @CreatedDate

	DECLARE GroupCursor CURSOR FOR 
		SELECT distinct [PropertyCode], [Category] FROM [dbo].[Expense] 
		WHERE [PropertyCode] IS NOT NULL AND [Category] IS NOT NULL 
		ORDER BY [PropertyCode], [Category]

	OPEN GroupCursor
		FETCH NEXT FROM GroupCursor INTO @PropertyCode, @Category

	WHILE @@FETCH_STATUS = 0

	BEGIN

		BEGIN TRY

			-- determine group category based on rules provided by Excel file
			Select top 1 @AccountCode = [Value] from [dbo].[SplitString](@Category, '·') order by [Value]
			Select top 1 @GroupCategory = [Value] from [dbo].[SplitString](@Category, '·') order by [Value] desc

			SET @AccountNumber = Substring(@AccountCode, 1, 3) + '00'
			IF (@AccountNumber = '50300')
				SET @GroupCategory = 'Cleaning'
			ELSE IF (@AccountNumber = '50400')
				SET @GroupCategory = 'Furnishings'
			ELSE IF (@AccountNumber = '50500')
				SET @GroupCategory = 'Consumables'
			ELSE IF (@AccountNumber = '50700')
				SET @GroupCategory = 'Laundry'
			ELSE IF (@AccountNumber = '51000')
				SET @GroupCategory = 'Move Out Costs'
			ELSE IF (@AccountNumber = '51300')
				SET @GroupCategory = 'Postage'
			ELSE IF (@AccountNumber = '51400')
				SET @GroupCategory = 'Rent'
			ELSE IF (@AccountNumber = '51600')
				SET @GroupCategory = 'Maintenance'
			ELSE IF (@AccountNumber = '51800')
				SET @GroupCategory = 'Utilities'

			--SET @GroupCategory = @MonthName + ' - ' + @GroupCategory

			INSERT INTO [dbo].[Expense]
				([JobCostId], [ParentId], [PropertyCode], [ExpenseDate], [Category], [ExpenseAmount], [IncludeOnStatement],
				 [ConfirmationCode], [ApprovalStatus], [ReservationId], [ReviewedBy], [ReviewedDate], [ApprovedNote],
				 [ApprovedBy], [ApprovedDate], [IsDeleted], [CreatedDate], [CreatedBy], [ModifiedDate], [ModifiedBy])
			VALUES
				(@JobCostId, @ParentId, @PropertyCode, @ExpenseDate, @GroupCategory, @ExpenseAmount, 1,
				 @ConfirmationCode, @ApprovalStatus, @ReservationId, @ReviewedBy, @ReviewedDate, '',
				 @ApprovedBy, @ApprovedDate, @IsDeleted, @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy)

			-- get inserted key
			SELECT @ExpenseId = IDENT_CURRENT('[dbo].[Expense]')

			-- create grouping
			UPDATE [dbo].[Expense] SET [ParentId] = @ExpenseId, [IncludeOnStatement] = 1 WHERE [PropertyCode] = @PropertyCode and [Category] LIKE @AccountCode + '%'
			UPDATE [dbo].[Expense] SET [ParentId] = [ExpenseId] WHERE [ExpenseId] = @ExpenseId

			-- update group expense amount
			SELECT @GroupAmount = SUM([ExpenseAmount]) FROM [dbo].[Expense] 
			WHERE [PropertyCode] = @PropertyCode and [ParentId] = @ExpenseId
			UPDATE [dbo].[Expense] SET [ExpenseAmount] = @GroupAmount WHERE [ExpenseId] = @ExpenseId

			SET @InsertCount = @InsertCount + 1	

		END TRY

		BEGIN CATCH
			SET @ErrorCount = @ErrorCount + 1
		END CATCH

		FETCH NEXT FROM GroupCursor INTO @PropertyCode, @Category

	END

	CLOSE GroupCursor 
	DEALLOCATE GroupCursor

	SELECT 
		'Converted Count' = Convert(NVARCHAR(10), @InsertCount),
		'Conversion Error Count' = Convert(NVARCHAR(10), @ErrorCount)
END
GO

CREATE PROCEDURE [dbo].[InitOwnerPayout]
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM [dbo].[OwnerPayout] -- will do cascade deletion for all related tables

    DBCC CHECKIDENT ('[dbo].[OwnerPayout]', RESEED, 0)
	DBCC CHECKIDENT ('[dbo].[Reservation]', RESEED, 0)
	DBCC CHECKIDENT ('[dbo].[Resolution]', RESEED, 0)

	DECLARE @PropertyPlaceholder varchar(50) = 'PropertyPlaceholder'

	DELETE FROM [dbo].[InputError]

	IF NOT EXISTS (SELECT 1 FROM [dbo].[CPL] WHERE [PropertyCode] = @PropertyPlaceholder)
		INSERT INTO [dbo].[CPL] ([PropertyCode], [PropertyStatus]) VALUES (@PropertyPlaceholder, 'Dead')

	SET IDENTITY_INSERT [dbo].[OwnerPayout] ON

	IF NOT EXISTS (SELECT 1 FROM [dbo].[OwnerPayout] WHERE [OwnerPayoutId] = 0)
		INSERT INTO [dbo].[OwnerPayout]		
				   ([OwnerPayoutId]
				   ,[PayoutAmount]
				   ,[PayoutDate]
				   ,[IsAmountMatched]
				   ,[DiscrepancyAmount]
				   ,[CreatedDate]
				   ,[CreatedBy]
				   ,[ModifiedDate]
				   ,[ModifiedBy]
				   ,[Source]
				   ,[AccountNumber]
				   ,[InputSource])
			 VALUES
				   (0
				   ,0
				   ,'2017-01-01'
				   ,Cast(0 as bit)
				   ,0
				   ,'2017-01-01'
				   ,''
				   ,'2017-01-01'
				   ,''
				   ,'Airbnb'
				   ,''
				   ,'')

	SET IDENTITY_INSERT [dbo].[OwnerPayout] OFF

	SET IDENTITY_INSERT [dbo].[Reservation] ON

	IF NOT EXISTS (SELECT 1 FROM [dbo].[Reservation] WHERE [ReservationId] = 0)
		INSERT INTO [dbo].[Reservation]
				   ([ReservationId]
				   ,[OwnerPayoutId]
				   ,[PropertyCode]
				   ,[ConfirmationCode]
				   ,[TransactionDate]
				   ,[CheckinDate]
				   ,[Nights]
				   ,[GuestName]
				   ,[Reference]
				   ,[Currency]
				   ,[TotalRevenue]
				   --,[HostFee]
				   --,[CleanFee]
				   ,[Source]
				   ,[CreatedDate]
				   ,[CreatedBy]
				   ,[ModifiedDate]
				   ,[ModifiedBy]
				   ,[InputSource])
			 VALUES
				   (0
				   ,0
				   ,'PropertyPlaceholder'
				   ,''
				   ,'2017-01-01'
				   ,'2017-01-01'
				   ,1
				   ,''
				   ,''
				   ,1
				   ,0
				   ,''
				   ,'2017-01-01'
				   ,''
				   ,'2017-01-01'
				   ,''
				   ,'')

	SET IDENTITY_INSERT [dbo].[Reservation] OFF

	SET IDENTITY_INSERT [dbo].[Expense] ON;
	insert into [dbo].[Expense] ([ExpenseId], [ReservationId],[ExpenseAmount], [CreatedDate], [ModifiedDate], [ApprovalStatus], [IsDeleted], [ParentId])
				  values(0, 0, 0, getdate(), getdate(), 0, 1, 0)
	SET IDENTITY_INSERT [dbo].[Expense] OFF;

END
GO

CREATE PROCEDURE [dbo].[RetrieveCombinedExpensesRevenue]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT * INTO #Temp FROM
	(
		SELECT [ExpenseId] = 0
			  ,[ParentId] = 0
			  ,'TreeNode' = '<div class="expense-tree-node" data-id="0" data-parentid="0" id="expense-tree-id-0">Expense Home</div>'
	
		UNION

		SELECT [ExpenseId]
			  ,[ParentId]
			  ,'TreeNode' = '<div class="expense-tree-node" data-id="' + Convert(NVARCHAR(10), [ExpenseId]) + 
							'" data-parentid="' + Convert(NVARCHAR(10), [ParentId]) + 
							'" id="expense-tree-id-' + Convert(NVARCHAR(10), [ExpenseId]) + 
							'">' + Convert(NVARCHAR(16), [ExpenseDate], 102) + 
							'---' + [Category] + 
							'---$' + Convert(NVARCHAR(20), [ExpenseAmount]) + 
							'---' + [PropertyCode]
		FROM [dbo].[Expense] e
		WHERE (CONVERT(date, e.[ExpenseDate]) >= @StartDate and CONVERT(date, e.[ExpenseDate]) <= @EndDate) and
			  (e.[PropertyCode] = @PropertyCode or @PropertyCode = '') and e.[IsDeleted] = 0
	) AS ExpenseTree

	SELECT * FROM #Temp ORDER BY [ParentId], [ExpenseId] desc -- must be sorted in this order
END
GO

CREATE PROCEDURE [dbo].[RetrieveExpensesRevenue]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT e.[ExpenseId]
		   ,e.[ParentId]
		   ,e.[ExpenseDate]
		   ,e.[ConfirmationCode]
		   ,e.[Category]
		   ,e.[ExpenseAmount]
		   ,e.[PropertyCode]
		   ,e.[ReservationId]
		   ,e.[IncludeOnStatement]
		   ,e.[ApprovedNote]
		   ,e.[ApprovalStatus]
		   ,'Reviewed' = case when e.[ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		   ,'Approved' = case when e.[ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
	FROM [dbo].[Expense] e
	WHERE (CONVERT(date, e.[ExpenseDate]) >= @StartDate and CONVERT(date, e.[ExpenseDate]) <= @EndDate) and
		  (e.[PropertyCode] = @PropertyCode or @PropertyCode = '') and 
		  e.[IsDeleted] = 0
	ORDER BY e.[ParentId], e.[ExpenseId] desc
END
GO

CREATE PROCEDURE [dbo].[RetrieveOtherRevenueFromTable]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT [OtherRevenueId]
      ,[PropertyCode]
      ,[OtherRevenueDate]
      ,[OtherRevenueAmount]
      ,[OtherRevenueDescription]
      ,[IsDeleted]
      ,[ApprovalStatus]
      ,[ReviewedBy]
      ,[ReviewedDate]
      ,[ApprovedBy]
      ,[ApprovedDate]
      ,[CreatedDate]
      ,[CreatedBy]
      ,[ModifiedDate]
      ,[ModifiedBy]
	FROM [dbo].[OtherRevenue]
	WHERE (CONVERT(date, [OtherRevenueDate]) >= @StartDate and CONVERT(date, [OtherRevenueDate]) <= @EndDate) and
		   ([PropertyCode] = @PropertyCode or @PropertyCode = '') and [IsDeleted] = 0
	ORDER BY [PropertyCode], [OtherRevenueDate]
END
GO

CREATE PROCEDURE [dbo].[RetrieveOtherRevenues]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT [OtherRevenueId]
      ,[PropertyCode]
      ,[OtherRevenueDate]
      ,[OtherRevenueAmount]
      ,[OtherRevenueDescription]
	  ,[IncludeOnStatement]
	  ,[ApprovedNote]
      ,[ApprovalStatus]
	  ,'Reviewed' = case when [ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
	  ,'Approved' = case when [ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
	FROM [dbo].[OtherRevenue]
	WHERE (CONVERT(date, [OtherRevenueDate]) >= @StartDate and CONVERT(date, [OtherRevenueDate]) <= @EndDate) and
		   ([PropertyCode] = @PropertyCode or @PropertyCode = '') and [IsDeleted] = 0
	ORDER BY [PropertyCode], [OtherRevenueDate]
END
GO

CREATE PROCEDURE [dbo].[RetrieveOwnerPayoutRevenue]
	@StartDate DateTime,
	@EndDate DateTime,
	@Source NVARCHAR(50)
AS
BEGIN
	SELECT * INTO #Temp FROM
	(	
		SELECT p.[OwnerPayoutId]
			  ,p.[Source]
			  ,[PayoutDate]
			  ,'PayToAccount' = [AccountNumber]
			  ,[PayoutAmount]
			  ,[IsAmountMatched]
			  ,p.[DiscrepancyAmount]
			  ,p.[InputSource]
			  ,'ReservationTotal' = (select sum([TotalRevenue]) from [dbo].[Reservation] rr
									where rr.[OwnerPayoutId] = p.[OwnerPayoutId] and rr.[IsDeleted] = 0)
			  ,'ResolutionTotal' = (select sum([ResolutionAmount]) from [dbo].[Resolution] rr
									where rr.[OwnerPayoutId] = p.[OwnerPayoutId] and rr.[IsDeleted] = 0)
			  ,'RevenueType' = 'Reservation'
			  ,r.[ConfirmationCode]
			  ,r.[CheckinDate]
			  ,r.[Nights]
			  ,'PropertyCode' = case when r.[PropertyCode] = 'PropertyPlaceholder' or r.[PropertyCode] is null  then '' else r.[PropertyCode] end
			  ,'Amount' = r.[TotalRevenue]
			  ,'ChildId' = r.[ReservationId]
		FROM [dbo].[OwnerPayout] p
		LEFT JOIN [dbo].[Reservation] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		where (Convert(Date, [PayoutDate]) >= Convert(Date, @StartDate) and Convert(Date, [PayoutDate]) <= Convert(Date, @EndDate)) and
				p.[OwnerPayoutId] > 0 and p.[IsDeleted] = 0 and
				(p.[Source] = @Source or (@source = '' and p.[IsAmountMatched] = 0))

		UNION

		SELECT p.[OwnerPayoutId]
			  ,p.[Source]
			  ,[PayoutDate]
			  ,'PayToAccount' = [AccountNumber]
			  ,[PayoutAmount]
			  ,[IsAmountMatched]
			  ,p.[DiscrepancyAmount]
			  ,p.[InputSource]
			  ,'ReservationTotal' = (select sum([TotalRevenue]) from [dbo].[Reservation] rr
									where rr.[OwnerPayoutId] = p.[OwnerPayoutId])
			  ,'ResolutionTotal' = (select sum([ResolutionAmount]) from [dbo].[Resolution] rr
									where rr.[OwnerPayoutId] = p.[OwnerPayoutId])
			  ,'RevenueType' = r.[ResolutionType]
			  ,'ConfirmationCode' = v.[ConfirmationCode]
			  ,v.[CheckinDate]
			  ,v.[Nights]
			  ,'PropertCode' =case when v.[PropertyCode] = 'PropertyPlaceholder' or v.[PropertyCode] is null then '' else v.[PropertyCode] end
			  ,'Amount' = r.[ResolutionAmount]
			  ,'ChildId' = r.[ResolutionId]
		FROM [dbo].[OwnerPayout] p
		INNER JOIN [dbo].[Resolution] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		LEFT JOIN [dbo].[Reservation] v on v.[ConfirmationCode] = r.[ConfirmationCode] and v.[ReservationId] > 0 and v.[IsDeleted] = 0
		where (Convert(Date, [PayoutDate]) >= Convert(Date, @StartDate) and Convert(Date, [PayoutDate]) <= Convert(Date, @EndDate)) and
				p.[OwnerPayoutId] > 0 and p.[IsDeleted] = 0 and 
				(p.[Source] = @Source or (@source = '' and p.[IsAmountMatched] = 0))
	) AS OwnerPayoutRevenue

	SELECT * from #Temp
	ORDER BY [PayoutDate], [PropertyCode]
END  
GO

CREATE PROCEDURE [dbo].[RetrieveOwnerPayoutRevenueById]
	@OwnerPayoutId int
AS
BEGIN
	SELECT * INTO #Temp FROM
	(	
		SELECT 
			  p.[Source]
			  ,[PayoutDate]
			  ,'PayToAccount' = [AccountNumber]
			  ,[PayoutAmount]
			  ,[IsAmountMatched]
			  ,'RevenueType' = 'Reservation'
			  ,r.[ConfirmationCode]
			  ,r.[CheckinDate]
			  ,'PropertyCode' = case when r.[PropertyCode] = 'PropertyPlaceholder' or r.[PropertyCode] is null  then '' else r.[PropertyCode] end
			  ,'Amount' = r.[TotalRevenue]
			  ,p.[DiscrepancyAmount]
			  ,p.[InputSource]
			  ,p.[OwnerPayoutId]
			  ,'ChildId' = r.[ReservationId]
		FROM [dbo].[OwnerPayout] p
		LEFT JOIN [dbo].[Reservation] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		where p.[OwnerPayoutId] = @OwnerPayoutId and p.[IsDeleted] = 0

		UNION

		SELECT 
			  p.[Source]
			  ,[PayoutDate]
			  ,'PayToAccount' = [AccountNumber]
			  ,[PayoutAmount]
			  ,[IsAmountMatched]
			  ,'RevenueType' = r.[ResolutionType]
			  ,'ConfirmationCode' = v.[ConfirmationCode]
			  ,v.[CheckinDate]
			  ,'PropertCode' =case when v.[PropertyCode] = 'PropertyPlaceholder' or v.[PropertyCode] is null then '' else v.[PropertyCode] end
			  ,'Amount' = r.[ResolutionAmount]
			  ,p.[DiscrepancyAmount]
			  ,p.[InputSource]
			  ,p.[OwnerPayoutId]
			  ,'ChildId' = r.[ResolutionId]
		FROM [dbo].[OwnerPayout] p
		INNER JOIN [dbo].[Resolution] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		LEFT JOIN [dbo].[Reservation] v on v.[ConfirmationCode] = r.[ConfirmationCode] and v.[ReservationId] > 0 and v.[IsDeleted] = 0
		where p.[OwnerPayoutId] = @OwnerPayoutId and p.[IsDeleted] = 0
	) AS OwnerPayoutRevenue

	SELECT * from #Temp
	ORDER BY [PayoutDate], [PropertyCode]
END  
GO

CREATE PROCEDURE [dbo].[RetrieveOwnerPayouts]
	@StartDate DateTime,
	@EndDate DateTime
AS
BEGIN
	SELECT [OwnerPayoutId]
      ,[PayoutDate]
      ,[Source]
      ,[AccountNumber]
      ,[PayoutAmount]
      ,[DiscrepancyAmount]
      ,[IsAmountMatched]
      ,[InputSource]
      ,[CreatedDate]
      ,[CreatedBy]
      ,[ModifiedDate]
      ,[ModifiedBy]
	  ,[IsDeleted]
	FROM [dbo].[OwnerPayout] p
	WHERE (CONVERT(date, p.[PayoutDate]) >= @StartDate and CONVERT(date, p.[PayoutDate]) <= @EndDate) and
		  [IsDeleted] = 0
	ORDER BY [PayoutDate] desc, [PayoutAmount] desc
  
END
GO

CREATE PROCEDURE [dbo].[RetrieveReservationRevenue]
	@StartDate DATETIME,
	@EndDate DATETIME,
	@PropertyCode NVARCHAR(50)
AS
BEGIN
	SELECT r.[OwnerPayoutId]
		,[ReservationId]
		,r.[PropertyCode]
		,'PayoutDate' = [TransactionDate]
		,[ConfirmationCode]
		,[Channel]
		,[TotalRevenue]
		,[CheckinDate]
		,[Nights]
		,[GuestName]
		,r.[IncludeOnStatement] --'IncludeOnStatement' = case when r.[IncludeOnStatement] = 0 then 0 else 1 end
		,r.[IsFutureBooking] -- 'IsFutureBooking' = case when r.[OwnerPayoutId] = 0 then Convert(bit, 0) else Convert(bit, 1) end
		,[ApprovalStatus]
		,r.[ApprovedNote]
		,'Reviewed' = case when [ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Approved' = case when [ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Finalized' = case when [ApprovalStatus] < 3 then Convert(bit, 0) else Convert(bit, 1) end
		,r.[InputSource]			
	FROM [dbo].[Reservation] r
	INNER JOIN [dbo].[CPL] c on c.[PropertyCode] = r.[PropertyCode] and c.[PropertyCode] = @PropertyCode
	LEFT JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId]
	WHERE ((r.[Source] like 'Off-Airbnb' and Convert(date, [CheckinDate]) >= Convert(date, @StartDate) and Convert(date, [CheckinDate]) <= Convert(date, @EndDate)) or
		   (r.[Source] not like 'Off-Airbnb' and Convert(date, [TransactionDate]) >= Convert(date, @StartDate) and Convert(date, [TransactionDate]) <= Convert(date, @EndDate)) or
		   r.[TransactionDate] is null) and
		   [ReservationId] > 0 and r.[InputSource] not like '%_pending' and r.[IsDeleted] = 0
	ORDER BY [TransactionDate]
END
GO

CREATE PROCEDURE [dbo].[RetrieveReservationRevenueById]
	@ReservationId int
AS
BEGIN
	SELECT Top 1 
		 r.[OwnerPayoutId]
		,[ReservationId]
		,r.[PropertyCode]
		,'PayoutDate' = [TransactionDate]
		,[ConfirmationCode]
		,[Channel]
		,[TotalRevenue]
		,[CheckinDate]
		,[Nights]
		,[GuestName]
		,r.[IncludeOnStatement] --'IncludeOnStatement' = case when r.[IncludeOnStatement] = 0 then 0 else 1 end
		,r.[IsFutureBooking] -- 'IsFutureBooking' = case when r.[OwnerPayoutId] = 0 then Convert(bit, 0) else Convert(bit, 1) end
		,[ApprovalStatus]
		,'Reviewed' = case when [ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Approved' = case when [ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Finalized' = case when [ApprovalStatus] < 3 then Convert(bit, 0) else Convert(bit, 1) end				
		,r.[InputSource]			
	FROM [dbo].[Reservation] r
	LEFT JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId]
	WHERE [ReservationId] = @ReservationId and r.[InputSource] not like '%_pending' and r.[IsDeleted] = 0
END
GO

CREATE PROCEDURE [dbo].[RetrieveReservations]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT [ReservationId]
      ,[OwnerPayoutId] 
      ,[PropertyCode]
      ,[ConfirmationCode]
      ,[TransactionDate]
	  ,[TotalRevenue]
      ,[Channel]
      ,[CheckinDate]
      ,[CheckoutDate]
      ,[Nights]
      ,[GuestName]
	  ,[IncludeOnStatement]
      ,[IsFutureBooking]
      ,[ApprovalStatus]
      ,[Source]
	  ,[Reference]
      ,[Currency]
      ,[CreatedDate]
      ,[CreatedBy]
      ,[ModifiedDate]
      ,[ModifiedBy]
      ,[InputSource]
      ,[LocalTax]
      ,[DamageWaiver]
      ,[AdminFee]
      ,[PlatformFee]
      ,[TaxRate]
      ,[ListingTitle]
      ,[ReviewedBy]
      ,[ReviewedDate]
      ,[ApprovedBy]
      ,[ApprovedDate]
	  ,[ApprovedNote]
	  ,[IsDeleted]
	FROM [dbo].[Reservation]
	WHERE ((CONVERT(date, [TransactionDate]) >= @StartDate and CONVERT(date, [TransactionDate]) <= @EndDate) or [TransactionDate] is null) and
		  ([PropertyCode] = @PropertyCode or @PropertyCode = '') and [InputSource] not like '%_pending' and
		  [IsDeleted] = 0
	ORDER BY [TransactionDate] desc
END
GO

CREATE PROCEDURE [dbo].[RetrieveResolutionRevenue]
	@StartDate DATETIME,
	@EndDate DATETIME,
	@PropertyCode NVARCHAR(50)
AS
BEGIN
	SELECT distinct r.[ResolutionId]
		,r.[OwnerPayoutId]
		--,v.[PropertyCode]
		,r.[ResolutionDate]
		,r.[ConfirmationCode]
		,r.[ResolutionType]
		,r.[ResolutionDescription]
		,r.[Impact]
		,r.[ResolutionAmount]
		,p.[Source]
		,r.[IncludeOnStatement]
		,r.[ApprovedNote]
		,r.[ApprovalStatus]
		,'Reviewed' = case when r.[ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Approved' = case when r.[ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end				
	FROM [dbo].[Resolution] r
	LEFT JOIN [dbo].[Reservation] v on v.[ConfirmationCode] = r.[ConfirmationCode] and v.[PropertyCode] <> 'PropertyPlaceholder'
	INNER JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId]
	WHERE ((CONVERT(date, [ResolutionDate]) >= CONVERT(date, @StartDate) and CONVERT(date, [ResolutionDate]) <= CONVERT(date, @EndDate)) or [ResolutionDate] is null) and
		   [ResolutionId] > 0 and (r.[InputSource] not like '%_pending' or r.[InputSource] is null) and 
		   --[PropetyCode] <> 'PropertyPlaceholder'
		   r.[IsDeleted] = 0
	ORDER BY [ResolutionDate], [ResolutionType]
END
GO

CREATE PROCEDURE [dbo].[RetrieveResolutions]
	@StartDate DateTime,
	@EndDate DateTime
AS
BEGIN
	SELECT [ResolutionId]
      ,[OwnerPayoutId]
	  ,[ConfirmationCode]
      ,[ResolutionDescription]
      ,[ResolutionAmount]
      ,[ResolutionDate]
      ,[CreatedDate]
      ,[CreatedBy]
      ,[ModifiedDate]
      ,[ModifiedBy]
      ,[InputSource]
      ,[ResolutionType]
      ,[Impact]
      ,[ApprovalStatus]
	  ,[IsDeleted]
  FROM [dbo].[Resolution]
	WHERE (CONVERT(date, [ResolutionDate]) >= @StartDate and CONVERT(date, [ResolutionDate]) <= @EndDate) or
			[ResolutionDate] is null and [InputSource] not like '%_pending' and [IsDeleted] = 0
	ORDER BY [ResolutionDate] desc
END
GO

CREATE PROCEDURE [dbo].[RetrieveRevenueReservation]
	@ReservationId int
AS
BEGIN
	SELECT Top 1 
		 r.[OwnerPayoutId]
		,[ReservationId]
		,r.[PropertyCode]
		,'PayoutDate' = [TransactionDate]
		,[ConfirmationCode]
		,[Channel]
		,[TotalRevenue]
		,[CheckinDate]
		,[Nights]
		,[GuestName]
		,r.[IncludeOnStatement] --'IncludeOnStatement' = case when r.[IncludeOnStatement] = 0 then 0 else 1 end
		,r.[IsFutureBooking] -- 'IsFutureBooking' = case when r.[OwnerPayoutId] = 0 then Convert(bit, 0) else Convert(bit, 1) end
		,[ApprovalStatus]
		,'Reviewed' = case when [ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Approved' = case when [ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Finalized' = case when [ApprovalStatus] < 3 then Convert(bit, 0) else Convert(bit, 1) end				
		,r.[InputSource]			
	FROM [dbo].[Reservation] r
	LEFT JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId]
	WHERE [ReservationId] = @ReservationId and r.[InputSource] not like '%_pending' and r.[IsDeleted] = 0
END
GO

CREATE PROCEDURE [dbo].[RetrieveRevenueReservationView]
	@StartDate DATETIME,
	@EndDate DATETIME,
	@PropertyCode NVARCHAR(50)
AS
BEGIN
	SELECT r.[OwnerPayoutId]
		,[ReservationId]
		,r.[PropertyCode]
		,'PayoutDate' = [TransactionDate]
		,[ConfirmationCode]
		,[Channel]
		,[TotalRevenue]
		,[CheckinDate]
		,[Nights]
		,[GuestName]
		,r.[IncludeOnStatement] --'IncludeOnStatement' = case when r.[IncludeOnStatement] = 0 then 0 else 1 end
		,r.[IsFutureBooking] -- 'IsFutureBooking' = case when r.[OwnerPayoutId] = 0 then Convert(bit, 0) else Convert(bit, 1) end
		,[ApprovalStatus]
		,'Reviewed' = case when [ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Approved' = case when [ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Finalized' = case when [ApprovalStatus] < 3 then Convert(bit, 0) else Convert(bit, 1) end				
		,r.[InputSource]			
	FROM [dbo].[Reservation] r
	INNER JOIN [dbo].[CPL] c on c.[PropertyCode] = r.[PropertyCode] and c.[PropertyCode] = @PropertyCode
	LEFT JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId]
	WHERE ((CONVERT(date, [TransactionDate]) >= CONVERT(date, @StartDate) and CONVERT(date, [TransactionDate]) <= CONVERT(date, @EndDate)) or [TransactionDate] is null) and
		   [ReservationId] > 0 and r.[InputSource] not like '%_pending' and r.[IsDeleted] = 0
	ORDER BY [TransactionDate]
END
GO

CREATE PROCEDURE [dbo].[UpdateOwnerPayoutMatchStatus]
	@OwnerPayoutId int
AS
BEGIN
	
	DECLARE @Discrepancy float

	SELECT * INTO #Temp FROM
	(
		SELECT 
			'Delta' = [PayoutAmount]  - sum(r.[TotalRevenue])
		FROM [dbo].[OwnerPayout] p
		INNER JOIN [dbo].[Reservation] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		WHERE p.[OwnerPayoutId] = @OwnerPayoutId
		GROUP BY p.[OwnerPayoutId], [PayoutAmount]

		UNION

		SELECT 
			'Delta' = [PayoutAmount]  - sum(r.[ResolutionAmount])
		FROM [dbo].[OwnerPayout] p
		INNER JOIN [dbo].[Resolution] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		WHERE p.[OwnerPayoutId] = @OwnerPayoutId
		GROUP BY p.[OwnerPayoutId], [PayoutAmount]
	) As Payout

	SELECT @Discrepancy = sum([Delta]) FROM #Temp

	IF @Discrepancy <> 0
	BEGIN
		UPDATE [dbo].[OwnerPayout] SET [IsAmountMatched] = 0, [DiscrepancyAmount] = @Discrepancy WHERE [OwnerPayoutId] = @OwnerPayoutId
	END
	ELSE
	BEGIN
		UPDATE [dbo].[OwnerPayout] SET [IsAmountMatched] = 1, [DiscrepancyAmount] = 0 WHERE [OwnerPayoutId] = @OwnerPayoutId
	END

	SELECT @Discrepancy as 'Discrepancy'

END
GO

CREATE PROCEDURE [dbo].[RetrieveCombinedExpenses]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT [ExpenseId]
		  ,[ReservationId]
		  ,[Category]
		  ,[ExpenseAmount]
		  ,[CreatedDate]
		  ,[CreatedBy]
		  ,[ModifiedDate]
		  ,[ModifiedBy]
		  ,[ApprovalStatus]
		  ,[ReviewedBy]
		  ,[ReviewedDate]
		  ,[ApprovedBy]
		  ,[ApprovedDate]
		  ,[IsDeleted]
		  ,[PropertyCode]
		  ,[ExpenseDate]
		  ,[ParentId]
		  ,[ConfirmationCode]
		  ,[JobCostId]
		  ,[IncludeOnStatement]
		  ,[ApprovedNote]
	FROM [dbo].[Expense]
	WHERE (CONVERT(date, [ExpenseDate]) >= Convert(date, @StartDate) and CONVERT(date, [ExpenseDate]) <= Convert(date, @EndDate)) and
		  ([PropertyCode] = @PropertyCode or @PropertyCode = '') and 
		   [ExpenseId] = [ParentId] and
		   [IsDeleted] = 0
	ORDER BY [ExpenseId] desc
END
GO

CREATE PROCEDURE [dbo].[FinalizePropertyStatement]
	@Month int,
	@Year int,
	@PropertyCode nvarchar(50),
	@EndingBalance float,
	@IsFinalized bit
AS
BEGIN
	
	IF EXISTS (SELECT * FROM [dbo].[PropertyBalance] WHERE [MOnth] = @Month and [Year] = @Year and [PropertyCode] = @PropertyCode)
	BEGIN
		UPDATE [dbo].[PropertyBalance]
		SET [EndingBalance] = Cast(@EndingBalance as decimal(18,2)), [IsFinalized] = @IsFinalized
		WHERE [Month] = @MOnth and [Year] = @Year and [PropertyCode] = @PropertyCode
	END

	ELSE
	BEGIN
		DECLARE @Date DateTime,
				@BeginningBalance float = 0,
				@LastEndingBalance float

		-- set beginning balance to 0 if pevious month ending balance >= 0
		SET @date = DateAdd(month, -1, DateFromParts(@Year, @Month, 1))
		SELECT TOP 1 @LastEndingBalance = [EndingBalance] FROM [dbo].[PropertyBalance] WHERE [MOnth] = DatePart(Month, @Date) and [Year] = DatePart(Year, @Date) and [PropertyCode] = @PropertyCode
		IF (@LastEndingBalance IS NULL) --try to get it from outstanding balance field in property table
		BEGIN
			SELECT TOP 1 @LastEndingBalance = [OutstandingBalance] FROM [dbo].[CPL] WHERE [PropertyCode] = @PropertyCode
		END

		IF (@LastEndingBalance IS NULL OR @LastEndingBalance >= 0)
			SET @BeginningBalance = 0
		ELSE
			SET @BeginningBalance = @LastEndingBalance

		INSERT INTO [dbo].[PropertyBalance] ([Month], [Year], [PropertyCode], [BeginningBalance], [EndingBalance], [IsFinalized])
		VALUES (@Month, @Year, @PropertyCode, Cast(@BeginningBalance as decimal(18,2)), Cast(@EndingBalance as decimal(18,2)), @IsFinalized)
	END

	SELECT 'Result' = 1
END
GO
