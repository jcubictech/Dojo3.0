SELECT [ConfirmationCode], COUNT(*)
FROM [dbo].[Reservation]
WHERE [InputSource] not like 'June 2 2017%'
GROUP BY [ConfirmationCode]
HAVING COUNT(*) > 1
