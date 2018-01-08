CREATE PROCEDURE [dbo].[GetOwnerSummaryPayoutMethodList]
	@Year int,
	@Month int 
AS
BEGIN
	DECLARE @EffectiveDate DateTime = DATEADD(month, 1, DATEFROMPARTS(@Year, @Month, 1))

	SELECT 
		m.[PayoutMethodId]
		,m.[PayoutMethodName]
		,'PropertyCount' = (select count(pp.[PropertyCode]) from [dbo].[PayoutMethod] p
							 inner join [dbo].[PropertyPayoutMethod] pp on pp.[PayoutMethodId] = p.[PayoutMethodId]
							 inner join [dbo].[CPL] c on c.[PropertyCode] = pp.[PropertyCode] and 
														 c.[PropertyStatus] <> 'Dead' and 
														 --c.[PropertyStatus] not like 'Pending%' and 
														 c.[Account] is not null and
														 (c.[ListingStartDate] is null or c.[ListingStartDate] < @EffectiveDate)
							 where p.[IsDeleted] = 0 and p.[PayoutMethodId] = m.[PayoutMethodId])
		,'StatementFinalizeCount' = (select count(os.[OwnerStatementId]) from [dbo].[OwnerStatement] os
									 inner join [dbo].[PropertyPayoutMethod] pm on pm.[PropertyCode] = os.[PropertyCode] and pm.[PayoutMethodId] = m.[PayoutMethodId]
									 where os.[Month] = @Month and os.[Year] = @Year and os.[StatementStatus] = 2)
		,'StatementNoDataCount' = (select count(os.[OwnerStatementId]) from [dbo].[OwnerStatement] os
									 inner join [dbo].[PropertyPayoutMethod] pm on pm.[PropertyCode] = os.[PropertyCode] and pm.[PayoutMethodId] = m.[PayoutMethodId]
									 where os.[Month] = @Month and os.[Year] = @Year and (os.[StatementStatus] = 1 and 
										   os.[Balance] = 0 and os.[AdvancePayments] = 0 and os.[UnitExpenseItems] = 0 and os.[CleaningFees] = 0 and os.[TotalRevenue] = 0))
		,'SummaryFinalizeCount' = (select count(os.[OwnerStatementId]) from [dbo].[OwnerStatement] os
									where os.[Month] = @Month and os.[Year] = @Year and os.[PropertyCode] = m.[PayoutMethodName] and os.[IsSummary] = 1 and [StatementStatus] = 2)
	INTO #Temp
	FROM [dbo].[PayoutMethod] m
	LEFT JOIN [dbo].[OwnerStatement] s on s.[PropertyCode] = m.[PayoutMethodName] and s.[Month] = @Month and s.[Year] = @Year and s.[IsSummary] = 1
	ORDER BY m.[PayoutMethodName]

	SELECT 
		'PayoutMethod' = [PayoutMethodName]
		,'PayoutMethodAndPropertyCode' = [PayoutMethodName]
		,[SummaryFinalized] = case when [SummaryFinalizeCount] > 0 then 1 else 0 end
		,[StatementFinalized] = case when [StatementFinalizeCount] > 0 and [StatementFinalizeCount] + [StatementNoDataCount] - [PropertyCount] >= 0 then 1 else 0 end
		,[StatementPartialFinalized] = case when [StatementFinalizeCount] > 0 and [PropertyCount] - [StatementFinalizeCount] > 0 then 1 else 0 end
		,[Empty] = case when [StatementFinalizeCount] > 0 then 0 else 1 end
		--,[SummaryFinalizeCount]
		,[StatementFinalizeCount]
		--,[StatementNoDataCount]
		,[PropertyCount]
		,[PayoutMethodId]
	FROM #Temp
	ORDER BY [PayoutMethodName]
END