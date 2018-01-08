CREATE PROCEDURE [dbo].[RetrieveInquiries]
	@StartDate DateTime,
	@EndDate DateTime
AS
BEGIN
	SELECT [Id]
      ,[GuestName]
      ,[InquiryTeam]
      ,c.[AirBnBHomeName] as 'AirBnBListingTitle'
      ,i.[PropertyCode]
      ,[Channel]
      ,c.[BookingGuidelines]
      ,[AdditionalInfo_StatusofInquiry]
      ,c.[AirBnb] as 'Airbnb URL'
      ,c.[Bedrooms]
      ,c.[Account]
      ,[InquiryDate]
      ,[InquiryTime__PST_]
      ,[Check_inDate]
      ,[Check_InDay]
      ,[Check_outDate]
      ,[Check_OutDay]
      ,[TotalPayout]
      ,[Weekdayorphandays]
      ,[DaysOut]
      ,[DaysOutPoints]
      ,[LengthofStay]
      ,[LengthofStayPoints]
      ,[OpenWeekdaysPoints]
      ,[NightlyRate]
      ,[Cleaning_Fee]
      ,[Doesitrequire2pricingteamapprovals]
      ,[TotalPoints]
      ,[OwnerApprovalNeeded]
      ,[ApprovedbyOwner]
      ,[Approvedby2PricingTeamMember]
      ,[PricingApprover1]
      ,[PricingDecision1]
      ,[PricingReason1]
      ,[PricingApprover2]
      ,[PricingDecision2]
      ,[PricingReason2]
      ,[PricingTeamTimeStamp]
      ,[InquiryAge]
      ,[Daystillcheckin]
      ,'InquiryCreatedTimestamp' = DateAdd(Hour, -8, [InquiryCreatedTimestamp])
	  ,c.[Market]
	  ,c.[BeltDesignation]
      ,i.[CreatedDate]
      ,i.[CreatedBy]
      ,i.[ModifiedDate]
      ,i.[ModifiedBy]
	FROM [dbo].[InquiriesValidation] i
	INNER JOIN [dbo].[CPL] c on c.[PropertyCode] = i.[PropertyCode]
	WHERE (CONVERT(date, dateadd(HOUR, -8, i.[InquiryCreatedTimestamp])) >= @StartDate and 
		   CONVERT(date, dateadd(HOUR, -8, i.[InquiryCreatedTimestamp])) <= @EndDate)
	ORDER BY [Id] desc, [InquiryCreatedTimestamp] desc
  
END
