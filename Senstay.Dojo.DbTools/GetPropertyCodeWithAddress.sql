CREATE PROCEDURE [dbo].[GetPropertyCodeWithAddress]
	@TableName nvarchar(50),
	@StartDate DateTime = '2016-01-01',
	@EndDate DateTime = '2050-12-31'
AS
BEGIN

	IF @TableName = 'Reservation'

	BEGIN
		SELECT distinct
			p.[PropertyCode]
			,'PropertyCodeAndAddress' = p.[PropertyCode] + (case when p.[Address] is null then '' else ' | ' +  p.[Address] end)
			,'AllReviewed' = case
								when (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[ApprovalStatus] < 1 and r.[IsDeleted] = 0 and 
									  ((r.[Channel] = 'Airbnb' and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) or
									   (r.[Channel] <> 'Airbnb' and Convert(date,r.[CheckinDate]) >= @StartDate and Convert(date,r.[CheckinDate]) <= @EndDate))) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and 
									  ((r.[Channel] = 'Airbnb' and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) or
									   (r.[Channel] <> 'Airbnb' and Convert(date,r.[CheckinDate]) >= @StartDate and Convert(date,r.[CheckinDate]) <= @EndDate))) > 0
								then 0
							 else
								1
							 end
			,'AllApproved' = case
								when (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[ApprovalStatus] < 2 and r.[IsDeleted] = 0 and 
									  ((r.[Channel] = 'Airbnb' and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) or
									   (r.[Channel] <> 'Airbnb' and Convert(date,r.[CheckinDate]) >= @StartDate and Convert(date,r.[CheckinDate]) <= @EndDate))) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and 
									  ((r.[Channel] = 'Airbnb' and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) or
									   (r.[Channel] <> 'Airbnb' and Convert(date,r.[CheckinDate]) >= @StartDate and Convert(date,r.[CheckinDate]) <= @EndDate))) > 0
								then 0
							 else
								1
							 end
			,'Finalized' = case
								when (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[ApprovalStatus] < 3 and r.[IsDeleted] = 0 and 
									  ((r.[Channel] = 'Airbnb' and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) or
									   (r.[Channel] <> 'Airbnb' and Convert(date,r.[CheckinDate]) >= @StartDate and Convert(date,r.[CheckinDate]) <= @EndDate))) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and 
									  ((r.[Channel] = 'Airbnb' and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) or
									   (r.[Channel] <> 'Airbnb' and Convert(date,r.[CheckinDate]) >= @StartDate and Convert(date,r.[CheckinDate]) <= @EndDate))) > 0
								then 0
							 else
								1
							 end
			,'Empty' = case
							when (select count(r.[PropertyCode]) from [dbo].[Reservation] r 
								  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and 
								  ((r.[Channel] = 'Airbnb' and Convert(date,r.[TransactionDate]) >= @StartDate and Convert(date,r.[TransactionDate]) <= @EndDate) or
								   (r.[Channel] <> 'Airbnb' and Convert(date,r.[CheckinDate]) >= @StartDate and Convert(date,r.[CheckinDate]) <= @EndDate))) = 0 
							then 
								1
							else 
								0
					   end
		FROM [dbo].[CPL] p
		WHERE (p.[PropertyStatus] like 'Active%' or p.[PropertyStatus] = 'Inactive' or p.[PropertyStatus] = 'Pending-Onboarding') and
			  p.[PropertyCode] <> 'PropertyPlaceholder'
		ORDER BY [PropertyCode]
	END

	ELSE IF (@TableName = 'Expense')
	BEGIN
		SELECT distinct
			p.[PropertyCode]
			,'PropertyCodeAndAddress' = p.[PropertyCode] + (case when p.[Address] is null then '' else ' | ' +  p.[Address] end)
			,'AllReviewed' = case
								when (select count(e.[PropertyCode]) from [dbo].[Expense] e 
									  where p.[PropertyCode] = e.[PropertyCode] and e.[ApprovalStatus] < 1 and e.[IsDeleted] = 0 and e.[ParentId] = e.[ExpenseId] and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0 and
									 (select count(e.[PropertyCode]) from [dbo].[Expense] e
									  where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0
								then 
									0
								else
									1
							 end
			,'AllApproved' = case
								when (select count(e.[PropertyCode]) from [dbo].[Expense] e 
									  where p.[PropertyCode] = e.[PropertyCode] and  e.[ApprovalStatus] < 2 and e.[IsDeleted] = 0 and e.[ParentId] = e.[ExpenseId] and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0 and
									 (select count(e.[PropertyCode]) from [dbo].[Expense] e 
									  where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0
								then
									0
								else
									1
							 end
			,'Finalized' = case
								when (select count(e.[PropertyCode]) from [dbo].[Expense] e 
									  where p.[PropertyCode] = e.[PropertyCode] and e.[ApprovalStatus] < 3 and e.[IsDeleted] = 0 and e.[ParentId] = e.[ExpenseId] and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0 and
									 (select count(e.[PropertyCode]) from [dbo].[Expense] e 
									  where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) > 0
								then 
									0
								else
									1
						   end
			,'Empty' = case
							when (select count(e.[PropertyCode]) from [dbo].[Expense] e 
								  where p.[PropertyCode] = e.[PropertyCode] and e.[ParentId] = e.[ExpenseId] and e.[IsDeleted] = 0 and Convert(date,e.[ExpenseDate]) >= @StartDate and Convert(date,e.[ExpenseDate]) <= @EndDate) = 0 
							then 
								1
							else 
								0
					   end
		FROM [dbo].[CPL] p
		WHERE (p.[PropertyStatus] like 'Active%' or p.[PropertyStatus] = 'Inactive' or p.[PropertyStatus] = 'Pending-Onboarding') and
				p.[PropertyCode] <> 'PropertyPlaceholder'
		ORDER BY [PropertyCode]
	END

	ELSE IF (@TableName = 'OtherRevenue')
	BEGIN
		SELECT distinct
			p.[PropertyCode]
			,'PropertyCodeAndAddress' = p.[PropertyCode] + (case when p.[Address] is null then '' else ' | ' +  p.[Address] end)
			,'AllReviewed' = case
								when (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[ApprovalStatus] < 1 and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0
								then 
									0
								else
									1
							 end
			,'AllApproved' = case
								when (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and  r.[ApprovalStatus] < 2 and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0
								then 
									0
								else
									1
							 end
			,'Finalized' = case
								when (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[ApprovalStatus] < 3 and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0 and
									 (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
									  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) > 0
								then 
									0
								else
									1
						   end
			,'Empty' = case
							when (select count(r.[PropertyCode]) from [dbo].[OtherRevenue] r 
								  where p.[PropertyCode] = r.[PropertyCode] and r.[IsDeleted] = 0 and Convert(date,r.[OtherRevenueDate]) >= @StartDate and Convert(date,r.[OtherRevenueDate]) <= @EndDate) = 0 
							then 
								1
							else 
								0
					   end
		FROM [dbo].[CPL] p
		WHERE (p.[PropertyStatus] like 'Active%' or p.[PropertyStatus] = 'Inactive' or p.[PropertyStatus] = 'Pending-Onboarding') and
				p.[PropertyCode] <> 'PropertyPlaceholder'
		ORDER BY [PropertyCode]
	END

END