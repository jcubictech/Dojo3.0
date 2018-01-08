CREATE PROCEDURE [dbo].[RetrieveOwnerPayouts]
	@StartDate DateTime,
	@EndDate DateTime
AS
BEGIN
	SELECT [OwnerPayoutId]
      ,[PayoutDate]
      ,[Source]
      ,[AccountNumber]
      ,[PayoutAmount]
      ,[DiscrepancyAmount]
      ,[IsAmountMatched]
      ,[InputSource]
      ,[CreatedDate]
      ,[CreatedBy]
      ,[ModifiedDate]
      ,[ModifiedBy]
	  ,[IsDeleted]
	FROM [dbo].[OwnerPayout] p
	WHERE (CONVERT(date, p.[PayoutDate]) >= @StartDate and CONVERT(date, p.[PayoutDate]) <= @EndDate) and 
		  [IsDeleted] = 0
	ORDER BY [PayoutDate] desc, [PayoutAmount] desc
  
END