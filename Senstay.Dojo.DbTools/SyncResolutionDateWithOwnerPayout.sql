CREATE PROCEDURE [dbo].[SyncResolutionDateWithOwnerPayout]
AS
BEGIN
	DECLARE @OwnerPayoutId int, 
			@ResolutionDate DateTime,
			@PayoutDate DateTime,
			@TotalRevenue float, 
			@UpdateCount int,
			@ErrorCount int

	SET @UpdateCount = 0
	SET @ErrorCount = 0

	DECLARE MigrateCursor CURSOR FOR
		SELECT r.[OwnerPayoutId], [ResolutionDate], [PayoutDate]
		FROM [dbo].[Resolution] r
		INNER JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId] and r.[ResolutionDate] <> p.[PayoutDate]

	OPEN MigrateCursor

	FETCH NEXT FROM MigrateCursor   
	INTO @OwnerPayoutId, @ResolutionDate, @PayoutDate

	WHILE @@FETCH_STATUS = 0
	BEGIN
		BEGIN TRY
			UPDATE [dbo].[Resolution] SET [ResolutionDate] = @PayoutDate WHERE [OwnerPayoutId] = @OwnerPayoutId

			SET @UpdateCount = @UpdateCount + 1
		END TRY

		BEGIN CATCH
			SET @ErrorCount = @ErrorCount + 1
		END CATCH

		FETCH NEXT FROM MigrateCursor   
	INTO @OwnerPayoutId, @ResolutionDate, @PayoutDate
	END

	CLOSE MigrateCursor 
	DEALLOCATE MigrateCursor

	SELECT 'Resolution Update Count' = Convert(NVARCHAR(10), @UpdateCount),
		   'Resolution  Error Count' = Convert(NVARCHAR(10), @ErrorCount)

END

