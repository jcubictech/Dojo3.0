using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyTitleMap")]
    public class PropertyTitleMap
    {
        [Key]
        public int PropertyTitleMapId { get; set; }

        [Required(ErrorMessage = "{0} is required."), MaxLength(200)]
        public string DojoPropertyTitle { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public string PropertyCode { get; set; }

        public string AirbnbReportTitle { get; set; }

        public bool IsDead { get; set; }
    }
}
