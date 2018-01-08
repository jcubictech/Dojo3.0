using System;

namespace Senstay.Dojo.Models.Grid
{
    public class OtherRevenueModel
    {
        public int OtherRevenueId { get; set; }

        public string PropertyCode { get; set; }

        public DateTime? OtherRevenueDate { get; set; }

        public float OtherRevenueAmount { get; set; } = 0;

        public string OtherRevenueDescription { get; set; }

        public bool IncludeOnStatement { get; set; } = true;

        public string ApprovedNote { get; set; }

        public RevenueApprovalStatus ApprovalStatus { get; set; }

        public bool Reviewed { get; set; }

        public bool Approved { get; set; }

    }
}
