using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("Expense")]
    public class Expense
    {
        [Key]
        public int ExpenseId { get; set; }

        public int? JobCostId { get; set; }

        public int ParentId { get; set; }

        public DateTime? ExpenseDate { get; set; }

        [MaxLength(50)]
        public string ConfirmationCode { get; set; }

        [MaxLength(200)]
        public string Category { get; set; }

        public int ReservationId { get; set; } = 0;

        [MaxLength(50)]
        public string PropertyCode { get; set; }

        public float ExpenseAmount { get; set; } = 0;

        public bool IncludeOnStatement { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public RevenueApprovalStatus ApprovalStatus { get; set; } = RevenueApprovalStatus.NotStarted;

        [MaxLength(100)]
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedDate { get; set; }

        [MaxLength(100)]
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        [MaxLength(500)]
        public string ApprovedNote { get; set; }

        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }
        [MaxLength(50)]
        public string ModifiedBy { get; set; }

        public virtual Reservation Reservation { get; set; } // foreign key

        public virtual JobCost JobCost { get; set; } // foreign key
    }
}
