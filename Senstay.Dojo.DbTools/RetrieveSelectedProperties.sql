CREATE PROCEDURE [dbo].[RetrieveSelectedProperties]
	@StartDate DateTime,
	@EndDate DateTime,
	@IsActive Bit = 1,
	@IsPending Bit = 1, 
	@IsDead Bit = 1
AS
BEGIN
	DECLARE
		@Active nvarchar(10) = 'Active%',
		@Pending nvarchar(10) = 'Pending%',
		@Inactive nvarchar(10) = 'Inactive',
		@Dead nvarchar(10) = 'Dead'

	IF @IsActive = 0 SET @Active = 'na'
	IF @IsPending = 0 SET @Pending = 'na'
	IF @IsDead = 0 SET @Inactive = 'na'
	IF @IsDead = 0 SET @Dead = 'na'

	SELECT [PropertyCode]
      ,[AirBnBHomeName]
      ,[Address]
      ,[PropertyStatus]
      ,[Vertical]
      ,[Owner]
      ,[NeedsOwnerApproval]
      ,[ListingStartDate]
      ,[StreamlineHomeName]
      ,[StreamlineUnitID]
      ,[Account]
      ,[City]
      ,[Market]
      ,[State]
      ,[Area]
      ,[Neighborhood]
      ,[BookingGuidelines]
      ,[Floor]
      ,[Bedrooms]
      ,[Bathrooms]
      ,[BedsDescription]
      ,[MaxOcc]
      ,[StdOcc]
      ,[Elevator]
      ,[A_C]
      ,[Parking]
      ,[WiFiNetwork]
      ,[WiFiPassword]
      ,[Ownership]
      ,[MonthlyRent]
      ,[DailyRent]
      ,[CleaningFees]
      ,[AIrBnBID]
      ,[AirbnbiCalexportlink]
      ,[Amenities]
      ,[Zipcode]
      ,[OldListingTitle]
      ,[GoogleDrivePicturesLink]
      ,[SquareFootage]
      ,[Password]
      ,[InquiryLeadApproval]
      ,[RevTeam2xApproval]
      ,[PendingContractDate]
      ,[PendingOnboardingDate]
      ,[SecurityDeposit]
      ,[Pool]
      ,[AirBnb]
      ,[FlipKey]
      ,[Expedia]
      ,[Inactive]
	  ,[Dead]
      ,[Currency]
      ,[CrossStreets]
      ,[SellingPoints]
      ,[CheckInType]
      ,[HomeAway]
      ,[BeltDesignation]
	  ,[ManagementFee]
	  ,[OutstandingBalance]
	  ,[OwnerEntity]
	  ,[OwnerPayout]
	  ,[PaymentEmail]
      ,[CreatedDate]
      ,[CreatedBy]
      ,[ModifiedDate]
      ,[ModifiedBy]
FROM [dbo].[CPL] p
WHERE ((CONVERT(date, dateadd(HOUR, -8, p.[CreatedDate])) >= @StartDate and CONVERT(date, dateadd(HOUR, -8, p.[CreatedDate])) <= @EndDate) or p.[CreatedDate] is null) and
	   (p.[PropertyStatus] like @Active or p.[PropertyStatus] like @Pending or p.[PropertyStatus] = @Inactive or p.[PropertyStatus] = @Dead or p.[PropertyStatus] is null)
ORDER BY p.[CreatedDate] desc, p.[PropertyCode]
END
