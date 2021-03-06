ALTER PROCEDURE [dbo].[FinalizePropertyStatement]
	@Month int,
	@Year int,
	@PropertyCode nvarchar(50),
	@EndingBalance float
AS
BEGIN
	
	UPDATE [dbo].[PropertyBalance]
	SET [EndingBalance] = @EndingBalance, [IsFinalized] = 1
	WHERE [Month] = @MOnth and [Year] = @Year and [PropertyCode] = @PropertyCode

	SELECT 'Result' = 1
END
