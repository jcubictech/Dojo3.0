using Senstay.Dojo.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyOwnership")]
    public class PropertyOwnership
    {
        [Key]
        public int PropertyOwnershipId { get; set; }

        [MaxLength(50), Required(ErrorMessage = "{0} is required.")]
        public string PropertyCode { get; set; } = AppConstants.DEFAULT_PROPERTY_CODE;

        [MaxLength(50)]
        public string PayoutAccount { get; set; }

        [MaxLength(50)]
        public string OwnerName { get; set; }

        [MaxLength(50)]
        public string OwnerEmail { get; set; }

        [MaxLength(100)]
        public string LoginAccount { get; set; }

        public virtual CPL Property { get; set; } // foreign key
    }
}
