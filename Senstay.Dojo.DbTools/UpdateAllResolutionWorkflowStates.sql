CREATE PROCEDURE [dbo].[UpdateAllResolutionWorkflowStates]
	@StartDate DateTime,
	@EndDate DateTime,
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
		UPDATE [dbo].[Resolution] SET [ApprovalStatus] = @State, 
									  [ReviewedDate] = @ReviewedDate, [ReviewedBy] = @ReviewedBy,
									  [ApprovedDate] = @ApprovedDate, [ApprovedBy] = @ApprovedBy,
									  [ModifiedDate] = @ModifiedDate, [ModifiedBy] = @User
		WHERE Convert(date, [ResolutionDate]) >= @StartDate and Convert(date, [ResolutionDate]) <= @EndDate and
			  [IsDeleted] = 0
	END
	ELSE IF @state = 2
	BEGIN
		SET @ApprovedDate = GETDATE()
		SET @ApprovedBy = @User
		SET @ModifiedDate = @ApprovedDate
		UPDATE [dbo].[Resolution] SET [ApprovalStatus] = @State, 
									  --[ReviewedDate] = @ReviewedDate, [ReviewedBy] = @ReviewedBy,
									  [ApprovedDate] = @ApprovedDate, [ApprovedBy] = @ApprovedBy,
									  [ModifiedDate] = @ModifiedDate, [ModifiedBy] = @User
		WHERE Convert(date, [ResolutionDate]) >= @StartDate and Convert(date, [ResolutionDate]) <= @EndDate and
			  [IsDeleted] = 0
	END
	ELSE
	BEGIN
		UPDATE [dbo].[Resolution] SET [ApprovalStatus] = @State, 
									  [ReviewedDate] = @ReviewedDate, [ReviewedBy] = @ReviewedBy,
									  [ApprovedDate] = @ApprovedDate, [ApprovedBy] = @ApprovedBy,
									  [ModifiedDate] = @ModifiedDate, [ModifiedBy] = @User
		WHERE Convert(date, [ResolutionDate]) >= @StartDate and Convert(date, [ResolutionDate]) <= @EndDate and
			  [IsDeleted] = 0
	END

	SELECT @@ROWCOUNT as 'Count'

END
