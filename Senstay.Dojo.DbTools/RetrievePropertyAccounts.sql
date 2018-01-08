CREATE PROCEDURE [dbo].[RetrievePropertyAccounts]
AS
BEGIN

	SELECT distinct
		 a.[PropertyAccountId]
		,[LoginAccount]
		,[OwnerName]
		,[OwnerEmail]
		,'CurrentPayoutMethodIds' = Reverse(Stuff(Reverse((select Convert(nvarchar,m.PayoutMethodId) + ',' AS 'data()'
									from [dbo].[PropertyAccountPayoutMethod] p 
									inner join [dbo].[PayoutMethod] m on m.[PayoutMethodId] = p.[PayoutMethodId]
									where p.[PropertyAccountId] = a.[PropertyAccountId]
									order by m.[PayoutMethodId]
									FOR XML PATH(''))),1,1,''))
	FROM [dbo].[PropertyAccount] a
	INNER JOIN [dbo].[PropertyAccountPayoutMethod] p on p.[PropertyAccountId] = a.[PropertyAccountId]
	INNER JOIN [dbo].[PayoutMethod] m on m.[PayoutMethodId] = p.[PayoutMethodId]
	ORDER BY [OwnerName]

END
