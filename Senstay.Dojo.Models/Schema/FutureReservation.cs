using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("FutureReservation")]
    public class FutureReservation
    {
        [Key]
        public int FutureReservationId { get; set; }

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

        [Required(ErrorMessage = "{0} is required.")]
        public float TotalRevenue { get; set; }

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

        public virtual CPL Property { get; set; } // foreign key
    }
}
