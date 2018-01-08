using System;

namespace Senstay.Dojo.Models.View
{
    public class FutureRevenueViewModel
    {
        public FutureRevenueViewModel()
        {
        }

        public string Account { get; set; }

        public DateTime ReportDate { get; set; }

        public int MonthCount { get; set; }

        public int QuarterCount { get; set; }

        public int SemiAnnualCount { get; set; }

        public double MonthRevenue { get; set; }

        public double QuarterRevenue { get; set; }

        public double SemiAnnualRevenue { get; set; }
    }
}