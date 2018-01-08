CREATE PROCEDURE [dbo].[GenerateInsertStatementsForOwnerPayoutBalance]
AS
BEGIN

SELECT * INTO #Temp FROM
(
	SELECT [OwnerPayout], 'OutstandingBalance' = Cast(SUM([OutstandingBalance]) as Decimal(18,2))
	FROM [dbo].[CPL]
	GROUP BY [OwnerPayout]

) AS table1

SELECT 'sqlInsert' = 'INSERT INTO [dbo].[PayoutMethodBalance] ([Month], [Year], [PayoutMethod], [BeginningBalance], [EndingBalance], [Isfinalized]) VALUES(6,2017,''' +
					 [OwnerPayout] + ''',' + Cast(Cast([OutstandingBalance] as Decimal(18,2)) as varchar(20))  + ',0,0)'	  
FROM #Temp ORDER BY [OwnerPayout]

END