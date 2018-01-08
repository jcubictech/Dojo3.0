using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("OwnerPayout")]
    public class OwnerPayout
    {
        [Key]
        public int OwnerPayoutId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public float PayoutAmount { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public DateTime? PayoutDate { get; set; }

        [MaxLength(50)]
        public string Source { get; set; }

        public string AccountNumber { get; set; }

        public bool IsAmountMatched { get; set; }

        public float? DiscrepancyAmount { get; set; }

        public bool IsDeleted { get; set; } = false;

        [MaxLength(100)]
        public string InputSource { get; set; }

        public DateTime CreatedDate { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

        [MaxLength(50)]
        public string ModifiedBy { get; set; }

        // payout 1 - M fields
        public virtual List<Reservation> Reservations { get; set; }

        public virtual List<Resolution> Resolutions { get; set; }
    }
}
