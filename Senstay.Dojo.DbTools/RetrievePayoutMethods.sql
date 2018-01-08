CREATE PROCEDURE [dbo].[RetrievePayoutMethods]
AS
BEGIN

	SELECT distinct
		 [PayoutMethodId]
		,[PayoutMethodName]
		,[EffectiveDate]
		,[PayoutMethodType] = case when [PayoutMethodType] = 1 then 'Checking' else 'Paypal' end
		,[PayoutAccount] = case when [PayoutAccount] is null then '' else [PayoutAccount] end
		,'CurrentPropertyCodes' = Reverse(Stuff(Reverse((select [PropertyCode] + ',' AS 'data()'
														 from  [dbo].[PropertyPayoutMethod] p
														 where p.[PayoutMethodId] = m.[PayoutMethodId]
														 order by p.[PropertyCode]
														 FOR XML PATH(''))),1,1,''))
	FROM [dbo].[PayoutMethod] m
	ORDER BY [PayoutMethodName]

END
