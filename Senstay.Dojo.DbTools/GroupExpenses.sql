CREATE PROCEDURE [dbo].[GroupExpenses]
	@GroupDate DateTime -- use the last day of the expense month
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
		@AccountNumber int,
		@PrimaryAccount nvarchar(50),
		@LineCount int = 0

	-- initialize data
	SET @ErrorCount = 0
	SET @InsertCount = 0
	SET @ModifiedDate = @CreatedDate

	DECLARE GroupCursor CURSOR FOR 
		SELECT distinct [PropertyCode], [Category] FROM [dbo].[Expense] 
		WHERE [PropertyCode] IS NOT NULL AND [Category] IS NOT NULL and Convert(Date, [CreatedDate]) >= Convert(Date, @GroupDate)
		ORDER BY [PropertyCode], [Category]

	OPEN GroupCursor
		FETCH NEXT FROM GroupCursor INTO @PropertyCode, @Category

	WHILE @@FETCH_STATUS = 0

	BEGIN

		BEGIN TRY

			SET @LineCount = @LineCount+ 1

			-- determine group category based on rules provided by Excel file
			Select top 1 @AccountCode = [Value] from [dbo].[SplitString](@Category, '·') order by [Value]
			Select top 1 @GroupCategory = [Value] from [dbo].[SplitString](@Category, '·') order by [Value] desc

			SET @PrimaryAccount = Substring(@AccountCode, 1, 3) + '00' 
			--Select @PrimaryAccount as 'PrimaryAccount', @LineCount as 'Line #'

			SET @AccountNumber = Cast(@PrimaryAccount as int)
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

			-- show error and exit
			--SELECT   
			--	ERROR_NUMBER() AS ErrorNumber  
			--   ,ERROR_MESSAGE() AS ErrorMessage
			--Return
		END CATCH

		FETCH NEXT FROM GroupCursor INTO @PropertyCode, @Category

	END

	CLOSE GroupCursor 
	DEALLOCATE GroupCursor

	SELECT 
		'Converted Count' = Convert(NVARCHAR(10), @InsertCount),
		'Conversion Error Count' = Convert(NVARCHAR(10), @ErrorCount)
END


