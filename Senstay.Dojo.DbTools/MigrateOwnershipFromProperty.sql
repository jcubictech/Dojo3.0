CREATE PROCEDURE [dbo].[MigrateOwnershipFromProperty]
	@Month int = 6,
	@Year int = 2017
AS
BEGIN

	INSERT INTO [dbo].[PropertyOwnership] ([PropertyCode], [OwnerName], [LoginAccount], [PayoutAccount])
		SELECT [PropertyCode], [Owner], [Account], [OwnerPayout]
		FROM [dbo].[CPL]
		WHERE [PropertyCode] <> 'PropertyPlaceholder' and [PropertyStatus] <> 'Dead'

	INSERT INTO [dbo].[PropertyBalance] ([Month], [Year], [PropertyCode], [BeginningBalance], [EndingBalance], [IsFinalized])
		SELECT @Month, @Year, [PropertyCode], round([OutstandingBalance], 2), 0, 0
		FROM [dbo].[CPL]
		WHERE [PropertyCode] <> 'PropertyPlaceholder' and [PropertyStatus] <> 'Dead'

END
