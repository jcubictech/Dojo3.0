CREATE PROCEDURE [dbo].[WeeklyUnitInServices]
	@StartDate DateTime,
	@EndDate DateTime
AS
BEGIN

	Declare @EndMonth int,
			@StartMOnth int
	Set @EndMonth = DatePart(Month, @EndDate)
	Set @StartMOnth = DatePart(Month, @StartDate)

	Select  [Market],
			IsNull([1], 0) as 'Week 1',
			IsNull([2], 0) as 'Week 2',
			IsNull([3], 0) as 'Week 3',
			IsNull([4], 0) as 'Week 4',
			IsNull([5], 0) as 'Week 5',
			IsNull([6], 0) as 'Week 6',
			IsNull([7], 0) as 'Week 7',
			IsNull([8], 0) as 'Week 8',
			IsNull([9], 0) as 'Week 9'
	From 
	(
	Select  p.[Market],
			DATEDIFF(week, DATEADD(MONTH, DATEDIFF(MONTH, 0, i.[InquiryCreatedTimestamp]), 0), i.[InquiryCreatedTimestamp]) + 1 as [Weeks],
			i.[TotalPayout] as 'TotalPayout'
	From [dbo].[CPL] p
	inner join [dbo].[InquiriesValidation] i on i.[PropertyCode] = p.[PropertyCode]

	-- Only get rows where the date is the same as the DatePeriod
	-- i.e DatePeriod is 30th December 2016 then only the weeks of May will be calculated
	Where DatePart(Month, i.[InquiryCreatedTimestamp]) >= @StartMOnth and
		  DatePart(Month, i.[InquiryCreatedTimestamp]) <= @EndMOnth and
		  [TotalPayout] > 0
	) p

	Pivot (Count([TotalPayout]) for Weeks in ([1],[2],[3],[4],[5],[6],[7],[8],[9])) as pv

END