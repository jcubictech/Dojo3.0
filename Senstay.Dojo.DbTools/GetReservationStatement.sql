CREATE PROCEDURE [dbo].[GetReservationStatement]
	@StartDate datetime ,
	@EndDate datetime ,
	@PropertyCode nvarchar(50)
AS
BEGIN

	SELECT
		'Type' = case when r.[Channel] not like 'Owner' and r.[Channel] not like 'Maintenance' and r.[Channel] not like 'Privé' then 'Reservation' else r.[Channel] end
		,'Guest' = [GuestName]
		,'Arrival' = Convert(date, [CheckinDate])
		,'Departure' = Convert(date, [CheckoutDate])
		,[Nights]
		,[TotalRevenue]
		,'ApprovedNote' = case when [ApprovedNote] is null then '' else [ApprovedNote] end
		,r.[Channel]
		,'TaxRate' = (select Top 1 [CityTax] from [dbo].[PropertyFee] where [PropertyCode] = r.[PropertyCode] and r.[IsTaxed] = 1 and Convert(Date,[EntryDate]) <= Convert(Date,r.[CheckinDate]) order by [EntryDate] desc)
		,'DamageWaiver' = (select Top 1 [DamageWaiver] from [dbo].[PropertyFee] where [PropertyCode] = r.[PropertyCode] and Convert(Date,[EntryDate]) <= Convert(Date,r.[CheckinDate]) order by [EntryDate] desc)
		,[CheckinDate]
	FROM [dbo].[Reservation] r
	WHERE (((r.[Source] is null or r.[Source] like 'Off-Airbnb') and Convert(date, [CheckinDate]) >= @StartDate and Convert(date, [CheckinDate]) <= @EndDate) or
		   (r.[Source] not like 'Off-Airbnb' and Convert(date, [TransactionDate]) >= @StartDate and Convert(date, [TransactionDate]) <= @EndDate)) and  		  
		  r.[PropertyCode] = @PropertyCode and 
		  [IsDeleted] = 0 and 
		  [IncludeOnStatement] = 1
	ORDER BY [CheckinDate]
END
