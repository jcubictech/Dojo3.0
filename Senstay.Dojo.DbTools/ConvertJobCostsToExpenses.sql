CREATE PROCEDURE [dbo].[ConvertJobCostsToExpenses]
	@StartDate DateTime,
	@EndDate DateTime
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
		@IncludeOnStatement int = 1

	-- initialize data
	SET @ErrorCount = 0
	SET @InsertCount = 0
 	SET @CreatedDate = Convert(date, getdate())
	SET @ModifiedDate = @CreatedDate

	DECLARE ConvertCursor CURSOR FOR 
		SELECT [JobCostId], [PropertyCode], [JobCostPayoutTo], [JobCostType], [JobCostDate], [JobCostNumber],
			   [JobCostSource], [JobCostMemo], [JobCostAccount], [JobCostClass], [JobCostAmount], [JobCostBalance]
		FROM [dbo].[JobCost]
		WHERE [PropertyCode] = [OriginalPropertyCode] and
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
	WHERE Convert(Date, [ExpenseDate]) >= @StartDate and Convert(Date, [ExpenseDate]) <= @EndDate

	SELECT 
		'Converted Count' = Convert(NVARCHAR(10), @InsertCount),
		'Conversion Error Count' = Convert(NVARCHAR(10), @ErrorCount)
END
