using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyPayoutEntity")]
    public class PropertyPayoutEntity
    {
        [Key]
        public int PropertyPayoutEntityId { get; set; }

        [MaxLength(50), Required]
        public string PropertyCode { get; set; }

        public int PayoutEntityId { get; set; }

        public virtual CPL Property { get; set; } // foreign key

        public virtual PayoutEntity PayoutEntity { get; set; } // foreign key
    }
}
