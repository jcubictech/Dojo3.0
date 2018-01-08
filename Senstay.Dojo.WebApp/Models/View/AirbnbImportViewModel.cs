using System;
using System.Collections.Generic;
using Senstay.Dojo.FtpClient;

namespace Senstay.Dojo.Models.View
{
    public class AirbnbImportFormModel
    {
        public AirbnbImportFormModel()
        {
        }

        public DateTime ReportDate { get; set; }

        public string CompletedTransactionFiles { get; set; }

        public string FutureTransactionFiles { get; set; }

        public string GrossTransactionFiles { get; set; }

        public FtpTransactionType TransactionFileType { get; set; }
    }

    public class AirbnbImportCalendarModel
    {
        public const int IMPORT_DISPLAY_DAYS = 28;

        public AirbnbImportCalendarModel()
        {
            calendarRows = new List<AirbnbImportCalendarRowModel>();
            CalendarHeaderRow = new List<string>();
        }

        public DateTime LatestDate { get; set; }

        public int DisplayDays { get; set; } = IMPORT_DISPLAY_DAYS;

        public List<AirbnbImportCalendarRowModel> calendarRows { get; set; }

        public List<string> CalendarHeaderRow { get; set; }
    }

    public class AirbnbImportCalendarRowModel
    {
        public AirbnbImportCalendarRowModel(int displayDays)
        {
            CalendarCols = new List<AirbnbImportCalendarColModel>();
            for (int i = 0; i < displayDays; i++) CalendarCols.Add(new AirbnbImportCalendarColModel());
        }

        public string Account { get; set; } = string.Empty;

        public int HasInputFile { get; set; } = 0;

        public List<AirbnbImportCalendarColModel> CalendarCols { get; set; }
    }

    public class AirbnbImportCalendarColModel
    {
        public int Column { get; set; } = 0;

        public string ReservationData { get; set; } = string.Empty;

        public string ResolutionData { get; set; } = string.Empty;

        public string GrossData { get; set; } = string.Empty;
    }

    public class AirbnbImportAccountItemModel
    {
        public string Account { get; set; }

        public string ImportDate { get; set; }

        public int MonthDay { get; set; }

        public int PayoutCount { get; set; }

        public int ReservationCount { get; set; }

        public int ResolutionCount { get; set; }
    }
}
