using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyAccount")]
    public class PropertyAccount
    {
        [Key]
        public int PropertyAccountId { get; set; }

        [MaxLength(100)]
        public string LoginAccount { get; set; }

        [MaxLength(50), Required]
        public string OwnerName { get; set; }

        [MaxLength(100)]
        public string OwnerEmail { get; set; }

        [NotMapped]
        public ICollection<PayoutMethod> PayoutMethods { get; set; }
    }
}
