CREATE PROCEDURE [dbo].[RetrieveResolutions]
	@StartDate DateTime,
	@EndDate DateTime
AS
BEGIN
	SELECT [ResolutionId]
      ,[OwnerPayoutId]
      ,[ResolutionDate]
      ,[ConfirmationCode]
      ,[ResolutionType]
      ,[ResolutionDescription]
      ,[ResolutionAmount]
      ,[IncludeOnStatement]
      ,[Impact]
      ,[PropertyCode]
      ,[ApprovalStatus]
      ,[ReviewedBy]
      ,[ReviewedDate]
      ,[ApprovedBy]
      ,[ApprovedDate]
      ,[ApprovedNote]
      ,[FinalizedBy]
      ,[FinalizedDate]
      ,[ClosedBy]
      ,[ClosedDate]
      ,[InputSource]
      ,[CreatedDate]
      ,[CreatedBy]
      ,[ModifiedDate]
      ,[ModifiedBy]
      ,[IsDeleted]
	FROM [dbo].[Resolution]
	WHERE (CONVERT(date, [ResolutionDate]) >= @StartDate and CONVERT(date, [ResolutionDate]) <= @EndDate) or
			[ResolutionDate] is null and [IsDeleted] = 0
	ORDER BY [ResolutionDate] desc
END