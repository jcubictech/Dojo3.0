CREATE PROCEDURE [dbo].[RetrieveOtherExpenses]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT [OtherExpenseId]
      ,[PropertyCode]
      ,[OtherExpenseDate]
      ,[OtherExpenseAmount]
      ,[OtherExpenseDescription]
	  ,[IncludeOnStatement]
	  ,[ApprovedNote]
      ,[ApprovalStatus]
	  ,'Reviewed' = case when [ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
	  ,'Approved' = case when [ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
	FROM [dbo].[OtherExpense]
	WHERE (CONVERT(date, [OtherExpenseDate]) >= @StartDate and CONVERT(date, [OtherExpenseDate]) <= @EndDate) and
		   ([PropertyCode] = @PropertyCode or @PropertyCode = '') and 
		   [IsDeleted] = 0
	ORDER BY [PropertyCode], [OtherExpenseDate]
END 