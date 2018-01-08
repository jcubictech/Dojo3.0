CREATE PROCEDURE [dbo].[GeneratePropertyTitleChangesFromAuditLogs]
	@LastModifiedDate DateTime  -- Process Auditlog to start after this date
AS
BEGIN
	DECLARE @sql nvarchar(1000),
			@PropertyCode nvarchar(50),
			@ColumnName nvarchar(50),
			@NewValue nvarchar(max),
			@ModifiedBy nvarchar(50),
			@ModifiedDate nvarchar(50),
			@InsertCount int = 0,
			@UpdateCount int = 0,
			@ErrorCount int = 0,
			@LastPropertyCode nvarchar(50) = ';'

	-- ========================================================================================
	--   PROCESS [EventDate] THAT STARTS AFTER THE LATEST [ModifiedDate] IN CPL TABLE
	-- ========================================================================================

	DECLARE LogCursor CURSOR FOR
		SELECT [RecordId], [ColumnName], [NewValue], [ModifiedBy], [EventDate]
		FROM [Dojo2.0].[dbo].[AuditLogs]
		WHERE [EventDate] > @LastModifiedDate and [TableName] = 'CPL' and ([EventType] = 'Added' or [EventType] = 'Modified')
		ORDER BY [EventDate] asc

	OPEN LogCursor

	FETCH NEXT FROM LogCursor   
	INTO @PropertyCode, @ColumnName, @NewValue, @ModifiedBy, @ModifiedDate
	
	CREATE Table #Temp ( Line Varchar(1000) )
		
	WHILE @@FETCH_STATUS = 0
	BEGIN
		BEGIN TRY
			IF CHARINDEX(';' + @PropertyCode + ';', @LastPropertyCode) <= 0 and @ColumnName = 'AirBnBHomeName' and @NewValue is not null and  
			   (Select Count([PropertyCode]) From [DojoDev].[dbo].[PropertyTitleHistory] Where [PropertyTitle] <> @NewValue) = 0
			BEGIN
				SET @LastPropertyCode = @LastPropertyCode + ';' + @PropertyCode + ';'
				INSERT INTO #Temp (Line) VALUES('INSERT INTO [dbo].[PropertyTitleHistory] ([PropertyCode], [PropertyTitle], [EffectiveDate]) VALUES (''' + @PropertyCode + ''',''' + @NewValue + ''',''' + @ModifiedDate + ''')')
				SET @InsertCount = @InsertCount + 1
			END
		END TRY

		BEGIN CATCH
			SELECT @@Error, @PropertyCode, @ColumnName, @NewValue, @ModifiedBy, @ModifiedDate
			INSERT INTO #Temp (Line) VALUES('Error: ' + @@Error + 'for Property = ' + @PropertyCode + + ', Column = ' + @ColumnName + ', Modified Date = ' + @ModifiedDate)
			SET @ErrorCount = @ErrorCount + 1
		END CATCH

		FETCH NEXT FROM LogCursor   
		INTO @PropertyCode, @ColumnName, @NewValue, @ModifiedBy, @ModifiedDate
	END

	CLOSE LogCursor 
	DEALLOCATE LogCursor

	SELECT 'Property Title Insert Count' = Convert(NVARCHAR(10), @InsertCount),
		   'Property Title Update Count' = Convert(NVARCHAR(10), @UpdateCount),
		   'Property Title Error Count' = Convert(NVARCHAR(10), @ErrorCount)

	SELECT * FROM #Temp WHERE [Line] is not NULL
	Drop Table #Temp
END