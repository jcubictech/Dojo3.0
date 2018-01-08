CREATE PROCEDURE [dbo].[RetrievePropertyEntities]
AS
BEGIN

	SELECT distinct
		 e.[PropertyEntityId]
		,[EntityName]
		,[EffectiveDate]
		,'CurrentPropertyCodes' = Reverse(Stuff(Reverse((select [PropertyCode] + ',' AS 'data()'
									from [dbo].[PropertyCodePropertyEntity] p 
									where p.[PropertyEntityId] = e.[PropertyEntityId]
									order by p.[PropertyCode]
									FOR XML PATH(''))),1,1,''))
	FROM [dbo].[PropertyEntity] e
	ORDER BY [EntityName]

END
