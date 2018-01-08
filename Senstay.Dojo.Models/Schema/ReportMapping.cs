using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("ReportMapping")]
    public class ReportMapping
    {
        [Key]
        public int ReportMappingId { get; set; }

        [MaxLength(50), Required]
        public string PropertyCode { get; set; }

        [MaxLength(100), Required]
        public string CustomerJob { get; set; }

        [MaxLength(100), Required]
        public string Class { get; set; }
    }
}
