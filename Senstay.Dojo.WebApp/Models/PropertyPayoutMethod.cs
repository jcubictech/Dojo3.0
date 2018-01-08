using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyPayoutMethod")]
    public class PropertyPayoutMethod
    {
        [Key]
        public int PropertyPayoutMethodId { get; set; }

        [MaxLength(50), Required]
        public string PropertyCode { get; set; }

        [Required]
        public int PayoutMethodId { get; set; }

        public virtual CPL Property { get; set; } // foreign key

        public virtual PayoutMethod PayoutMethod { get; set; } // foreign key
    }
}
