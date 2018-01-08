CREATE PROCEDURE [dbo].[GenerateSqlForOffAirbnbTotalRevenue]
AS
BEGIN

	--update [dbo].[Reservation] set [TotalRevenue] = 3402.49 where [ReservationId] = 9330 or [ReservationId] = 9331  -- 4249.09
	--update [dbo].[Reservation] set [TotalRevenue] = 865.89 where [ReservationId] = 9320  -- 1118.7

	SELECT r.[PropertyCode]
		,[ConfirmationCode]
		,[CheckInDate]
		,[IsTaxed]
		,f.[CityTax]
		,f.[DamageWaiver]
		,[TotalRevenue]
		,'GrossRevenue' = case
							when [IsTaxed] = 1 and f.[CityTax] > 0 and [TotalRevenue] - f.[DamageWaiver] > 0 
								then Cast(Cast((([TotalRevenue] - f.[DamageWaiver]) / (1.14 + f.[CityTax])) as decimal(18,2)) as float)
							else 
								Cast(Cast([TotalRevenue] as decimal(18,2)) as float)
						  end
		,'Updatesql' = case
							when [IsTaxed] = 1 and f.[CityTax] > 0 and [TotalRevenue] - f.[DamageWaiver] > 0 
								then 'Update [dbo].[Reservation] Set [AdminFee] = ' + Cast([TotalRevenue] as nvarchar(20)) + ', [TotalRevenue] = ' + 
									  Cast(Cast(Cast((([TotalRevenue] - f.[DamageWaiver]) / (1.14 + f.[CityTax])) as decimal(18,2)) as float) as nvarchar(20)) +
									 ' Where [ReservationId] = ' + Cast(r.[ReservationId] as nvarchar(10))
							when [IsTaxed] = 1 and f.[CityTax] > 0 and [TotalRevenue] - f.[DamageWaiver] <= 0
								then 'Update [dbo].[Reservation] Set [AdminFee] = ' + Cast([TotalRevenue] as nvarchar(20)) + ', [TotalRevenue] = 0' + 
									 ' Where [ReservationId] = ' + Cast(r.[ReservationId] as nvarchar(10))
							else 
								'Update [dbo].[Reservation] Set [AdminFee] = ' + Cast([TotalRevenue] as nvarchar(20)) + 
									 ' Where [ReservationId] = ' + Cast(r.[ReservationId] as nvarchar(10))
						  end
		,'RestoreSql' = 'Update [dbo].[Reservation] Set [TotalRevenue] = [AdminFee] Where [AdminFee] <> 0 and [ReservationId] = ' + Cast(r.[ReservationId] as nvarchar(10))
		,[IncludeOnStatement]
		,[ApprovalStatus]
		,r.[ReservationId]
	FROM [dbo].[Reservation] r
	INNER JOIN [dbo].[PropertyFee] f on f.[PropertyCode] = r.[PropertyCode]
	WHERE [Channel] not like 'Airbnb' and [Channel] is not null and [TotalRevenue] <> 0 and f.[DamageWaiver] <> 0 
		  --and f.[CityTax] > 0 and [CheckInDate] >= '2017-07-01' --and r.[IsDeleted] = 0
	ORDER BY [CheckInDate], r.[PropertyCode]

END