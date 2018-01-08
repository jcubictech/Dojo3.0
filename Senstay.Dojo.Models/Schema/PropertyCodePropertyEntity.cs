using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyCodePropertyEntity")]
    public class PropertyCodePropertyEntity
    {
        [Key]
        public int PropertyCodePropertyEntityId { get; set; }

        [Required]
        public int PropertyEntityId { get; set; }

        [MaxLength(50), Required]
        public string PropertyCode { get; set; }

        public virtual CPL Property { get; set; }

        public virtual PropertyEntity PropertyEntity { get; set; }
    }
}
