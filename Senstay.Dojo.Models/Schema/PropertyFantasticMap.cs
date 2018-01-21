using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyFantasticMap")]
    public class PropertyFantasticMap
    {
        [Key]
        public int PropertyFantasticMapId { get; set; }

        [Required(ErrorMessage = "{0} is required."), MaxLength(50)]
        public string PropertyCode { get; set; }

        [Required(ErrorMessage = "{0} is required."), MaxLength(50)]
        public string ListingId { get; set; }
    }
}
