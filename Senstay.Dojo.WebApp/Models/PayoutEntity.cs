using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PayoutEntity")]
    public class PayoutEntity
    {
        [Key]
        public int PayoutEntityId { get; set; }

        [MaxLength(50), Required]
        public string PayoutEntityName { get; set; }

        [MaxLength(50)]
        public string OwnerContact { get; set; }

        [MaxLength(100)]
        public string LoginAccount { get; set; }

        public DateTime EffectiveDate { get; set; }
    }
}
