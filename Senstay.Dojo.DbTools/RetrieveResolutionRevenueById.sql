CREATE PROCEDURE [dbo].[RetrieveResolutionRevenueById]
	@ResolutionId int
AS
BEGIN
	SELECT distinct r.[ResolutionId]
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
		,'Reviewed' = case when r.[ApprovalStatus] < 1 then Convert(bit, 0) else Convert(bit, 1) end					
		,'Approved' = case when r.[ApprovalStatus] < 2 then Convert(bit, 0) else Convert(bit, 1) end				
	FROM [dbo].[Resolution] r
	LEFT JOIN [dbo].[Reservation] v on v.[ConfirmationCode] = r.[ConfirmationCode] and v.[PropertyCode] <> 'PropertyPlaceholder'
	INNER JOIN [dbo].[OwnerPayout] p on p.[OwnerPayoutId] = r.[OwnerPayoutId]
	WHERE [ResolutionId] = @ResolutionId and r.[IsDeleted] = 0 and (r.[InputSource] is null or r.[InputSource] not like '%_pending') 
END