using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyAccountPayoutMethod")]
    public class PropertyAccountPayoutMethod
    {
        [Key]
        public int PropertyAccountPayoutMethodId { get; set; }

        [Required]
        public int PropertyAccountId { get; set; }

        [Required]
        public int PayoutMethodId { get; set; }

        public virtual PropertyAccount PropertyAccount { get; set; }

        public virtual PayoutMethod PayoutMethod { get; set; }
    }
}
