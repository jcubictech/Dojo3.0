CREATE PROCEDURE [dbo].[GetPropertiesForOwnerSummary]
	@PaymentMethod nvarchar(50)
AS
BEGIN
	SELECT
		 p.[PropertyCode]
		,'OwnerName' = p.[Owner]
		,p.[Vertical]
		,'Address' = Replace(Replace(Replace(p.[Address], 'COMBO:', ''), 'ROOM:', ''), 'MULTI:', '')
	FROM [dbo].[CPL] p 
	INNER JOIN [dbo].[PropertyPayoutMethod] ppm on ppm.[PropertyCode] = p.[PropertyCode]
	INNER JOIN [dbo].[PayoutMethod] m on m.[PayoutMethodId] = ppm.[PayoutMethodId] and m.[PayoutMethodName] = @PaymentMethod
	WHERE (p.[PropertyStatus] like 'Active%' or [PropertyStatus] = 'Inactive' or [PropertyStatus] = 'Pending-Onboarding')
	ORDER BY [PropertyCode]
END
