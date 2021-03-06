CREATE PROCEDURE [dbo].[AddOrUpdateInquiriesFromAuditLogs]
	@LastModifiedDate DateTime  -- Process Auditlog to start after this date
AS
BEGIN
	DECLARE @sql nvarchar(1000),
			@Key nvarchar(50),
			@ColumnName nvarchar(50),
			@NewValue nvarchar(max),
			@ModifiedBy nvarchar(50),
			@ModifiedDate nvarchar(50),
			@InsertCount int = 0,
			@UpdateCount int = 0,
			@ErrorCount int = 0

	-- ========================================================================================
	--   PROCESS [EventDate] THAT STARTS AFTER THE LATEST [ModifiedDate] IN CPL TABLE
	-- ========================================================================================

	DECLARE LogCursor CURSOR FOR
		SELECT [RecordId], [ColumnName], [NewValue], [ModifiedBy], [EventDate]
		FROM [Dojo2.0].[dbo].[AuditLogs]
		WHERE [EventDate] > @LastModifiedDate and 
			  [TableName] = 'InquiriesValidation' and 
			  ([EventType] = 'Added' or [EventType] = 'Modified')
		ORDER BY [EventDate] asc

	OPEN LogCursor

	FETCH NEXT FROM LogCursor   
	INTO @Key, @ColumnName, @NewValue, @ModifiedBy, @ModifiedDate
	
	CREATE Table #Temp ( Line Varchar(1000) )

	SET IDENTITY_INSERT [DojoDev].[dbo].[InquiriesValidation] ON	
	WHILE @@FETCH_STATUS = 0
	BEGIN
		BEGIN TRY
			IF Not Exists (SELECT 1 FROM [DojoDev].[dbo].[InquiriesValidation] WHERE [Id] = @Key)
			BEGIN
				INSERT INTO [DojoDev].[dbo].[InquiriesValidation] ([Id], [GuestName], [InquiryTeam], [PropertyCode], [Channel]) 
				VALUES (Cast(@Key as int), '', '', 'NY101', '')
				INSERT INTO #Temp (Line) VALUES('INSERT INTO [DojoDev].[dbo].[InquiriesValidation] ([Id], [GuestName], [InquiryTeam], [PropertyCode], [Channel]) VALUES (' + @Key + ', '''', '''', ''NY101'', '''')')
				SET @InsertCount = @InsertCount + 1
			END

			IF @ColumnName <> 'Id'
			BEGIN
				-- need to use dynamic SQL as columns name is a variable
				SET @sql = 'UPDATE [DojoDev].[dbo].[InquiriesValidation] SET ' + @ColumnName + ' = ''' + Replace(@NewValue, '''', '''''') + ''' WHERE [Id] = ' + @Key
				exec sp_executesql @sql, N''

				IF @NewValue is NULL
					INSERT INTO #Temp (Line) VALUES('UPDATE [DojoDev].[dbo].[InquiriesValidation] SET [' + @ColumnName + '] = NULL WHERE [Id] = ' + @Key)
				ELSE
					INSERT INTO #Temp (Line) VALUES('UPDATE [DojoDev].[dbo].[InquiriesValidation] SET [' + @ColumnName + '] = ''' + REPLACE(LTRIM(RTRIM(@NewValue)), '''', '''''') + ''' WHERE [Id] = ' + @Key)

				SET @UpdateCount = @UpdateCount + 1
			END
		END TRY

		BEGIN CATCH
			SELECT @@Error, @Key, @ColumnName, @NewValue, @ModifiedBy, @ModifiedDate
			--INSERT INTO #Temp (Line) VALUES('Error: ' + Cast(@@Error as varchar(20)) + 'for Inquiry ID = ' + @Key + + ', Column = ' + @ColumnName + ', Modified Date = ' + @ModifiedDate)
			SET @ErrorCount = @ErrorCount + 1
		END CATCH

		FETCH NEXT FROM LogCursor   
		INTO @Key, @ColumnName, @NewValue, @ModifiedBy, @ModifiedDate
	END

	CLOSE LogCursor 
	DEALLOCATE LogCursor

	SET IDENTITY_INSERT [DojoDev].[dbo].[InquiriesValidation] OFF	

	SELECT 'Inquiry Insert Count' = Convert(NVARCHAR(10), @InsertCount),
		   'Inquiry Update Count' = Convert(NVARCHAR(10), @UpdateCount),
		   'Inquiry  Error Count' = Convert(NVARCHAR(10), @ErrorCount)

	SELECT * FROM #Temp WHERE [Line] is not NULL
	Drop Table #Temp
END
