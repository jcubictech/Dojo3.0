CREATE PROCEDURE [dbo].[GetFavoritePage]
	@UserName nvarchar(40)
AS
BEGIN

SELECT [ClaimValue] as 'Page' FROM [dbo].[AspNetUserClaims] c
INNER JOIN [dbo].[AspNetUsers] u on u.[Id] = c.UserId and u.[UserName] = @UserName
WHERE c.[ClaimType] = 'FavoritePage'

END
