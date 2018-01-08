using System;

namespace Senstay.Dojo.Models.View
{
    public class StatementReportViewModel
    {
        public StatementReportViewModel()
        {
            // default to last date of last month
            //var lastMonth = DateTime.Now.AddMonths(-1);
            //ReportDate = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
        }

        public StatementReportItemType ReportItemType { get; set; }

        public string PropertyCode { get; set; }

        public string Vertical { get; set; }

        public string CustomerJob { get; set; }

        public string ClassName { get; set; }

        public DateTime ReportDate { get; set; }

        public long InvoiceNumber { get; set; }

        public string ItemName { get; set; }

        public int Quantity { get; set; } = 1;

        public double Rate { get; set; }

        public double Amount { get; set; }
    }
}