CREATE PROCEDURE [dbo].[RetrieveCombinedExpensesRevenue]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT * INTO #Temp FROM
	(
		SELECT [ExpenseId] = 0
			  ,[ParentId] = 0
			  ,'TreeNode' = '<div class="expense-tree-node" data-id="0" data-parentid="0" id="expense-tree-id-0">Expense Home</div>'
	
		UNION

		SELECT [ExpenseId]
			  ,[ParentId]
			  ,'TreeNode' = '<div class="expense-tree-node" data-id="' + Convert(NVARCHAR(10), [ExpenseId]) + 
							'" data-parentid="' + Convert(NVARCHAR(10), [ParentId]) + 
							'" id="expense-tree-id-' + Convert(NVARCHAR(10), [ExpenseId]) + 
							'">' + Convert(NVARCHAR(16), [ExpenseDate], 102) + 
							'---' + [Category] + 
							'---$' + Convert(NVARCHAR(20), [ExpenseAmount]) + 
							'---' + [PropertyCode]
		FROM [dbo].[Expense] e
		WHERE (CONVERT(date, e.[ExpenseDate]) >= @StartDate and CONVERT(date, e.[ExpenseDate]) <= @EndDate) and
			  (e.[PropertyCode] = @PropertyCode or @PropertyCode = '') and e.[IsDeleted] = 0
	) AS ExpenseTree

	SELECT * FROM #Temp ORDER BY [ParentId], [ExpenseId] desc -- must be sorted in this order
END 