CREATE PROCEDURE [dbo].[GetPayoutBalancesForMonth]
	@Month int,
	@Year int,
	@PayoutMethodId int = 0
AS
BEGIN
	Declare @LastMonthDate DateTime,
			@LastMonth int,
			@LastYear int

	SET @LastMonthDate = DateAdd(Month, -1, DATEFROMPARTS(@Year, @Month, 1))
	SET @LastMonth = Month(@LastMonthDate)
	SET @LastYear = Year(@LastMonthDate)

	SELECT
		 m.[PayoutMethodId]
		 ,m.[PayoutMethodName]
		 ,m.[PayoutAccount]
		 ,m.[PayoutMethodType]
		 ,p.[PayoutPaymentId]
		 ,p.[PaymentDate]
		 ,'BeginBalance' = Round((select sum(b.[AdjustedBalance]) 
										from [dbo].[PropertyBalance] b
										inner join [dbo].[PropertyPayoutMethod] ppm on ppm.[PropertyCode] = b.[PropertyCode] and ppm.[PayoutMethodId] = m.[PayoutMethodId]
										where b.[Month] = @Month and b.[Year] = @Year), 2)
		 
		 --,'BeginBalance' = Round((select sum(s.[BeginBalance]) 
			--							from [dbo].[OwnerStatement] s
			--							inner join [dbo].[PropertyPayoutMethod] ppm on ppm.[PropertyCode] = s.[PropertyCode] and ppm.[PayoutMethodId] = m.[PayoutMethodId]
			--							where s.[StatementStatus] >= 1 and s.[Month] = @Month and s.[Year] = @Year), 2)
		 
		 ,'TotalBalance' = (select Top 1 [Balance] from [dbo].[OwnerStatement] os where os.[PropertyCode] = m.[PayoutMethodName] and os.[Month] = @Month and os.[Year] = @Year and os.[StatementStatus] >= 1)
		 ,'PaymentAmount' = (select Sum([PaymentAmount]) from [dbo].[PayoutPayment] p where p.[PaymentMonth] = @Month and p.[PaymentYear] = @Year and p.[PayoutMethodId] = m.[PayoutMethodId])
	INTO #Temp
	FROM [dbo].[PayoutMethod] m
	LEFT JOIN [dbo].[PayoutPayment] p on p.[PayoutMethodId] = m.[PayoutMethodId] and p.[PaymentMonth] = @Month and p.[PaymentYear] = @Year
	LEFT JOIN [dbo].[OwnerStatement] s on m.[PayoutMethodName] = s.[PropertyCode] and s.[Month] = @Month and s.[Year] = @Year and s.[StatementStatus] >= 1
	WHERE (@PayoutMethodId = 0 or m.[PayoutMethodId] = @PayoutMethodId)

	SELECT 
		  [PayoutMethodId]
		 ,[PayoutPaymentId] = case when [PayoutPaymentId] is null then 0 else [PayoutPaymentId] end
		 ,[PayoutMethodName]
		 ,[PayoutAccount]
		 ,[PayoutMethodType]
		 ,[PaymentMonth] = @Month
		 ,[PaymentYear] = @Year
		 ,[PaymentDate]
		 ,[BeginBalance] = case when [BeginBalance] is not null then [BeginBalance] 
							else (select top 1 [Balance]  -- try to get from owner summary ending balance
								  from [dbo].[OwnerStatement] s 
								  where s.[Month] <= @LastMonth and s.[Year] <= @LastYear and s.[PropertyCode] = t.[PayoutMethodName] and s.[IsSummary] = 1 and s.[BeginBalance] is not null
								  order by s.[Year] desc, s.[Month] desc)
							end
		 ,[TotalBalance]
		 ,[PaymentAmount]
		 ,[CarryOver] = case when [TotalBalance] < 0 and ([PaymentAmount] is null or [PaymentAmount] = 0) then [TotalBalance] 
							 when [TotalBalance] < 0 and ([PaymentAmount] is null or [PaymentAmount] > 0) then [TotalBalance] - [PaymentAmount]
							 when ([TotalBalance] is null or [TotalBalance] = 0) and ([PaymentAmount] is null or [PaymentAmount] = 0) then [BeginBalance]
							 else Convert(float, Cast([TotalBalance] - [PaymentAmount] as decimal(18,2)))
						end
	FROM #Temp t
	ORDER BY [PayoutMethodName]
END