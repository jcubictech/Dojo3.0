using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PayoutMethod")]
    public class PayoutMethod
    {
        [Key]
        public int PayoutMethodId { get; set; }

        [MaxLength(100), Required]
        public string PayoutMethodName { get; set; }

        [MaxLength(50)]
        public string PayoutAccount { get; set; }

        public PayoutMethodType PayoutMethodType { get; set; } = PayoutMethodType.Checking;

        public DateTime EffectiveDate { get; set; }

        public bool IsDeleted { get; set; } = false;

        // payout method 1 - M fields
        public virtual List<PayoutPayment> PayoutPayments { get; set; }

        [NotMapped]
        public double PayoutAmount { get; set; } = 0;

        [NotMapped]
        public double TotalPayments { get; set; } = 0;

        [NotMapped]
        public double TotalBalance { get; set; } = 0;
    }
}
