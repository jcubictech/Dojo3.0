using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyEntity")]
    public class PropertyEntity
    {
        [Key]
        public int PropertyEntityId { get; set; }

        [MaxLength(100), Required]
        public string EntityName { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        [NotMapped]
        public ICollection<CPL> PropertyList { get; set; }
    }
}
