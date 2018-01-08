using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PayoutHistory")]
    public class PayoutHistory
    {
        [Key]
        public int PayoutHistoryId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public int Month { get; set; } = 1;

        [Required(ErrorMessage = "{0} is required.")]
        public int Year { get; set; } = 2016;

        [MaxLength(50), Required(ErrorMessage = "{0} is required.")]
        public string PayoutMethod { get; set; }

        public double? Amount { get; set; }

        public double? EndingBalance { get; set; } = 0;

        public bool IsFinalized { get; set; } = false;
    }
}
