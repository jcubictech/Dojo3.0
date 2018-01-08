CREATE PROCEDURE [dbo].[MarketPayoutOfMonth]
	@MonthDate DateTime
AS
BEGIN

	Declare @TheMonth int
	Set @TheMonth = DatePart(Month, @MonthDate)

	Select  [Market],
			IsNull([1], 0) as 'Week 1',
			IsNull([2], 0) as 'Week 2',
			IsNull([3], 0) as 'Week 3',
			IsNull([4], 0) as 'Week 4',
			IsNull([5], 0) as 'Week 5'
	From 
	(
	Select  p.[Market],
			DATEDIFF(week, DATEADD(MONTH, DATEDIFF(MONTH, 0, i.[InquiryCreatedTimestamp]), 0), i.[InquiryCreatedTimestamp]) + 1 as [Weeks],
			i.[TotalPayout] as 'TotalPayout'
	From [dbo].[CPL] p
	inner join [dbo].[InquiriesValidation] i on i.[PropertyCode] = p.[PropertyCode]

	-- Only get rows where the date is the same as the DatePeriod
	-- i.e DatePeriod is 30th December 2016 then only the weeks of May will be calculated
	Where DatePart(Month, i.[InquiryCreatedTimestamp]) = @TheMonth
	) p

	Pivot (Sum([TotalPayout]) for Weeks in ([1],[2],[3],[4],[5])) as pv

END