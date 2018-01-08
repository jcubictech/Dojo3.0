CREATE PROCEDURE [dbo].[SyncReservationDateWithOwnerPayout]
AS
BEGIN
	DECLARE @OwnerPayoutId int, 
			@TransactionDate DateTime,
			@PayoutDate DateTime,
			@TotalRevenue float, 
			@UpdateCount int,
			@ErrorCount int

	SET @UpdateCount = 0
	SET @ErrorCount = 0

	DECLARE MigrateCursor CURSOR FOR
		SELECT r.[OwnerPayoutId], [TransactionDate], [PayoutDate]
		FROM [dbo].[Reservation] r
		INNER JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId] and r.[TransactionDate] <> p.[PayoutDate]

	OPEN MigrateCursor

	FETCH NEXT FROM MigrateCursor   
	INTO @OwnerPayoutId, @TransactionDate, @PayoutDate

	WHILE @@FETCH_STATUS = 0
	BEGIN
		BEGIN TRY
			UPDATE [dbo].[Reservation] SET [TransactionDate] = @PayoutDate WHERE [OwnerPayoutId] = @OwnerPayoutId

			SET @UpdateCount = @UpdateCount + 1
		END TRY

		BEGIN CATCH
			SET @ErrorCount = @ErrorCount + 1
		END CATCH

		FETCH NEXT FROM MigrateCursor   
	INTO @OwnerPayoutId, @TransactionDate, @PayoutDate
	END

	CLOSE MigrateCursor 
	DEALLOCATE MigrateCursor

	SELECT 'Resolution Update Count' = Convert(NVARCHAR(10), @UpdateCount),
		   'Resolution  Error Count' = Convert(NVARCHAR(10), @ErrorCount)

END
