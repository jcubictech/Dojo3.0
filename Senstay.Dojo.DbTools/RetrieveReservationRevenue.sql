CREATE PROCEDURE [dbo].[RetrieveReservationRevenue]
	@StartDate DATETIME,
	@EndDate DATETIME,
	@PropertyCode NVARCHAR(50)
AS
BEGIN
	SELECT
		r.[OwnerPayoutId]
		,[ReservationId]
		,r.[PropertyCode]
		,'PayoutDate' = [TransactionDate]
		,[ConfirmationCode]
		,[Channel]
		,[IsTaxed]
		,'TaxRate' = (select Top 1 [CityTax] from [dbo].[PropertyFee] where [PropertyCode] = r.[PropertyCode] and Convert(Date,[EntryDate])  <= Convert(Date,r.[CheckinDate]) order by [EntryDate] desc)
		,'DamageWaiver' = (select Top 1 [DamageWaiver] from [dbo].[PropertyFee] where [PropertyCode] = r.[PropertyCode] and Convert(Date,[EntryDate])  <= Convert(Date,r.[CheckinDate]) order by [EntryDate] desc)
		,[TotalRevenue]
		,[CheckinDate]
		,[Nights]
		,[GuestName]
		,r.[IncludeOnStatement] --'IncludeOnStatement' = case when r.[IncludeOnStatement] = 0 then 0 else 1 end
		,r.[IsFutureBooking] -- 'IsFutureBooking' = case when r.[OwnerPayoutId] = 0 then Convert(bit, 0) else Convert(bit, 1) end
		,[ApprovalStatus]
		,r.[ApprovedNote]
		,'Reviewed' = case when [ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Approved' = case when [ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Finalized' = case when [ApprovalStatus] < 3 then Convert(bit, 0) else Convert(bit, 1) end
		,r.[InputSource]
		,'Source' = c.[Account]
		,'DataSource' = r.[Source]
	INTO #Temp		
	FROM [dbo].[Reservation] r
	INNER JOIN [dbo].[CPL] c on c.[PropertyCode] = r.[PropertyCode] and c.[PropertyCode] = @PropertyCode
	LEFT JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId]
	WHERE (((r.[Source] is null or r.[Source] like 'Off-Airbnb') and Convert(date, [CheckinDate]) >= Convert(date, @StartDate) and Convert(date, [CheckinDate]) <= Convert(date, @EndDate)) or
		   (r.[Source] not like 'Off-Airbnb' and Convert(date, [TransactionDate]) >= Convert(date, @StartDate) and Convert(date, [TransactionDate]) <= Convert(date, @EndDate)) or
		   r.[TransactionDate] is null) and
		   [ReservationId] > 0 and r.[InputSource] not like '%_pending' and r.[IsDeleted] = 0

	SELECT *
		,'TaxCollected' = case 
							when [IsTaxed] = 0 or [DataSource] not like 'Off-Airbnb' or [TaxRate] is null or [TaxRate] = 0 or [TotalRevenue] - [DamageWaiver] <= 0
							then 0 
							else Cast(Cast(([TotalRevenue] * [TaxRate]) as Decimal(18,2)) as float)
					   end
		,'OverlapColor' = case when (Select Count([ReservationId]) From #Temp tt
									 Where t.[ConfirmationCode] <> tt.[ConfirmationCode] and tt.[IncludeOnStatement] = 1 and t.[IncludeOnStatement] = 1 and
										   Not(Convert(date, t.[CheckinDate]) >= Convert(date, DateAdd(day, tt.[Nights], tt.[CheckinDate])) or 
											   Convert(date, DateAdd(day, t.[Nights], t.[CheckinDate])) <= Convert(date, tt.[CheckinDate]))) > 0
							   then 'red'
							   else ''
							   end
	FROM #Temp t
	ORDER BY t.[CheckinDate] asc, [ConfirmationCode] asc
END
