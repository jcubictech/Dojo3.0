CREATE PROCEDURE [dbo].[FinalizePropertyStatement]
	@Month int,
	@Year int,
	@PropertyCode nvarchar(50),
	@EndingBalance float,
	@IsFinalized bit
AS
BEGIN
	
	Declare @CurrentDate DateTime,
			@Date DateTime,
			@NextMonth int,
			@NexYear int,
			@NextBalance float

	SET @CurrentDate = datefromparts(@Year, @Month, 15)
	SET @Date = dateadd(month, 1, @CurrentDate)
	SET @NextMonth = Month(@Date)
	SET @NexYear = Year(@Date)
	SET @NextBalance =  Cast(@EndingBalance as decimal(18,2))

	IF EXISTS (SELECT * FROM [dbo].[PropertyBalance] WHERE [Month] = @NextMonth and [Year] = @NexYear and [PropertyCode] = @PropertyCode)
	BEGIN
		IF (@IsFinalized = 1)
		BEGIN
			UPDATE [dbo].[PropertyBalance]
			SET [BeginningBalance] = @NextBalance, [AdjustedBalance] = @NextBalance
			WHERE [Month] = @NextMonth and [Year] = @NexYear and [PropertyCode] = @PropertyCode
		END
		ELSE
		BEGIN
			DELETE FROM [dbo].[PropertyBalance]
			WHERE [Month] = @NextMonth and [Year] = @NexYear and [PropertyCode] = @PropertyCode
		END
	END
	ELSE IF (@IsFinalized = 1)
	BEGIN
		INSERT INTO [dbo].[PropertyBalance] ([Month], [Year], [PropertyCode], [BeginningBalance], [AdjustedBalance])
		VALUES (@NextMonth, @NexYear, @PropertyCode, @NextBalance, @NextBalance)
	END

	SELECT 'Result' = 1
END
