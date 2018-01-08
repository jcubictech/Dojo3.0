using System;
using System.ComponentModel;

namespace Senstay.Dojo.Models.Grid
{
    public class ResolutionRevenueModel
    {
        public ResolutionRevenueModel()
        {
        }

        public int ResolutionId { get; set; }

        public int OwnerPayoutId { get; set; }

        public string PropertyCode { get; set; }

        public DateTime? ResolutionDate { get; set; }

        [DisplayName("Confirmation/Property Code")]
        public string ConfirmationCode { get; set; }

        public string ResolutionType { get; set; }

        public string ResolutionDescription { get; set; }

        public string Impact { get; set; }

        public string Cause { get; set; }

        public string Product { get; set; }

        public string PayToAccount { get; set; }

        public float ResolutionAmount { get; set; } = 0;

        public bool IncludeOnStatement { get; set; } = true;

        public string Source { get; set; }

        public string ApprovedNote { get; set; }

        public RevenueApprovalStatus ApprovalStatus { get; set; } = RevenueApprovalStatus.NotStarted;

        public bool Reviewed { get; set; } = false;

        public bool Approved { get; set; } = false;

        public bool CanStatementSeeIt { get; set; } = true;
    }
}
