CREATE PROCEDURE [dbo].[RetrieveReservationRevenueById]
	@ReservationId int
AS
BEGIN
	SELECT Top 1 
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
		,r.[ApprovalStatus]
		,r.[ApprovedNote]
		,'Reviewed' = case when [ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Approved' = case when [ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Finalized' = case when [ApprovalStatus] < 3 then Convert(bit, 0) else Convert(bit, 1) end				
		,r.[InputSource]
		,r.[Source]			
	FROM [dbo].[Reservation] r
	LEFT JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId]
	WHERE [ReservationId] = @ReservationId and r.[InputSource] not like '%_pending'
END 