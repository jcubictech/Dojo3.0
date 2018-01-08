CREATE PROCEDURE [dbo].[RetrieveExpensesRevenue]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT e.[ExpenseId]
		   ,e.[ParentId]
		   ,e.[ExpenseDate]
		   ,e.[ConfirmationCode]
		   ,e.[Category]
		   ,e.[ExpenseAmount]
		   ,e.[PropertyCode]
		   ,e.[ReservationId]
		   ,e.[IncludeOnStatement]
		   ,e.[ApprovedNote]
		   ,e.[ApprovalStatus]
		   ,'Memo' = case when j.[JobCostMemo] is null then '' else j.[JobCostMemo] end
		   ,'Reviewed' = case when e.[ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		   ,'Approved' = case when e.[ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
	FROM [dbo].[Expense] e
	LEFT JOIN [dbo].[Reservation] r ON r.[ReservationId] = e.[ReservationId]
	LEFT JOIN [dbo].[JobCost] j on j.[JobCostId] = e.[JobCostId]				   
	WHERE (CONVERT(date, e.[ExpenseDate]) >= @StartDate and CONVERT(date, e.[ExpenseDate]) <= @EndDate) and
		  (e.[PropertyCode] = @PropertyCode or @PropertyCode = '') and 
		  e.[IsDeleted] = 0
	ORDER BY r.[TransactionDate], [Category], [ExpenseAmount]
END