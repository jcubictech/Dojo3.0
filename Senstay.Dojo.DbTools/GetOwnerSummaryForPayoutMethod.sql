CREATE PROCEDURE [dbo].[GetOwnerSummaryForPayoutMethod]
	@Month int,
	@Year int,
	@PayoutMehtodId int
AS
BEGIN

	SELECT [OwnerStatementId]
      ,[Month]
      ,[Year]
      ,s.[PropertyCode]
      ,[StatementStatus]
      ,[FinalizedBy]
      ,[FinalizedDate]
      ,[TotalRevenue]
      ,[TaxCollected]
      ,[CleaningFees]
      ,[ManagementFees]
      ,[UnitExpenseItems]
      ,[AdvancePayments]
      ,[Balance]
      ,s.[BeginBalance]
	  ,s.[IsSummary]
	FROM [dbo].[OwnerStatement] s
	INNER JOIN [dbo].[PayoutMethod] m on m.[PayoutMethodId] = @PayoutMehtodId
	INNER JOIN [dbo].[PropertyPayoutMethod] p on p.[PayoutMethodId] = m.[PayoutMethodId] and p.[PropertyCode] = s.[PropertyCode]
	WHERE s.[Month] = @Month and s.[Year] = @Year

	UNION

	SELECT [OwnerStatementId]
      ,[Month]
      ,[Year]
      ,[PropertyCode]
      ,[StatementStatus]
      ,[FinalizedBy]
      ,[FinalizedDate]
      ,[TotalRevenue]
      ,[TaxCollected]
      ,[CleaningFees]
      ,[ManagementFees]
      ,[UnitExpenseItems]
      ,[AdvancePayments]
      ,[Balance]
      ,[BeginBalance]
	  ,[IsSummary]
	FROM [dbo].[OwnerStatement] s
	INNER JOIN [dbo].[PayoutMethod] m on m.[PayoutMethodId] = @PayoutMehtodId and s.[PropertyCode] = m.[PayoutMethodName]
	WHERE s.[Month] = @Month and s.[Year] = @Year and s.[IsSummary] = 1

END