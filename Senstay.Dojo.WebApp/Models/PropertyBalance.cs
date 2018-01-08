using Senstay.Dojo.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyBalance")]
    public class PropertyBalance
    {
        [Key]
        public int PropertyBalanceId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public int Month { get; set; } = 0;

        [Required(ErrorMessage = "{0} is required.")]
        public int Year { get; set; } = 0;

        [MaxLength(50), Required(ErrorMessage = "{0} is required.")]
        public string PropertyCode { get; set; } = AppConstants.DEFAULT_PROPERTY_CODE;

        public double? BeginningBalance { get; set; }

        public double? AdjustedBalance { get; set; }
    }
}
