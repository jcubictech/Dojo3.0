using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("OtherRevenue")]
    public class OtherRevenue
    {
        [Key]
        public int OtherRevenueId { get; set; }

        public string PropertyCode { get; set; }

        public DateTime? OtherRevenueDate { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public float OtherRevenueAmount { get; set; } = 0;

        [MaxLength(500)]
        public string OtherRevenueDescription { get; set; }

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

        public virtual CPL Property { get; set; } // foreign key
    }
}
