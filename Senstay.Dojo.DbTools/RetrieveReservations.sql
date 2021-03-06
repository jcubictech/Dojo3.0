CREATE PROCEDURE [dbo].[RetrieveReservations]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT [ReservationId]
      ,[OwnerPayoutId]
      ,[PropertyCode]
      ,[ConfirmationCode]
      ,[TransactionDate]
      ,[CheckinDate]
      ,[CheckoutDate]
      ,[Nights]
      ,[GuestName]
      ,[Channel]
      ,[Source]
      ,[Reference]
      ,[Currency]
      ,[TotalRevenue]
      ,[LocalTax]
      ,[DamageWaiver]
      ,[AdminFee]
      ,[PlatformFee]
      ,[TaxRate]
      ,[IncludeOnStatement]
      ,[IsFutureBooking]
      ,[ApprovalStatus]
      ,[ReviewedBy]
      ,[ReviewedDate]
      ,[ApprovedBy]
      ,[ApprovedDate]
      ,[ApprovedNote]
      ,[IsDeleted]
      ,[InputSource]
      ,[ListingTitle]
      ,[CreatedDate]
      ,[CreatedBy]
      ,[ModifiedDate]
      ,[ModifiedBy]
      ,[IsTaxed]
	FROM [dbo].[Reservation]
	WHERE (([Source] like 'Off-Airbnb' and Convert(date, [CheckinDate]) >= Convert(date, @StartDate) and Convert(date, [CheckinDate]) <= Convert(date, @EndDate)) or
		   ([Source] not like 'Off-Airbnb' and [InputSource] not like '%_pending' and Convert(date, [TransactionDate]) >= Convert(date, @StartDate) and Convert(date, [TransactionDate]) <= Convert(date, @EndDate)) or
		   [TransactionDate] is null) and
		  ([PropertyCode] = @PropertyCode or @PropertyCode = '') and [InputSource] not like '%_pending' and
		  [IsDeleted] = 0
	ORDER BY [TransactionDate] desc
END
