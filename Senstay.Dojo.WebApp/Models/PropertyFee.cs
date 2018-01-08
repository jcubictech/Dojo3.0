using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyFee")]
    public class PropertyFee
    {
        [Key]
        public int PropertyCostId { get; set; }

        public string PropertyCode { get; set; }

        public double? DamageWaiver { get; set; } = 0;

        public double? ManagementFee { get; set; } = 0;

        public double? CityTax { get; set; } = 0;

        public double? AdminFee { get; set; } = 0;

        public double? PlatformFee { get; set; } = 0;

        public double? TaxableFactor { get; set; } = 0;

        public DateTime EntryDate { get; set; }

        public string InputSource { get; set; }

        public virtual CPL Property { get; set; } // foreign key
    }
}
