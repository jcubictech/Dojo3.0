CREATE PROCEDURE [dbo].[RetrievePayoutMethodPayments]
	@StartDate DateTime,
	@EndDate DateTime
AS
BEGIN
	Declare @Month int = Month(@StartDate),
			@Year int = Year(@StartDate)

	SELECT 
		   m.[PayoutMethodId]
		  ,m.[PayoutMethodName]
		  ,m.[EffectiveDate]
		  ,m.[PayoutAccount]
		  ,'PayoutMethodType' = case 
									when m.[PayoutMethodType] = 1 then 'Checking'
									when m.[PayoutMethodType] = 2 then 'Paypal'
									else 'Unknown'
								end 

		  --,'BeginBalance' = Round((select sum(s.[BeginBalance]) 
				--						from [dbo].[OwnerStatement] s
				--						inner join [dbo].[PropertyPayoutMethod] ppm on ppm.[PropertyCode] = s.[PropertyCode] and ppm.[PayoutMethodId] = m.[PayoutMethodId]
				--						where s.[StatementStatus] >= 1 and DATEFROMPARTS(s.[Year], s.[Month], 1) >= @StartDate and DATEFROMPARTS(s.[Year], s.[Month], 1) <= @EndDate), 2)

		  ,'BeginBalance' = Round((select sum(b.[AdjustedBalance]) 
										from [dbo].[PropertyBalance] b
										inner join [dbo].[PropertyPayoutMethod] ppm on ppm.[PropertyCode] = b.[PropertyCode] and ppm.[PayoutMethodId] = m.[PayoutMethodId]
										where b.[Year] = @Year and b.[Month] = @Month), 2)

		  ,'TotalBalance' = Round(( select sum(s.[Balance]) 
									from [dbo].[OwnerStatement] s
									inner join [dbo].[PropertyPayoutMethod] ppm on ppm.[PropertyCode] = s.[PropertyCode] and ppm.[PayoutMethodId] = m.[PayoutMethodId]
									where s.[StatementStatus] >= 1 and s.[Year] = @Year and s.[Month] = @Month), 2)

		  ,'PaymentTotal' = Round((select sum(pp.[PaymentAmount]) 
									from [dbo].[PayoutPayment] pp 
									where pp.[PayoutMethodId] = m.[PayoutMethodId] and DATEFROMPARTS(pp.[PaymentYear], pp.[PaymentMonth], 1) >= @StartDate and DATEFROMPARTS(pp.[PaymentYear], pp.[PaymentMonth], 1) <= @EndDate), 2)

		  ,'Properties' = Reverse(Stuff(Reverse((SELECT ppm.[PropertyCode] + ',' AS 'data()' 
									  FROM [dbo].[PropertyPayoutMethod] ppm
									  WHERE ppm.[PayoutMethodId] = m.[PayoutMethodId]
									  ORDER BY ppm.[PropertyCode]
									  FOR XML PATH(''))),1,1,''))
		  ,p.[PaymentAmount]
		  ,p.[PaymentDate]
		  ,'PaymentMonth' = case when p.[PaymentMonth] is null then @Month else p.[PaymentMonth] end
		  ,'PaymentYear' = case when p.[PaymentYear] is null then @Year else p.[PaymentYear] end
		  ,P.[PayoutPaymentId]
	INTO #Temp
	FROM [dbo].[PayoutMethod] m
	LEFT JOIN [dbo].[PayoutPayment] p on p.[PayoutMethodId] = m.[PayoutMethodId] and DATEFROMPARTS(p.[PaymentYear], p.[PaymentMonth], 1) >= @StartDate and DATEFROMPARTS(p.[PaymentYear], p.[PaymentMonth], 1) <= @EndDate
	WHERE Convert(Date, [EffectiveDate]) <= @EndDate and m.[IsDeleted] = 0

	SELECT [PayoutMethodId]
		  ,[PayoutMethodName]
		  ,[EffectiveDate]
		  ,[PayoutAccount]
		  ,[PayoutMethodType]
		  ,[BeginBalance]
		  ,'SelectedProperties' = [Properties]
		  ,'TotalBalance' = case when [TotalBalance] is null then 0 else Cast(Cast([TotalBalance] as decimal(18,2)) as float) end
		  ,'PayoutTotal' = case when [PaymentTotal] is null then 0 else Cast([PaymentTotal] as float) end
		  ,'CarryOver' = Cast(Cast([TotalBalance] - [PaymentTotal] as decimal(18,2)) as float)
		  ,[PaymentAmount]
		  ,[PaymentDate]
		  ,[PaymentMonth]
		  ,[PaymentYear]
		  ,[PayoutPaymentId]
	FROM #Temp
	--WHERE [PaymentMonth] is not null and [PaymentYear] is not null
	--WHERE [PayoutPaymentId] is not null and [PaymentDate] is not null and [PaymentAmount] is not null
	ORDER BY [PayoutMethodName], [PaymentDate]

END