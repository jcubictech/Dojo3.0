CREATE PROCEDURE [dbo].[GetCarryOverForProperties]
	@Month int,
	@Year int,
	@PropertyCode nvarchar(50) = ''
AS
BEGIN

	Declare @StatementDate DateTime,
			@PayoutMethod nvarchar(100) = ''

	Set @StatementDate = Convert(Date, DATEFROMPARTS(@Year, @Month, 15))

	if (@PropertyCode <> '')
	begin
		select Top 1 @PayoutMethod = [PayoutMethodName] from [PayoutMethod] m
		inner join [dbo].[PropertyPayoutMethod] pm on pm.[PayoutMethodId] = m.[PayoutMethodId] and pm.[PropertyCode] = @PropertyCode
	end

	select [OwnerStatementId]
			,s.[PropertyCode]
			,[Balance]
			,[EndingBalance] = (select Top 1 (case when p.[PaymentAmount] is not null then os.[Balance] - p.[PaymentAmount] else os.[Balance] end)
								from [dbo].[OwnerStatement] os
								inner join [dbo].[PayoutMethod] m on m.[PayoutMethodName] = os.[PropertyCode] and m.[EffectiveDate] <= @StatementDate
								inner join [dbo].[PropertyPayoutMethod] pm on pm.[PropertyCode] = s.[PropertyCode] and pm.[PayoutMethodId] = m.[PayoutMethodId]
								left join [dbo].[PayoutPayment] p on p.[PayoutMethodId] = m.[PayoutMethodId] and p.[PaymentMonth] = @Month and p.[PaymentYear] = @year
								where os.[Month] = @Month and os.[Year] = @year and os.[IsSummary] = 1
								order by os.[PropertyCode], m.[EffectiveDate] desc)

			,m.[PayoutMethodName]
			,m.[PayoutMethodId]
	into #Temp
	from [dbo].[OwnerStatement] s
	inner join [dbo].[PropertyPayoutMethod] pm on pm.[PropertyCode] = s.[PropertyCode]
	inner join [dbo].[PayoutMethod] m on m.[PayoutMethodId] = pm.[PayoutMethodId] and Convert(date, m.[EffectiveDate]) <= @StatementDate and (@PayoutMethod = '' or m.[PayoutMethodName] = @PayoutMethod)
	where s.[Month] = @Month and s.[Year] = @year and s.[IsSummary] = 0
	
	select [OwnerStatementId]
		   ,[PayoutMethodName]
		   ,[PropertyCode]
		   ,[Balance]
		   ,[TotalBalance] = (case when p.[PaymentAmount] is null then 0 else p.[PaymentAmount] end) + (case when [EndingBalance] is null then [Balance] else [EndingBalance] end)
		   ,[TotalPayment] = (case when p.[PaymentAmount] is null then 0 else p.[PaymentAmount] end)
		   ,[CarryOver] = Cast(Cast(case when [EndingBalance] is null then [Balance] else [EndingBalance] end as decimal(18,2)) as float)
	from #Temp t
	left join [dbo].[PayoutPayment] p on p.[PayoutMethodId] = t.[PayoutMethodId] and p.[PaymentMonth] = @Month and p.[PaymentYear] = @year
	order by [PayoutMethodName], [PropertyCode]

END