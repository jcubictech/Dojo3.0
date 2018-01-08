CREATE PROCEDURE [dbo].[UpdateAllExpenseWorkflowStates]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode nvarchar(50),
	@State int,
	@User nvarchar(50)
AS
BEGIN
	
	DECLARE @ReviewedDate DateTime = null,
			@ReviewedBy nvarchar(50) = null,
			@ApprovedDate DateTime = null,
			@ApprovedBy nvarchar(50) = null,
			@ModifiedDate DateTime = GETDATE()
		
	IF @state = 1
	BEGIN
		SET @ReviewedDate = GETDATE()
		SET @ReviewedBy = @User
		SET @ModifiedDate = @ReviewedDate
		UPDATE [dbo].[Expense] SET [ApprovalStatus] = @State, 
								   [ReviewedDate] = @ReviewedDate, [ReviewedBy] = @ReviewedBy,
								   [ApprovedDate] = @ApprovedDate, [ApprovedBy] = @ApprovedBy,
								   [ModifiedDate] = @ModifiedDate, [ModifiedBy] = @User
		WHERE Convert(date, [ExpenseDate]) >= @StartDate and Convert(date, [ExpenseDate]) <= @EndDate and
			  [PropertyCode] = @PropertyCode and [IsDeleted] = 0
	END
	ELSE IF @state = 2
	BEGIN
		SET @ApprovedDate = GETDATE()
		SET @ApprovedBy = @User
		SET @ModifiedDate = @ApprovedDate
		UPDATE [dbo].[Expense] SET [ApprovalStatus] = @State, 
								   [ApprovedDate] = @ApprovedDate, [ApprovedBy] = @ApprovedBy,
								   [ModifiedDate] = @ModifiedDate, [ModifiedBy] = @User
		WHERE Convert(date, [ExpenseDate]) >= @StartDate and Convert(date, [ExpenseDate]) <= @EndDate and
			  [PropertyCode] = @PropertyCode and [IsDeleted] = 0
	END
	ELSE
	BEGIN
		UPDATE [dbo].[Expense] SET [ApprovalStatus] = @State, 
								   [ReviewedDate] = @ReviewedDate, [ReviewedBy] = @ReviewedBy,
								   [ApprovedDate] = @ApprovedDate, [ApprovedBy] = @ApprovedBy,
								   [ModifiedDate] = @ModifiedDate, [ModifiedBy] = @User
		WHERE Convert(date, [ExpenseDate]) >= @StartDate and Convert(date, [ExpenseDate]) <= @EndDate and
			  [PropertyCode] = @PropertyCode and [IsDeleted] = 0
	END

	SELECT @@ROWCOUNT as 'Count'

END
