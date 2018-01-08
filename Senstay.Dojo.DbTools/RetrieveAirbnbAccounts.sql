CREATE PROCEDURE [dbo].[RetrieveAirbnbAccounts]
	@StartDate DateTime,
	@EndDate DateTime
AS
BEGIN
	SELECT [Id]
      ,[Email]
      ,[Password]
      ,[Gmailpassword]
      ,'Status' = LOWER([Status])
      ,[DateAdded]
      ,[SecondaryAccountEmail]
      ,[AccountAdmin]
      ,[Vertical]
      ,[Owner_Company]
      ,[Name]
      ,[PhoneNumber1]
      ,[PhoneNumberOwner]
      ,[DOB1]
      ,[Payout_Method]
      ,[PointofContact]
      ,[PhoneNumber2]
      ,[DOB2]
      ,[EmailAddress]
      ,[ActiveListings]
      ,[Pending_Onboarding]
      ,[In_activeListings]
      ,[ofListingsinLAMarket]
      ,[ofListingsinNYCMarket]
      ,[ProxyIP]
      ,[C2ndProxyIP]
      ,[CreatedDate]
      ,[CreatedBy]
      ,[ModifiedDate]
      ,[ModifiedBy]
  FROM [dbo].[AirbnbAccount]
	WHERE (CONVERT(date, [DateAdded]) >= @StartDate and CONVERT(date, [DateAdded]) <= @EndDate) or
			[DateAdded] is null
	ORDER BY [DateAdded] desc
  
END