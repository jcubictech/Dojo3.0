using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("OwnerStatement")]
    public class OwnerStatement
    {
        [Key]
        public int OwnerStatementId { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [MaxLength(50), Required]
        public string PropertyCode { get; set; }

        public double TotalRevenue { get; set; } = 0;

        public double TaxCollected { get; set; } = 0;

        public double CleaningFees { get; set; } = 0;

        public double ManagementFees { get; set; } = 0;

        public double UnitExpenseItems { get; set; } = 0;

        public double AdvancePayments { get; set; } = 0;

        public double BeginBalance { get; set; } = 0;

        public double Balance { get; set; } = 0;

        public bool IsSummary { get; set; }

        [Required]
        public StatementStatus StatementStatus { get; set; } = StatementStatus.Preliminary;

        [MaxLength(100)]
        public string FinalizedBy { get; set; }

        public DateTime? FinalizedDate { get; set; }

        //public virtual CPL Property { get; set; } // foreign key
    }
}
