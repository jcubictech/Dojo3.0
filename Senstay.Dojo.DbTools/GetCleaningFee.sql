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
