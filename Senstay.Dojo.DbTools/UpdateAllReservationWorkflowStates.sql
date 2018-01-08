CREATE PROCEDURE [dbo].[UpdateAllReservationWorkflowStates]
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
		UPDATE [dbo].[Reservation] SET [ApprovalStatus] = @State, 
								   [ReviewedDate] = @ReviewedDate, [ReviewedBy] = @ReviewedBy,
								   [ApprovedDate] = @ApprovedDate, [ApprovedBy] = @ApprovedBy,
								   [ModifiedDate] = @ModifiedDate, [ModifiedBy] = @User
		WHERE (([Channel] = 'Airbnb' and Convert(date, [TransactionDate]) >= @StartDate and Convert(date, [TransactionDate]) <= @EndDate) or 
			   ([Channel] <> 'Airbnb' and Convert(date, [CheckinDate]) >= @StartDate and Convert(date, [CheckinDate]) <= @EndDate)) and
			  [PropertyCode] = @PropertyCode and [IsDeleted] = 0
	END
	ELSE IF @state = 2
	BEGIN
		SET @ApprovedDate = GETDATE()
		SET @ApprovedBy = @User
		SET @ModifiedDate = @ApprovedDate
		UPDATE [dbo].[Reservation] SET [ApprovalStatus] = @State, 
								   [ApprovedDate] = @ApprovedDate, [ApprovedBy] = @ApprovedBy,
								   [ModifiedDate] = @ModifiedDate, [ModifiedBy] = @User
		WHERE (([Channel] = 'Airbnb' and Convert(date, [TransactionDate]) >= @StartDate and Convert(date, [TransactionDate]) <= @EndDate) or 
			   ([Channel] <> 'Airbnb' and Convert(date, [CheckinDate]) >= @StartDate and Convert(date, [CheckinDate]) <= @EndDate)) and
			  [PropertyCode] = @PropertyCode and [IsDeleted] = 0
	END
	ELSE
	BEGIN
		UPDATE [dbo].[Reservation] SET [ApprovalStatus] = @State, 
								   [ReviewedDate] = @ReviewedDate, [ReviewedBy] = @ReviewedBy,
								   [ApprovedDate] = @ApprovedDate, [ApprovedBy] = @ApprovedBy,
								   [ModifiedDate] = @ModifiedDate, [ModifiedBy] = @User
		WHERE (([Channel] = 'Airbnb' and Convert(date, [TransactionDate]) >= @StartDate and Convert(date, [TransactionDate]) <= @EndDate) or 
			   ([Channel] <> 'Airbnb' and Convert(date, [CheckinDate]) >= @StartDate and Convert(date, [CheckinDate]) <= @EndDate)) and
			  [PropertyCode] = @PropertyCode and [IsDeleted] = 0
	END

	SELECT @@ROWCOUNT as 'Count'

END
