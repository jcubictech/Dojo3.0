using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("Reservation")]
    public class Reservation
    {
        [Key]
        public int ReservationId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public int OwnerPayoutId { get; set; } // foreign key

        [Required(ErrorMessage = "{0} is required."), MaxLength(50)]
        public string PropertyCode { get; set; } // foreign key

        [Index(IsUnique = false)]
        [MaxLength(50)]
        public string ConfirmationCode { get; set; }

        [Index(IsUnique = false)]
        public DateTime? TransactionDate { get; set; }

        [Index(IsUnique = false)]
        [Required(ErrorMessage = "{0} is required.")]
        public DateTime? CheckinDate { get; set; }

        public DateTime? CheckoutDate { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public int Nights { get; set; }

        [Required(ErrorMessage = "{0} is required."), MaxLength(100)]
        public String GuestName { get; set; }

        [MaxLength(50)]
        public string Channel { get; set; }

        [MaxLength(100)]
        public string Source { get; set; }

        [MaxLength(100)]
        public string Reference { get; set; }

        public CurrencyType Currency { get; set; } = CurrencyType.USD;

        [Required(ErrorMessage = "{0} is required.")]
        public float TotalRevenue { get; set; }

        public float? LocalTax { get; set; }

        public float? DamageWaiver { get; set; }

        public float? AdminFee { get; set; }

        public float? PlatformFee { get; set; }

        public float? TaxRate { get; set; }

        public bool IsTaxed { get; set; } = false;

        public bool IncludeOnStatement { get; set; } = true;

        public bool IsFutureBooking { get; set; }

        public RevenueApprovalStatus ApprovalStatus { get; set; } = RevenueApprovalStatus.NotStarted;

        [MaxLength(100)]
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedDate { get; set; }

        [MaxLength(100)]
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        [MaxLength(500)]
        public string ApprovedNote { get; set; }

        public bool IsDeleted { get; set; } = false;

        [MaxLength(100)]
        public string InputSource { get; set; }

        [MaxLength(200)]
        public string ListingTitle { get; set; }

        public DateTime CreatedDate { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

        [MaxLength(50)]
        public string ModifiedBy { get; set; }

        public virtual OwnerPayout OwnerPayout { get; set; } // foreign key

        public virtual CPL Property { get; set; } // foreign key
    }
}
