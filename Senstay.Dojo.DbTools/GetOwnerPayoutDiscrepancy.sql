CREATE PROCEDURE [dbo].[GetOwnerPayoutDiscrepancy]
	@OwnerPayoutId int
AS
BEGIN

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
			'Delta' = [PayoutAmount]  - sum(r.[ResolutionAmount])
		FROM [dbo].[OwnerPayout] p
		INNER JOIN [dbo].[Resolution] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		WHERE p.[OwnerPayoutId] = @OwnerPayoutId
		GROUP BY p.[OwnerPayoutId], [PayoutAmount]
	) As Payout

	SELECT 'Discrepancy' = sum([Delta]) FROM #Temp

END
