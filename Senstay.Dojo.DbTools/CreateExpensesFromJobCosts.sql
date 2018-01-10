CREATE PROCEDURE [dbo].[CreateExpensesFromJobCosts]
	@StartDate DateTime,
	@EndDate DateTime,
	@StartJobCostId int = 0 -- exclusive
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
		@IsDeleted int = 0,
		@IncludeOnStatement int = 1,
		@StartExpenseId int = 0

	-- initialize data
	SET @ErrorCount = 0
	SET @InsertCount = 0
 	SET @CreatedDate = getdate()
	SET @ModifiedDate = @CreatedDate

	-- Get the latest ID to start
	SELECT @StartExpenseId = Max([ExpenseId]) FROM [dbo].[Expense]

	-- ===========================================================================================================
	--		CONVERT JOBCOSTs TO EXPENSES
	-- ===========================================================================================================

	DECLARE ConvertCursor CURSOR FOR 
		SELECT [JobCostId], [PropertyCode], [JobCostPayoutTo], [JobCostType], [JobCostDate], [JobCostNumber],
			   [JobCostSource], [JobCostMemo], [JobCostAccount], [JobCostClass], [JobCostAmount], [JobCostBalance]
		FROM [dbo].[JobCost]
		WHERE [PropertyCode] = [OriginalPropertyCode] and [JobCostBillable] = 1 and [JobCostId] > @StartJobCostId and
			  Convert(date, [JobCostDate]) >= @StartDate and Convert(date, [JobCostDate]) <= @EndDate

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
				 [ApprovedBy], [ApprovedDate], [IsDeleted], [IncludeOnStatement],
				 [CreatedDate], [CreatedBy], [ModifiedDate], [ModifiedBy])
			VALUES
				(@JobCostId, @ParentId, @PropertyCode, @ExpenseDate, @Category, @ExpenseAmount,
				 @ConfirmationCode, @ApprovalStatus, @ReservationId, @ReviewedBy, @ReviewedDate,
				 @ApprovedBy, @ApprovedDate, @IsDeleted, @IncludeOnStatement,
				 @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy)

			SET @InsertCount = @InsertCount + 1	

		END TRY

		BEGIN CATCH
			SELECT ERROR_MESSAGE() AS ErrorMessage
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
	WHERE Convert(Date, [ExpenseDate]) >= @StartDate and Convert(Date, [ExpenseDate]) <= @EndDate and [ExpenseId] > @StartExpenseId

	-- ===========================================================================================================
	--		GROUP EXPENSES
	-- ===========================================================================================================
	
	DECLARE 
		@GroupDate DateTime = @EndDate,
		@AccountCode nvarchar(50),
		@GroupCategory nvarchar(100),
		@GroupAmount float,
		@AccountNumber int,
		@PrimaryAccount nvarchar(50),
		@LineCount int = 0

		SET @InsertCount = 0
		SET @ErrorCount = 0
		SET @JobCostId = null
		SET @ParentId = 0
		SET @ExpenseDate = DateAdd(Hour, 11, @EndDate)
		SET @ExpenseAmount = 0
		SET @ConfirmationCode = ''
		SET @ReservationId = 0

	DECLARE GroupCursor CURSOR FOR 
		SELECT distinct [PropertyCode], [Category] FROM [dbo].[Expense] 
		WHERE [PropertyCode] IS NOT NULL AND [Category] IS NOT NULL and [Category] not like 'Rebalance%' and
			  Convert(Date, [CreatedDate]) >= Convert(Date, @GroupDate)	and [ExpenseId] > @StartExpenseId	  
		ORDER BY [PropertyCode], [Category]

	OPEN GroupCursor
		FETCH NEXT FROM GroupCursor INTO @PropertyCode, @Category

	WHILE @@FETCH_STATUS = 0

	BEGIN

		BEGIN TRY

			SET @LineCount = @LineCount + 1

			-- determine group category based on rules provided by Excel file
			Select top 1 @AccountCode = [Value] from [dbo].[SplitString](@Category, '·') order by [Value]
			Select top 1 @GroupCategory = [Value] from [dbo].[SplitString](@Category, '·') order by [Value] desc

			SET @PrimaryAccount = Substring(@AccountCode, 1, 3) + '00' 
			--Select @PrimaryAccount as 'PrimaryAccount', @LineCount as 'Line #'

			SET @AccountNumber = Cast(@PrimaryAccount as int) -- if this is not an integer, raise exception and continue
			SET @AccountNumber = @AccountNumber / 100 
			IF (@AccountNumber = 503)
				SET @GroupCategory = 'Cleaning'
			ELSE IF (@AccountNumber = 504)
				SET @GroupCategory = 'Furnishings'
			ELSE IF (@AccountNumber = 505)
				SET @GroupCategory = 'Consumables'
			ELSE IF (@AccountNumber = 507)
				SET @GroupCategory = 'Laundry'
			ELSE IF (@AccountNumber = 510)
				SET @GroupCategory = 'Move Out Costs'
			ELSE IF (@AccountNumber = 513)
				SET @GroupCategory = 'Postage'
			ELSE IF (@AccountNumber = 514)
				SET @GroupCategory = 'Rent'
			ELSE IF (@AccountNumber = 516)
				SET @GroupCategory = 'Maintenance'
			ELSE IF (@AccountNumber = 518)
				SET @GroupCategory = 'Utilities'
			ELSE IF (@AccountNumber = 519)
				SET @GroupCategory = 'Housewares'
			ELSE
				SET @GroupCategory = 'Other'

			SELECT Top 1 @ExpenseId = [ExpenseId] FROM [dbo].[Expense] 
			WHERE [ExpenseId] = [ParentId] and [PropertyCode] = @PropertyCode and [ApprovedNote] = @PrimaryAccount and Convert(Date, [CreatedDate]) >= Convert(Date, @GroupDate)

			IF (@ExpenseId is NULL)
			BEGIN
				INSERT INTO [dbo].[Expense]
					([JobCostId], [ParentId], [PropertyCode], [ExpenseDate], [Category], [ExpenseAmount], [IncludeOnStatement],
					 [ConfirmationCode], [ApprovalStatus], [ReservationId], [ReviewedBy], [ReviewedDate], [ApprovedNote],
					 [ApprovedBy], [ApprovedDate], [IsDeleted], [CreatedDate], [CreatedBy], [ModifiedDate], [ModifiedBy])
				VALUES
					(@JobCostId, @ParentId, @PropertyCode, @ExpenseDate, @GroupCategory, @ExpenseAmount, 1,
					 @ConfirmationCode, @ApprovalStatus, @ReservationId, @ReviewedBy, @ReviewedDate, @PrimaryAccount,
					 @ApprovedBy, @ApprovedDate, @IsDeleted, @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy)

				-- get inserted key
				SELECT @ExpenseId = IDENT_CURRENT('[dbo].[Expense]')
				UPDATE [dbo].[Expense] SET [ParentId] = [ExpenseId] WHERE [ExpenseId] = @ExpenseId
				SET @InsertCount = @InsertCount + 1
			END

			-- create grouping
			UPDATE [dbo].[Expense] SET [ParentId] = @ExpenseId, [IncludeOnStatement] = 1 
			WHERE [PropertyCode] = @PropertyCode and [Category] LIKE @AccountCode + '%' and Convert(Date, [CreatedDate]) >= Convert(Date, @GroupDate)

			-- update group expense amount
			SELECT @GroupAmount = SUM([ExpenseAmount]) FROM [dbo].[Expense] 
			WHERE [PropertyCode] = @PropertyCode and [ParentId] = @ExpenseId and [ParentId] <> [ExpenseId]
			UPDATE [dbo].[Expense] SET [ExpenseAmount] = @GroupAmount WHERE [ExpenseId] = @ExpenseId
		
			SET @ExpenseId = NULL

		END TRY

		BEGIN CATCH
			SET @ErrorCount = @ErrorCount + 1
			SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage
		END CATCH

		FETCH NEXT FROM GroupCursor INTO @PropertyCode, @Category

	END

	CLOSE GroupCursor 
	DEALLOCATE GroupCursor

	Select 'New Expense Count' = @InsertCount, 'Error Count' = @ErrorCount

END