using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PayoutPayment")]
    public class PayoutPayment
    {
        [Key]
        public int PayoutPaymentId { get; set; }

        public int PaymentMonth { get; set; }

        public int PaymentYear { get; set; }

        public double PaymentAmount { get; set; }

        public DateTime PaymentDate { get; set; }

        public int? PayoutMethodId { get; set; }

        public virtual PayoutMethod PayoutMethod { get; set; } // foreign key
    }
}
