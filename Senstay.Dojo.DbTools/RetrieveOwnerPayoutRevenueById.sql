CREATE PROCEDURE [dbo].[RetrieveOwnerPayoutRevenueById]
	@OwnerPayoutId int
AS
BEGIN
	SELECT [OwnerPayoutId]
		,[Source]
		,[PayoutDate]
		,'PayToAccount' = [AccountNumber]
		,[PayoutAmount]
		,[IsAmountMatched]
		,'DiscrepancyAmount' = Round([DiscrepancyAmount], 2)
		,[InputSource]
	FROM [dbo].[OwnerPayout]
	WHERE [OwnerPayoutId] = @OwnerPayoutId and [IsDeleted] = 0

END  
