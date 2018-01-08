CREATE PROCEDURE [dbo].[RetrieveCombinedExpenses]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT [ExpenseId]
		  ,[ReservationId]
		  ,[Category]
		  ,[ExpenseAmount]
		  ,[CreatedDate]
		  ,[CreatedBy]
		  ,[ModifiedDate]
		  ,[ModifiedBy]
		  ,[ApprovalStatus]
		  ,[ReviewedBy]
		  ,[ReviewedDate]
		  ,[ApprovedBy]
		  ,[ApprovedDate]
		  ,[IsDeleted]
		  ,[PropertyCode]
		  ,[ExpenseDate]
		  ,[ParentId]
		  ,[ConfirmationCode]
		  ,[JobCostId]
		  ,[IncludeOnStatement]
		  ,[ApprovedNote]
	FROM [dbo].[Expense]
	WHERE (CONVERT(date, [ExpenseDate]) >= Convert(date, @StartDate) and CONVERT(date, [ExpenseDate]) <= Convert(date, @EndDate)) and
		  ([PropertyCode] = @PropertyCode or @PropertyCode = '') and 
		   [ExpenseId] = [ParentId] and
		   [IsDeleted] = 0
	ORDER BY [ExpenseId] desc
END