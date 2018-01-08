CREATE PROCEDURE [dbo].[RetrieveOwnerPayoutRevenue]
	@StartDate DateTime,
	@EndDate DateTime,
	@Source NVARCHAR(50)
AS
BEGIN
	SELECT * INTO #Temp FROM
	(	
		SELECT p.[OwnerPayoutId]
			  ,p.[Source]
			  ,[PayoutDate]
			  ,'PayToAccount' = [AccountNumber]
			  ,[PayoutAmount]
			  --,[IsAmountMatched]
			  --,'DiscrepancyAmount' = Round(p.[DiscrepancyAmount], 2)
			  ,p.[InputSource]
			  ,'ReservationTotal' = case
										when Round((select sum([TotalRevenue]) from [dbo].[Reservation] rr where rr.[OwnerPayoutId] = p.[OwnerPayoutId] and rr.[IsDeleted] = 0), 2) is null
										then 0
										else Round((select sum([TotalRevenue]) from [dbo].[Reservation] rr where rr.[OwnerPayoutId] = p.[OwnerPayoutId] and rr.[IsDeleted] = 0), 2)
									end
			  ,'ResolutionTotal' = case
										when Round((select sum([ResolutionAmount]) from [dbo].[Resolution] rr where rr.[OwnerPayoutId] = p.[OwnerPayoutId] and rr.[IsDeleted] = 0), 2) is null
										then 0
										else Round((select sum([ResolutionAmount]) from [dbo].[Resolution] rr where rr.[OwnerPayoutId] = p.[OwnerPayoutId] and rr.[IsDeleted] = 0), 2)
									end
			  ,'RevenueType' = 'Reservation'
			  ,r.[ConfirmationCode]
			  ,r.[CheckinDate]
			  ,r.[Nights]
			  ,'PropertyCode' = case when r.[PropertyCode] = 'PropertyPlaceholder' or r.[PropertyCode] is null  then '' else r.[PropertyCode] end
			  ,'Amount' = r.[TotalRevenue]
			  ,'ChildId' = r.[ReservationId]
		FROM [dbo].[OwnerPayout] p
		LEFT JOIN [dbo].[Reservation] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		where (Convert(Date, [PayoutDate]) >= Convert(Date, @StartDate) and Convert(Date, [PayoutDate]) <= Convert(Date, @EndDate)) and
				p.[OwnerPayoutId] > 0 and p.[IsDeleted] = 0 and
				(p.[Source] = @Source or (@source = '' and p.[IsAmountMatched] = 0))

		UNION

		SELECT p.[OwnerPayoutId]
			  ,p.[Source]
			  ,[PayoutDate]
			  ,'PayToAccount' = [AccountNumber]
			  ,[PayoutAmount]
			  --,[IsAmountMatched]
			  --,'DiscrepancyAmount' = Round(p.[DiscrepancyAmount], 2)
			  ,p.[InputSource]
			  ,'ReservationTotal' = case
										when Round((select sum([TotalRevenue]) from [dbo].[Reservation] rr where rr.[OwnerPayoutId] = p.[OwnerPayoutId] and rr.[IsDeleted] = 0), 2) is null
										then 0
										else Round((select sum([TotalRevenue]) from [dbo].[Reservation] rr where rr.[OwnerPayoutId] = p.[OwnerPayoutId] and rr.[IsDeleted] = 0), 2)
									end
			  ,'ResolutionTotal' = case 
										when Round((select sum([ResolutionAmount]) from [dbo].[Resolution] rr where rr.[OwnerPayoutId] = p.[OwnerPayoutId] and rr.[IsDeleted] = 0), 2) is null
										then 0
										else Round((select sum([ResolutionAmount]) from [dbo].[Resolution] rr where rr.[OwnerPayoutId] = p.[OwnerPayoutId] and rr.[IsDeleted] = 0), 2)
									end
			  ,'RevenueType' = r.[ResolutionType]
			  ,'ConfirmationCode' = v.[ConfirmationCode]
			  ,v.[CheckinDate]
			  ,v.[Nights]
			  ,'PropertCode' = case when (v.[PropertyCode] = 'PropertyPlaceholder' or v.[PropertyCode] is null) and r.[PropertyCode] is null then '' 
									when r.[PropertyCode] is not null and r.[PropertyCode] <> '' then r.[PropertyCode]
									else v.[PropertyCode] 
							   end
			  ,'Amount' = r.[ResolutionAmount]
			  ,'ChildId' = r.[ResolutionId]
		FROM [dbo].[OwnerPayout] p
		INNER JOIN [dbo].[Resolution] r on r.[OwnerPayoutId] = p.[OwnerPayoutId] and r.[IsDeleted] = 0
		LEFT JOIN [dbo].[Reservation] v on v.[ConfirmationCode] = r.[ConfirmationCode] and v.[ReservationId] > 0 and v.[IsDeleted] = 0
		where (Convert(Date, [PayoutDate]) >= Convert(Date, @StartDate) and Convert(Date, [PayoutDate]) <= Convert(Date, @EndDate)) and
				p.[OwnerPayoutId] > 0 and p.[IsDeleted] = 0 and 
				(p.[Source] = @Source or (@source = '' and p.[IsAmountMatched] = 0))
	) AS OwnerPayoutRevenue

	SELECT *
		  ,'IsAmountMatched' =  case 
									when abs([ReservationTotal] + [ResolutionTotal] - [PayoutAmount]) < 0.01
									then Cast(1 as bit)
									else Cast(0 as bit)
								end
		  ,'DiscrepancyAmount' = case 
									when abs([ReservationTotal] + [ResolutionTotal] - [PayoutAmount]) > 0.01
									then [ReservationTotal] + [ResolutionTotal] - [PayoutAmount]
									else 0
								end
	FROM #Temp
	WHERE [Source] = @source or (@source = '' and abs([ReservationTotal] + [ResolutionTotal] - [PayoutAmount]) > 0.01)
	ORDER BY [PayoutDate], [PropertyCode]
END