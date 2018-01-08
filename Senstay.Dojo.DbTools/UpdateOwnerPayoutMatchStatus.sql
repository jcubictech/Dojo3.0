CREATE PROCEDURE [dbo].[UpdateOwnerPayoutMatchStatus]
	@OwnerPayoutId int
AS
BEGIN
	
	DECLARE @Discrepancy float

	SELECT * INTO #Temp FROM
	(
		SELECT 
			'Delta' = [PayoutAmount]  - sum(r.[TotalRevenue])
		FROM [dbo].[OwnerPayout] p
		INNER JOIN [dbo].[Reservation] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		WHERE p.[OwnerPayoutId] = @OwnerPayoutId
		GROUP BY p.[OwnerPayoutId], [PayoutAmount]

		UNION

		SELECT 
			'Delta' = -sum(r.[ResolutionAmount])
		FROM [dbo].[OwnerPayout] p
		INNER JOIN [dbo].[Resolution] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		WHERE p.[OwnerPayoutId] = @OwnerPayoutId
		GROUP BY p.[OwnerPayoutId], [PayoutAmount]
	) As Payout

	SELECT @Discrepancy = sum([Delta]) FROM #Temp

	IF @Discrepancy <> 0
	BEGIN
		UPDATE [dbo].[OwnerPayout] SET [IsAmountMatched] = 0, [DiscrepancyAmount] = @Discrepancy WHERE [OwnerPayoutId] = @OwnerPayoutId
	END
	ELSE
	BEGIN
		UPDATE [dbo].[OwnerPayout] SET [IsAmountMatched] = 1, [DiscrepancyAmount] = 0 WHERE [OwnerPayoutId] = @OwnerPayoutId
	END

	SELECT 'Count' = case when @Discrepancy is null then 0 else 1 end

END
