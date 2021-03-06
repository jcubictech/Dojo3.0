USE [DojoDev]
GO
/****** Object:  StoredProcedure [dbo].[AddCalendarData]    Script Date: 5/21/2017 11:37:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[AddCalendarData]
	@StartDate DateTime,
	@EndDate DateTime,
	@FiscalQuarterOffset int = 0
AS
BEGIN

	DECLARE @Date DateTime = @StartDate,
			@FiscalQuarter int

	SET @FiscalQuarter = ((DatePart(Quarter, @Date) - @FiscalQuarterOffset + 3) % 4) + 1

	WHILE (@Date < @EndDate)
	BEGIN
		INSERT INTO [dbo].[DimDate] (
			[DateId]
			,[Date]
			,[DayOfWeek]
			,[DayOfMonth]
			,[DayOfYear]
			,[WeekOfYear]
			,[MonthOfYear]
			,[NameOfWeek]
			,[NameOfMonth]
			,[CalendarQuarter]
			,[CalendarSemester]
			,[CalendarYear]
			,[FiscalQuarter]
			,[FiscalSemester]
			,[FiscalYear])
		VALUES (
			Cast(Format(@Date, 'yyyyMMdd') as int) -- date key
			,@Date  -- alternate key with date object
			,DatePart(dw, @Date) -- day of week
			,Day(@Date) -- day of month
			,DatePart(dy, @Date) -- day of year
			,DatePart(Week, @Date) -- week # of the year
			,DatePart(mm, @Date) -- month of the year
			,DateName(dw, @Date) -- week day name
			,DateName(Month , DateAdd(Month, DatePart(mm, @Date), -1)) -- month name
			,DatePart(Quarter, @Date) -- calendar quarter
			,case when Month(@Date) < 7 then 1 else 2 end -- calendar semester
			,Year(@Date)  -- calendar year
			,@FiscalQuarter -- fiscal quarter
			,case when @FiscalQuarter < 3 then 1 else 2 end  -- fiscal semester
			,Year(@Date)  -- fiscal year
		)

		SET @Date = DateAdd(Day, 1, @Date)
	END
END
