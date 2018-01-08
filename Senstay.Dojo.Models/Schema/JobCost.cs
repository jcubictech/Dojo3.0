using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("JobCost")]
    public class JobCost
    {
        [Key]
        public int JobCostId { get; set; }

        [MaxLength(50)]
        public string PropertyCode { get; set; }

        [MaxLength(100)]
        public string JobCostPayoutTo { get; set; }

        [MaxLength(50)]
        public string JobCostType { get; set; }

        public DateTime? JobCostDate { get; set; }

        [MaxLength(50)]
        public string JobCostNumber { get; set; }

        [MaxLength(50)]
        public string JobCostSource { get; set; }

        [MaxLength(500)]
        public string JobCostMemo { get; set; }

        [MaxLength(50)]
        public string JobCostAccount { get; set; }

        [MaxLength(50)]
        public string JobCostClass { get; set; }

        public bool JobCostBillable { get; set; }

        public float? JobCostAmount { get; set; }

        public float? JobCostBalance { get; set; }

        [MaxLength(50)]
        public string OriginalPropertyCode { get; set; }

        public virtual CPL Property { get; set; } // foreign key
    }
}