using System;
using System.ComponentModel.DataAnnotations;

namespace Senstay.Dojo.Models.Grid
{
    public class ReservationRevenueModel
    {
        public ReservationRevenueModel()
        {
        }

        public int ReservationId { get; set; }

        public int OwnerPayoutId { get; set; }

        public DateTime? PayoutDate { get; set; }

        public string ConfirmationCode { get; set; }

        [Required]
        public string Channel { get; set; }

        [Required]
        public float TotalRevenue { get; set; }

        [Required]
        public DateTime? CheckinDate { get; set; }

        [Required]
        public int Nights { get; set; }

        [Required]
        public string GuestName { get; set; }

        public bool IncludeOnStatement { get; set; } = true;

        public bool IsTaxed { get; set; } = false;

        public bool IsFutureBooking { get; set; } = false;

        public int ApprovalStatus { get; set; }

        public string PropertyCode { get; set; }

        public bool Reviewed { get; set; }

        public bool Approved { get; set; }

        public bool Finalized { get; set; }

        public string ApprovedNote { get; set; }

        public double? TaxRate { get; set; } = 0;

        public double TaxCollected { get; set; } = 0;

        public string OverlapColor { get; set; } = "";

        public string InputSource { get; set; }

        public string Source { get; set; }

        public DateTime? Month { get; set; }
    }

    public class DuplicateReservationModel
    {
        public DuplicateReservationModel()
        {
        }

        public int ReservationId { get; set; }

        public int OwnerPayoutId { get; set; }

        public DateTime TransactionDate { get; set; }

        public string PropertyCode { get; set; }

        public string ConfirmationCode { get; set; }

        public string GuestName { get; set; }

        public DateTime CheckinDate { get; set; }

        public int Nights { get; set; }

        public float TotalRevenue { get; set; }

        public string PayoutAccount { get; set; }

        public string Channel { get; set; } = "Airbnb";

        public bool IsSameTransactionDate { get; set; } = false;
    }

    public class MissingPropertyCodesModel
    {
        public MissingPropertyCodesModel()
        {
        }

        public int ReservationId { get; set; }

        public string PropertyCode { get; set; }

        public string ConfirmationCode { get; set; }

        public string ListingTitle { get; set; }

        public DateTime TransactionDate { get; set; }

        public string GuestName { get; set; }

        public DateTime CheckinDate { get; set; }

        public int Nights { get; set; }

        public float TotalRevenue { get; set; }
    }
}