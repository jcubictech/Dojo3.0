CREATE PROCEDURE [dbo].[RetrieveExpenseRevenueById]
	@ExpenseId int
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
		   ,'Reviewed' = case when e.[ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		   ,'Approved' = case when e.[ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
	FROM [dbo].[Expense] e
	LEFT JOIN [dbo].[Reservation] r ON r.[ReservationId] = e.[ReservationId]								   
	WHERE e.[ExpenseId] = @ExpenseId and 
		  e.[IsDeleted] = 0
	ORDER BY r.[TransactionDate], [Category], [ExpenseAmount]
END