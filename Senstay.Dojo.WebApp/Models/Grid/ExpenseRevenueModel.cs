using System;
using System.Collections.Generic;

namespace Senstay.Dojo.Models.Grid
{
    public class ExpenseRevenueModel
    {
        public ExpenseRevenueModel()
        {
            Children = new List<ExpenseRevenueModel>();
        }

        public int ExpenseId { get; set; } = 0;

        public int ParentId { get; set; } = 0;

        public DateTime? ExpenseDate { get; set; }

        public string ConfirmationCode { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public float ExpenseAmount { get; set; } = 0;

        public bool IncludeOnStatement { get; set; } = true;

        public string ApprovedNote { get; set; }

        public int ReservationId { get; set; } = 0;

        public string PropertyCode { get; set; } = string.Empty;

        public RevenueApprovalStatus ApprovalStatus { get; set; } = RevenueApprovalStatus.NotStarted;

        public bool Reviewed { get; set; } = false;

        public bool Approved { get; set; } = false;

        public string Memo { get; set; }

        public List<ExpenseRevenueModel> Children { get; set; }
    }
}
