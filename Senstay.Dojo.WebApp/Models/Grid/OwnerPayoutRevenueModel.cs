using System;
using System.Collections.Generic;

namespace Senstay.Dojo.Models.Grid
{
    public class OwnerPayoutRevenueFlatModel
    {
        public OwnerPayoutRevenueFlatModel()
        {
        }

        public int OwnerPayoutId { get; set; }

        public string Source { get; set; }

        public DateTime? PayoutDate { get; set; }

        public float PayoutAmount { get; set; }

        public string PayToAccount { get; set; }

        public bool IsAmountMatched { get; set; }

        public double? DiscrepancyAmount { get; set; }

        public string InputSource { get; set; }

        public double? ReservationTotal { get; set; }

        public double? ResolutionTotal { get; set; }

        public string RevenueType { get; set; }

        public DateTime? CheckinDate { get; set; }

        public int? Nights { get; set; }

        public string ConfirmationCode { get; set; }

        public float? Amount { get; set; }

        public int? ChildId { get; set; }

        public string PropertyCode { get; set; }
    }

    public class OwnerPayoutRevenueModel
    {
        public OwnerPayoutRevenueModel()
        {
        }

        public int OwnerPayoutId { get; set; }

        public string Source { get; set; }

        public DateTime? PayoutDate { get; set; }

        public float PayoutAmount { get; set; }

        public string PayToAccount { get; set; }

        public bool IsAmountMatched { get; set; }

        public double? DiscrepancyAmount { get; set; }

        public string InputSource { get; set; }

        public double? ReservationTotal { get; set; }

        public double? ResolutionTotal { get; set; }

        public List<OwnerPayoutRevenueChildModel> Children { get; set; }
    }

    public class OwnerPayoutRevenueChildModel
    {
        public string RevenueType { get; set; }

        public DateTime? CheckinDate { get; set; }

        public int? Nights { get; set; }

        public string ConfirmationCode { get; set; }

        public float? Amount { get; set; }

        public int? ChildId { get; set; }

        public string PropertyCode { get; set; }
    }
}