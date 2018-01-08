CREATE PROCEDURE [dbo].[GetAdvancePaymentStatement]
	@StartDate datetime ,
	@EndDate datetime ,
	@PropertyCode nvarchar(50)
AS
BEGIN

SELECT * INTO #Temp FROM
(
	SELECT 
		'Guest' = r.[GuestName]
		,'PaymentDate' = r.[TransactionDate]
		,'Amount' = r.[TotalRevenue]
	FROM [dbo].[OwnerPayout] p
	INNER JOIN [dbo].[Reservation] r ON r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[PropertyCode] = @PropertyCode and r.[includeOnStatement] = 1 and 
										r.[IsDeleted] = 0 and [Channel] not like 'Owner%' and r.[Source] is not null
	WHERE Convert(date, [PayoutDate]) >= @StartDate and Convert(date, [PayoutDate]) <= @EndDate and
		  --p.[AccountNumber] not like '%9146' and 
		  RIGHT(p.[AccountNumber], 4) not in (select RIGHT([Name], 4) from [dbo].[Lookup] where [Type] = 'SenStayAccount') and
		  p.[IsDeleted] = 0

	UNION ALL

	SELECT Distinct
		'Guest' = v.[GuestName]
		,'PaymentDate' = r.[ResolutionDate]
		,'Amount' = r.[ResolutionAmount]
	FROM [dbo].[Resolution] r
	INNER JOIN [dbo].[Reservation] v on v.[ConfirmationCode] = r.[ConfirmationCode] and v.[PropertyCode] = @PropertyCode and v.[IsDeleted] =  0 and [Channel] not like 'Owner%'
	INNER JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId] and RIGHT(p.[AccountNumber], 4) not in (select RIGHT([Name], 4) from [dbo].[Lookup] where [Type] = 'SenStayAccount') -- and p.[AccountNumber] not like '%9146'
	WHERE (CONVERT(date, [ResolutionDate]) >= @StartDate and CONVERT(date, [ResolutionDate]) <= @EndDate) and
		   [ResolutionId] > 0 and r.[ConfirmationCode] is not null and r.[Impact] <> 'Nonowner Expense' and
		   r.[includeOnStatement] = 1 and r.[IsDeleted] =  0 and (r.[ApprovedNote] not like '%exclude from advance payment%' or r.[ApprovedNote] is null)

	UNION ALL

	SELECT Distinct
		'Guest' = r.[ResolutionDescription]
		,'PaymentDate' = r.[ResolutionDate]
		,'Amount' = r.[ResolutionAmount]
	FROM [dbo].[Resolution] r
	INNER JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId]
	WHERE (CONVERT(date, [ResolutionDate]) >= @StartDate and CONVERT(date, [ResolutionDate]) <= @EndDate) and
		   [ResolutionId] > 0 and r.[PropertyCode] = @PropertyCode and r.[Impact] = 'Advance Payout' and r.[includeOnStatement] = 1 and r.[IsDeleted] =  0

) AS AdvancePayments

SELECT distinct * FROM #Temp
WHERE [Guest] IS NOT nulL
ORDER BY 'PaymentDate', 'Guest'

END