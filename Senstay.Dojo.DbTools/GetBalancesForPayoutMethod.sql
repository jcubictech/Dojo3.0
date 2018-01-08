CREATE PROCEDURE [dbo].[GetBalancesForPayoutMethod]
	@Month int,
	@Year int,
	@PayoutMehtodId int
AS
BEGIN

	SELECT s.[PropertyCode]
      ,[Month]
      ,[Year]
      ,[BeginningBalance] = s.[Balance]
	  ,'AdjustedBalance' = Cast(0 as float)
	  ,s.[IsSummary]
	FROM [dbo].[OwnerStatement] s
	INNER JOIN [dbo].[PayoutMethod] m on m.[PayoutMethodId] = @PayoutMehtodId
	INNER JOIN [dbo].[PropertyPayoutMethod] p on p.[PayoutMethodId] = m.[PayoutMethodId] and p.[PropertyCode] = s.[PropertyCode]
	WHERE s.[Month] = @Month and s.[Year] = @Year

	UNION

	SELECT s.[PropertyCode]
      ,[Month]
      ,[Year]
      ,[BeginningBalance] = s.[Balance]
	  ,'AdjustedBalance' = Cast(0 as float)
	  ,s.[IsSummary]
	FROM [dbo].[OwnerStatement] s
	INNER JOIN [dbo].[PayoutMethod] m on m.[PayoutMethodId] = @PayoutMehtodId and s.[PropertyCode] = m.[PayoutMethodName]
	WHERE s.[Month] = @Month and s.[Year] = @Year and s.[IsSummary] = 1

END