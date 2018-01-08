using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Models
{
    [Table("PayoutProperty")]
    public class PayoutProperty
    {
        [Key]
        public int PayoutPropertyId { get; set; }

        [MaxLength(50), Required(ErrorMessage = "{0} is required.")]
        public string PropertyCode { get; set; } = AppConstants.DEFAULT_PROPERTY_CODE;

        public int PayoutMethodId { get; set; }
    }
}
