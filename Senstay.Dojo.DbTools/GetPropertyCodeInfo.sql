CREATE PROCEDURE [dbo].[GetPropertyCodeInfo]
AS
BEGIN

	SELECT [PropertyCode]
		,[CityTax]
		,[DamageWaiver]
		,[ManagementFee]
		,ROW_NUMBER() OVER(PARTITION BY [PropertyCode] ORDER BY [PropertyCode], [EntryDate] desc) 'rank'
	INTO #Fee
	FROM [dbo].[PropertyFee]

	SELECT 
		p.[PropertyCode]
		,f.[CityTax]
		,f.[DamageWaiver]
		,f.[ManagementFee]
		,[PayoutMethod] = (select top 1 m.[PayoutMethodName] from [dbo].[PropertyPayoutMethod] pm
															 inner join [dbo].[PayoutMethod] m on m.[PayoutMethodId] = pm.[PayoutMethodId] and m.[IsDeleted] = 0
															  where pm.[PropertyCode] = p.[PropertyCode])
		,[PayoutEntity] = (select top 1 e.[EntityName] from [dbo].[PropertyCodePropertyEntity] pe
													   inner join [dbo].[PropertyEntity] e on e.[PropertyEntityId] = pe.[PropertyEntityId]
													   where pe.[PropertyCode] = p.[PropertyCode])
	INTO #Temp
	FROM [dbo].[CPL] p
	left join #Fee f on f.[PropertyCode] = p.[PropertyCode] and [Rank] = 1
	WHERE p.[PropertyStatus] <> 'Dead'
	ORDER BY p.[PropertyCode]

	SELECT
		[PropertyCode]
		,[CityTax] = case when [CityTax] is null then 0 else [CityTax] end
		,[DamageWaiver]= case when [DamageWaiver] is null then 0 else [DamageWaiver] end
		,[ManagementFee]= case when [ManagementFee] is null then 0 else [ManagementFee] end
		,[PropertyOwner] = (select top 1 a.[OwnerName] from [dbo].[PropertyAccount] a 
													   inner join [dbo].[PropertyAccountPayoutMethod] am on am.[PropertyAccountId] = a.[PropertyAccountId]
													   inner join [dbo].[PayoutMethod] m on m.[PayoutMethodId] = am.[PayoutMethodId]
													   where m.[PayoutMethodName] = [PayoutMethod])
		,[PayoutMethod]
		,[PayoutEntity]
	FROM #Temp
	ORDER BY [PropertyCode]
END

