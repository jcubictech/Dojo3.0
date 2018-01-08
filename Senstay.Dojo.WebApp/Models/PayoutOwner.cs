using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PayoutOwner")]
    public class PayoutOwner
    {
        [Key]
        public int PayoutOwnerId { get; set; }

        [MaxLength(50)]
        public string OwnerName { get; set; }

        [MaxLength(100)]
        public string OwnerEmail { get; set; }

        [MaxLength(100)]
        public string LoginAccount { get; set; }

        public DateTime EffectiveDate { get; set; }

        // payout method 1 - M fields
        public virtual List<PayoutMethod> PayoutMethods { get; set; }
    }
}
