CREATE PROCEDURE [dbo].[MigratePropertiesFromAuditLogs]
	@LastModifiedDate DateTime = null  -- Process Auditlog to start after this date; if null, start date is yesterday
AS
BEGIN
	IF @LastModifiedDate is NULL SET @LastModifiedDate = DateAdd(DAY, -1, CAST(GetDate() AS DATE))

	SELECT [RecordId], [EventDate], [EventType],
	   'SqlStatement' = Case
					 When a1.[EventType] = 'Modified' Then
					   'UPDATE [DojoDev].[dbo].[CPL] SET ' +
						STUFF((SELECT ', [' + a2.[ColumnName] + '] = ''' + REPLACE(LTRIM(RTRIM(a2.[NewValue])), '''', '''''') + ''''
							   FROM [Dojo2.0].[dbo].[AuditLogs] a2
							   WHERE a1.[RecordId] = a2.[RecordId] and a1.[EventDate] = a2.[EventDate] and a1.[EventType] = a2.[EventType] and a2.[ColumnName] <> 'PropertyCode'
							   FOR XML PATH(''), TYPE).value('.', 'varchar(max)')
							  ,1,2, '') +
						' WHERE [PropertyCode] = ''' + a1.[RecordId] + ''''
					 When a1.[EventType] = 'Added' AND Not Exists(SELECT 1 FROM [DojoDev].[dbo].[CPL] WHERE [PropertyCode] = a1.[RecordId]) Then
						'INSERT INTO [DojoDev].[dbo].[PropertyCode]) VALUES (''' + a1.[RecordId] + ''');' +
						'UPDATE [DojoDev].[dbo].[CPL] SET ' +
						 STUFF((SELECT ', [' + a2.[ColumnName] + '] = ''' + REPLACE(LTRIM(RTRIM(a2.[NewValue])), '''', '''''') + ''''
							FROM [Dojo2.0].[dbo].[AuditLogs] a2
							WHERE a1.[RecordId] = a2.[RecordId] and a1.[EventDate] = a2.[EventDate] and a1.[EventType] = a2.[EventType] and a2.[ColumnName] <> 'PropertyCode'
							FOR XML PATH(''), TYPE).value('.', 'varchar(max)')
							,1,2, '') +
						 ' WHERE [PropertyCode] = ''' + a1.[RecordId] + ''''
					 When a1.[EventType] = 'Added' and Exists(SELECT 1 FROM [DojoDev].[dbo].[CPL] WHERE [PropertyCode] = a1.[RecordId]) Then
						'UPDATE [DojoDev].[dbo].[CPL] SET ' +
						 STUFF((SELECT ', [' + a2.[ColumnName] + '] = ''' + REPLACE(LTRIM(RTRIM(a2.[NewValue])), '''', '''''') + ''''
							FROM [Dojo2.0].[dbo].[AuditLogs] a2
							WHERE a1.[RecordId] = a2.[RecordId] and a1.[EventDate] = a2.[EventDate] and a1.[EventType] = a2.[EventType] and a2.[ColumnName] <> 'PropertyCode'
							FOR XML PATH(''), TYPE).value('.', 'varchar(max)')
							,1,2, '') +
						 ' WHERE [PropertyCode] = ''' + a1.[RecordId] + ''''
					 When a1.[EventType] = 'Deleted' Then
						'DELETE FROM [DojoDev].[dbo].[CPL] WHERE [PropertyCode] = ''' + a1.[RecordId] + ''''
					 Else
						NULL
					 End

	INTO #Temp
		
	FROM [Dojo2.0].[dbo].[AuditLogs] a1
	WHERE [EventDate] > @LastModifiedDate and 
		  [TableName] = 'CPL' and 
		  ([EventType] = 'Added' or [EventType] = 'Modified' or [EventType] = 'Deleted')
	GROUP BY [EventDate], [RecordId], [EventType]
	ORDER BY [EventDate], [RecordId], [EventType]

	--SELECT [EventDate], [RecordId], [EventType], [SqlStatement] FROM #Temp ORDER BY [EventDate], [RecordId], [EventType]

	DECLARE @SqlStatement nvarchar(max),
			@ExecCount int = 0,
			@ErrorCount int = 0

	DECLARE SqlCursor CURSOR FOR
		SELECT [SqlStatement] FROM #Temp ORDER BY [EventDate] asc

	OPEN SqlCursor

	FETCH NEXT FROM SqlCursor INTO @SqlStatement

	WHILE @@FETCH_STATUS = 0
	BEGIN
		BEGIN TRY
				SET @SqlStatement = Replace(@SqlStatement, '[PaymentMethod]', '[OwnerEntity]')
				SET @SqlStatement = Replace(@SqlStatement, '[PaymentSource]', '[OwnerPayout]')
				exec sp_executesql @SqlStatement, N''
				SET @ExecCount = @ExecCount + 1
		END TRY

		BEGIN CATCH
			SELECT @@Error, @SqlStatement
			SET @ErrorCount = @ErrorCount + 1
		END CATCH

		FETCH NEXT FROM SqlCursor INTO @SqlStatement
	END

	CLOSE SqlCursor
	DEALLOCATE SqlCursor

END
