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

        public DateTime EntryDate { get; set; }

        public double? DamageWaiver { get; set; } = 0;

        public double? ManagementFee { get; set; } = 0;

        public double? CityTax { get; set; } = 0;

        public double? Cleanings { get; set; }

        public double? Laundry { get; set; }

        public double? Consumables { get; set; }

        public double? PoolService  { get; set; }

        public double? Landscaping { get; set; }

        public double? TrashService  { get; set; }

        public double? PestService  { get; set; }

        public string InputSource { get; set; }

        public virtual CPL Property { get; set; } // foreign key
    }
}
