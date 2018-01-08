CREATE PROCEDURE [dbo].[InitOwnerPayout]
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM [dbo].[OwnerPayout] -- will do cascade deletion for all related tables

    DBCC CHECKIDENT ('[dbo].[OwnerPayout]', RESEED, 0)
	DBCC CHECKIDENT ('[dbo].[Reservation]', RESEED, 0)
	DBCC CHECKIDENT ('[dbo].[Resolution]', RESEED, 0)

	DECLARE @PropertyPlaceholder varchar(50) = 'PropertyPlaceholder'

	DELETE FROM [dbo].[InputError]

	IF NOT EXISTS (SELECT 1 FROM [dbo].[CPL] WHERE [PropertyCode] = @PropertyPlaceholder)
		INSERT INTO [dbo].[CPL] ([PropertyCode], [PropertyStatus]) VALUES (@PropertyPlaceholder, 'Dead')

	SET IDENTITY_INSERT [dbo].[OwnerPayout] ON

	IF NOT EXISTS (SELECT 1 FROM [dbo].[OwnerPayout] WHERE [OwnerPayoutId] = 0)
		INSERT INTO [dbo].[OwnerPayout]		
				   ([OwnerPayoutId]
				   ,[PayoutAmount]
				   ,[PayoutDate]
				   ,[IsAmountMatched]
				   ,[DiscrepancyAmount]
				   ,[CreatedDate]
				   ,[CreatedBy]
				   ,[ModifiedDate]
				   ,[ModifiedBy]
				   ,[Source]
				   ,[AccountNumber]
				   ,[InputSource]
				   ,[IsDeleted])
			 VALUES
				   (0
				   ,0
				   ,'2017-01-01'
				   ,Cast(0 as bit)
				   ,0
				   ,'2017-01-01'
				   ,''
				   ,'2017-01-01'
				   ,''
				   ,'Airbnb'
				   ,''
				   ,''
				   ,0)

	SET IDENTITY_INSERT [dbo].[OwnerPayout] OFF

	SET IDENTITY_INSERT [dbo].[Reservation] ON

	IF NOT EXISTS (SELECT 1 FROM [dbo].[Reservation] WHERE [ReservationId] = 0)
		INSERT INTO [dbo].[Reservation]
				   ([ReservationId]
				   ,[OwnerPayoutId]
				   ,[PropertyCode]
				   ,[ConfirmationCode]
				   ,[TransactionDate]
				   ,[CheckinDate]
				   ,[Nights]
				   ,[GuestName]
				   ,[Reference]
				   ,[Currency]
				   ,[TotalRevenue]
				   --,[HostFee]
				   --,[CleanFee]
				   ,[Source]
				   ,[CreatedDate]
				   ,[CreatedBy]
				   ,[ModifiedDate]
				   ,[ModifiedBy]
				   ,[InputSource]
				   ,[ApprovalStatus]
				   ,[IncludeOnStatement]
				   ,[IsFutureBooking]
				   ,[IsDeleted])
			 VALUES
				   (0
				   ,0
				   ,'PropertyPlaceholder'
				   ,''
				   ,'2017-01-01'
				   ,'2017-01-01'
				   ,1
				   ,''
				   ,''
				   ,1
				   ,0
				   ,''
				   ,'2017-01-01'
				   ,''
				   ,'2017-01-01'
				   ,''
				   ,''
				   ,0
				   ,0
				   ,0
				   ,0)

	SET IDENTITY_INSERT [dbo].[Reservation] OFF

	SET IDENTITY_INSERT [dbo].[Expense] ON;
	insert into [dbo].[Expense] ([ExpenseId], [ReservationId],[ExpenseAmount], [CreatedDate], [ModifiedDate], [ApprovalStatus], [IncludeOnStatement], [IsDeleted], [ParentId])
				  values(0, 0, 0, getdate(), getdate(), 0, 0, 1, 0)
	SET IDENTITY_INSERT [dbo].[Expense] OFF;

END
