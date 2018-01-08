CREATE PROCEDURE [dbo].[GetDuplicateReservations]
	@StartDate datetime,
	@EndDate datetime

AS
BEGIN

	-- get the duplicate record set first
	select count(*) as 'Count'
		,[PropertyCode]
		,[ConfirmationCode]
		,[GuestName]
		,[CheckinDate] = Convert(date, [CheckinDate])
		,[Nights]
		,[TotalRevenue]
	into #Temp
	from [dbo].[Reservation] r
	where r.[IsDeleted] = 0 and Convert(date, r.[TransactionDate]) >= @StartDate and Convert(date, r.[TransactionDate]) <= @EndDate
	group by[PropertyCode], [ConfirmationCode], [GuestName], [CheckinDate], [Nights], [TotalRevenue]
	having count(*) > 1

	-- add transaction date to each record
	select distinct
		 t.[PropertyCode]
		,t.[ConfirmationCode]
		,t.[GuestName]
		,[CheckinDate] = Convert(date, t.[CheckinDate])
		,t.[Nights]
		,t.[TotalRevenue]
		,r.[TransactionDate]
	into #Temp2
	from #Temp t
	inner join [dbo].[Reservation] r on r.[PropertyCode] = t.[PropertyCode] and 
										r.[ConfirmationCode] = t.[ConfirmationCode] and 
										r.[GuestName] = t.[GuestName] and 
										r.[TotalRevenue] = t.[TotalRevenue] and
										r.[Nights] = t.[Nights] and
										Convert(date, r.[CheckinDate]) = Convert(date, t.[CheckinDate])

	select
		 t.[PropertyCode]
		,t.[ConfirmationCode]
		,t.[GuestName]
		,t.[CheckinDate]
		,t.[Nights]
		,t.[TotalRevenue]
		,t.[TransactionDate]
		,r.[ReservationId]
		,r.[OwnerPayoutId]
		,[PayoutAccount] = (select top 1 [AccountNumber] from [dbo].[OwnerPayout] p where p.[OwnerPayoutId] = r.[OwnerPayoutId])
	from #Temp2 t
	inner join [dbo].[Reservation] r on r.[PropertyCode] = t.[PropertyCode] and 
										r.[ConfirmationCode] = t.[ConfirmationCode] and 
										r.[GuestName] = t.[GuestName] and 
										r.[TotalRevenue] = t.[TotalRevenue] and
										r.[Nights] = t.[Nights] and
										Convert(date, r.[CheckinDate]) = Convert(date, t.[CheckinDate]) and
										Convert(date, r.[TransactionDate]) = Convert(date, t.[TransactionDate])
	order by [PropertyCode], [ConfirmationCode], [GuestName], [CheckinDate], [Nights], [TotalRevenue], [PayoutAccount], [TransactionDate]

END
