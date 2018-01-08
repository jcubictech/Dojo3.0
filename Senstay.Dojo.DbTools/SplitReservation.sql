CREATE PROCEDURE [dbo].[SplitReservation]
	@ReservationId int,
	@PropertyCodes nvarchar(500)
AS
BEGIN

	DECLARE @NewReservationId int,
			@NewPropertyCode nvarchar(50),
			@NewPropertyCount int,
			@NewPropertyIndex int = 0

	SELECT @NewPropertyCount = Count([Value]) from [dbo].[SplitString](@PropertyCodes, ';')

	DECLARE PropertyCursor CURSOR FOR  
	SELECT [Value] from [dbo].[SplitString](@PropertyCodes, ';')

	OPEN PropertyCursor 

	FETCH NEXT FROM PropertyCursor INTO @NewPropertyCode

	WHILE @@FETCH_STATUS = 0  
	BEGIN
		-- add a same record for the splitted reservation
		INSERT INTO [dbo].[Reservation] 
			  ([OwnerPayoutId],[PropertyCode],[ConfirmationCode],[TransactionDate],[CheckinDate],[Nights],[GuestName],[Reference],[Currency],[Source],[CreatedDate],
			   [CreatedBy],[ModifiedDate],[ModifiedBy],[InputSource],[CheckoutDate],[Channel],[TotalRevenue],[LocalTax],[DamageWaiver],[AdminFee],[PlatformFee],
			   [TaxRate],[IncludeOnStatement],[IsFutureBooking],[ApprovalStatus],[ListingTitle],[ReviewedBy],[ReviewedDate],[ApprovedBy],[ApprovedDate],[IsDeleted],[ApprovedNote])

		SELECT [OwnerPayoutId],[PropertyCode],[ConfirmationCode],[TransactionDate],[CheckinDate],[Nights],[GuestName],[Reference],[Currency],[Source],[CreatedDate],
			   [CreatedBy],[ModifiedDate],[ModifiedBy],[InputSource],[CheckoutDate],[Channel],[TotalRevenue],[LocalTax],[DamageWaiver],[AdminFee],[PlatformFee],
			   [TaxRate],[IncludeOnStatement],[IsFutureBooking],[ApprovalStatus],[ListingTitle],[ReviewedBy],[ReviewedDate],[ApprovedBy],[ApprovedDate],[IsDeleted],[ApprovedNote]

		FROM [dbo].[Reservation] WHERE [ReservationId] = @ReservationId

		SET @NewReservationId = IDENT_CURRENT('[dbo].[Reservation]')
	
		-- update the splitted reservation to new property code, append index to confirmation code, and set the splitted total revenue
		SET @NewPropertyIndex = @NewPropertyIndex + 1
		UPDATE [dbo].[Reservation] SET [PropertyCode] = @NewPropertyCode, 
										[ConfirmationCode] = [ConfirmationCode] + '-' + Cast(@NewPropertyIndex as nvarchar(10)), 
										[TotalRevenue] = [TotalRevenue] / @NewPropertyCount
		WHERE [ReservationId] = @NewReservationId

		FETCH NEXT FROM PropertyCursor INTO @NewPropertyCode 
	END
	CLOSE PropertyCursor
	DEALLOCATE PropertyCursor

	-- mark the source reservation as deleted
	IF @NewPropertyCount is not null and (@NewPropertyCount > 0)
		UPDATE [dbo].[Reservation] SET [IsDeleted] = 1 WHERE [ReservationId] = @ReservationId

	SELECT @NewPropertyCount as 'Result'

END
