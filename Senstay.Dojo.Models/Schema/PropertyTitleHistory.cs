using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyTitleHistory")]
    public class PropertyTitleHistory
    {
        [Key]
        public int PropertyTitleHistoryId { get; set; }

        [Required(ErrorMessage = "{0} is required."), MaxLength(200)]
        public string PropertyTitle { get; set; }

        [Required(ErrorMessage = "{0} is required."), MaxLength(50)]
        public string PropertyCode { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public DateTime EffectiveDate { get; set; }
    }
}
