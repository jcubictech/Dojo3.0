CREATE PROCEDURE [dbo].[WeeklyMarketPayouts]
	@StartDate DateTime,
	@EndDate DateTime
AS
BEGIN
	Declare @EndWeek int,
			@StartWeek int
	Set @EndWeek = DatePart(wk, @EndDate) + 1
	Set @StartWeek = DatePart(wk, @StartDate)

SELECT		@EndWeek - DATEPART(wk, [InquiryCreatedTimestamp]) as 'Week', Sum([TotalPayout]) as 'Payout', [Market]
FROM		[dbo].[InquiriesValidation] i
INNER JOIN [dbo].[CPL] p on p.[PropertyCode] = i.[PropertyCode]
WHERE DATEPART(wk, [InquiryCreatedTimestamp]) >= @StartWeek and DATEPART(wk, [InquiryCreatedTimestamp]) <= @EndWeek
GROUP BY	DATEPART(wk, [InquiryCreatedTimestamp]), [Market]
ORDER BY	[Market] desc, [Week] asc

END