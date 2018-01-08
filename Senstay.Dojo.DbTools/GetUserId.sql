CREATE PROCEDURE [dbo].[GetUserId]
	@UserName nvarchar(40)
AS
BEGIN
	SELECT [Id] as 'userId' FROM [dbo].[AspNetUsers] u
	WHERE u.[UserName] = @UserName or u.[Email] = @UserName
END

