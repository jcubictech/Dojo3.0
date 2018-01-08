CREATE PROCEDURE [dbo].[InitOwnerPaymentTables]
AS
BEGIN
	--delete from [dbo].[PayoutMethod]
	--DBCC CHECKIDENT ('[dbo].[PayoutMethod]', RESEED, 0)
	--insert into [dbo].[PayoutMethod] ([PayoutMethodName], [PayoutMethodType], [BeginBalance], [EffectiveDate])
	--select distinct [OwnerPayout], 1, 0, '2017-07-01 11:00:00' from [dbo].[CPL] where [PropertyStatus] <> 'Dead' and [OwnerPayout] is not null

	--insert into [dbo].[PropertyPayoutMethod] ([PropertyCode], [PayoutMethodId])
	--select [PropertyCode]
	--	   ,'PayoutMethodId' = (select Top 1 [PayoutMethodId] from [dbo].[PayoutMethod] m where m.[PayoutMethodName] = p.[OwnerPayout])
	--from [dbo].[CPL] p where [PropertyStatus] <> 'Dead' and [OwnerPayout] is not null

	--insert into [dbo].[PayoutEntity] ([PayoutEntityName], [OwnerContact], [LoginAccount], [EffectiveDate])
	--select distinct [OwnerEntity], [Owner], [Account], '2017-07-01 11:00:00' from [dbo].[CPL] where [PropertyStatus] <> 'Dead' and [OwnerEntity] is not null

	--insert into [dbo].[PropertyPayoutEntity] ([PropertyCode], [PayoutEntityId])
	--select [PropertyCode]
	--	   ,'PayoutEntityId' = (select Top 1 [PayoutEntityId] from [dbo].[PayoutEntity] e where e.[PayoutEntityName] = p.[OwnerEntity])
	--from [dbo].[CPL] p where [PropertyStatus] <> 'Dead' and [OwnerEntity] is not null

	--insert into [dbo].[PayoutPayment] ([PaymentAmount], [PaymentDate], [PayoutMethodId])
	--select [Amount]
	--	  ,'PaymentDate' = Cast([Year] as nvarchar(4)) + '-' + Cast([Month] as nvarchar(2)) + '-15 11:00:00'
	--	  ,'PayoutMethodId' = (select Top 1 [PayoutMethodId] from [dbo].[PayoutMethod] m where m.[PayoutMethodName] = h.[PayoutMethod])
	--from [dbo].[PayoutHistory] h where [IsFinalized] = 1

	SELECT m.[PayoutMethodId]
		,[PayoutMethodName]
		,[PropertyCode]
		,[PayoutAccount]
		,[PayoutMethodType]
		,[BeginBalance]
		,[EffectiveDate]
	FROM [dbo].[PayoutMethod] m
	inner join [dbo].[PropertyPayoutMethod] p on p.[PayoutMethodId] = m.[PayoutMethodId]
	order by [PropertyCode], [PayoutMethodName]

	SELECT e.[PayoutEntityId]
		,[PropertyCode]
		,[OwnerContact]
		,[LoginAccount]
		,[EffectiveDate]
		,[PayoutEntityName]
	FROM [dbo].[PayoutEntity] e
	inner join [dbo].[PropertyPayoutEntity] p on p.[PayoutEntityId] = e.[PayoutEntityId]
	order by [PropertyCode], [PayoutEntityName]

END
