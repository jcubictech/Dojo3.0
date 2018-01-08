CREATE PROCEDURE [dbo].[RetrieveOtherRevenueFromTable]
	@StartDate DateTime,
	@EndDate DateTime,
	@PropertyCode NVarChar(50) = ''
AS
BEGIN
	SELECT [OtherRevenueId]
      ,[PropertyCode]
      ,[OtherRevenueDate]
      ,[OtherRevenueAmount]
      ,[OtherRevenueDescription]
      ,[IsDeleted]
      ,[ApprovalStatus]
      ,[ReviewedBy]
      ,[ReviewedDate]
      ,[ApprovedBy]
      ,[ApprovedDate]
      ,[CreatedDate]
      ,[CreatedBy]
      ,[ModifiedDate]
      ,[ModifiedBy]
	FROM [dbo].[OtherRevenue]
	WHERE (CONVERT(date, [OtherRevenueDate]) >= @StartDate and CONVERT(date, [OtherRevenueDate]) <= @EndDate) and
		   ([PropertyCode] = @PropertyCode or @PropertyCode = '') and [IsDeleted] = 0
	ORDER BY [PropertyCode], [OtherRevenueDate]
END 