CREATE PROCEDURE [dbo].[ArchiveAuditLogs]
AS
BEGIN

	DECLARE
		@ArchiveDayDistance int,
		@StartDate DateTime,
		@EndDate DateTime,
		@Id int,
		@EventDate datetime,
		@EventType nvarchar(80),
		@TableName nvarchar(80),
		@RecordId nvarchar(80),
		@ColumnName nvarchar(80),
		@OriginalValue nvarchar(max),
		@NewValue nvarchar(max),
		@AuditMessage nvarchar(max),
		@ModifiedBy nvarchar(max),
		@InsertCount int,
		@ErrorCount int

	SET @InsertCount = 0
	SET @ErrorCount = 0
	SET @ArchiveDayDistance = -91
	SET @StartDate = DateAdd(DAY, @ArchiveDayDistance, CAST(GetDate() AS DATE))
	SET @EndDate = DateAdd(DAY, 1, @StartDate)

	DECLARE LogCursor CURSOR FOR 

		SELECT [Id]
			,[EventDate]
			,[EventType]
			,[TableName]
			,[RecordId]
			,[ColumnName]
			,[OriginalValue]
			,[NewValue]
			,[AuditMessage]
			,[ModifiedBy]
		FROM  [DojoDev].[dbo].[AuditLogs]
		WHERE [EventDate] >= @StartDate and [EventDate] < @EndDate

		OPEN LogCursor

		FETCH NEXT FROM LogCursor   
		INTO @Id, @EventDate, @EventType, @TableName, @RecordId, @ColumnName, @OriginalValue, @NewValue, @AuditMessage, @ModifiedBy

		SET IDENTITY_INSERT [DojoMaintenance].[dbo].[AuditLogs] ON

		WHILE @@FETCH_STATUS = 0
		BEGIN
		BEGIN TRY
			INSERT INTO [DojoMaintenance].[dbo].[AuditLogs] 
			([Id], [EventDate], [EventType], [TableName], [RecordId], [ColumnName], [OriginalValue], [NewValue], [AuditMessage], [ModifiedBy])
			VALUES (@Id, @EventDate, @EventType, @TableName, @RecordId, @ColumnName, @OriginalValue, @NewValue, @AuditMessage, @ModifiedBy)

			DELETE FROM [DojoDev].[dbo].[AuditLogs] WHERE [Id] = @Id

			SET @InsertCount = @InsertCount + 1
		END TRY

		BEGIN CATCH
			SET @ErrorCount = @ErrorCount + 1
		END CATCH

		FETCH NEXT FROM LogCursor   
		INTO @Id, @EventDate, @EventType, @TableName, @RecordId, @ColumnName, @OriginalValue, @NewValue, @AuditMessage, @ModifiedBy
	END

	SET IDENTITY_INSERT [DojoMaintenance].[dbo].[AuditLogs] OFF

	CLOSE LogCursor 
	DEALLOCATE LogCursor

	--SELECT 'AuditLogs Insert Count' = Convert(NVARCHAR(10), @InsertCount),
	--	   'AuditLogs  Error Count' = Convert(NVARCHAR(10), @ErrorCount)
END
