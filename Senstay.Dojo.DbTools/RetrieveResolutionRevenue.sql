CREATE PROCEDURE [dbo].[RetrieveResolutionRevenue]
	@StartDate DATETIME,
	@EndDate DATETIME,
	@PropertyCode NVARCHAR(50) -- not use
AS
BEGIN
	SELECT distinct 
		 r.[ResolutionId]
		,r.[OwnerPayoutId]
		,'PropertyCode' = case when r.[PropertyCode] is not null and r.[PropertyCode] <> '' then r.[PropertyCode] else v.[PropertyCode] end
		,r.[ResolutionDate]
		,r.[ConfirmationCode]
		,r.[ResolutionType]
		,r.[ResolutionDescription]
		,r.[Impact]
		,r.[Cause]
		,r.[ResolutionAmount]
		,p.[Source]
		,r.[IncludeOnStatement]
		,r.[ApprovedNote]
		,r.[ApprovalStatus]
		,'Product' = (select Top 1 [Vertical] from [dbo].[CPL] where [PropertyCode] = v.[PropertyCode])
		,'PayToAccount' = RTrim(Replace(p.[AccountNumber], '(USD)', ''))
		,'Reviewed' = case when r.[ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Approved' = case when r.[ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end				
	FROM [dbo].[Resolution] r
	LEFT JOIN [dbo].[Reservation] v on v.[ConfirmationCode] = r.[ConfirmationCode] and v.[PropertyCode] <> 'PropertyPlaceholder'
	INNER JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId]
	WHERE ((CONVERT(date, [ResolutionDate]) >= CONVERT(date, @StartDate) and CONVERT(date, [ResolutionDate]) <= CONVERT(date, @EndDate)) or [ResolutionDate] is null) and
		   [ResolutionId] > 0 and (r.[InputSource] not like '%_pending' or r.[InputSource] is null) and 
		   --[PropetyCode] <> 'PropertyPlaceholder'
		   r.[IsDeleted] = 0
	ORDER BY [ResolutionDate], [ResolutionType]
END