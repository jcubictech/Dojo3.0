using Senstay.Dojo.Helpers;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("Resolution")]
    public class Resolution
    {
        [Key]
        public int ResolutionId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public int OwnerPayoutId { get; set; } // foreign key

        public DateTime? ResolutionDate { get; set; }

        [MaxLength(50)]
        public string ConfirmationCode { get; set; }

        [MaxLength(100)]
        public string ResolutionType { get; set; }

        [MaxLength(500)]
        public string ResolutionDescription { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public float ResolutionAmount { get; set; } = 0;

        public bool IncludeOnStatement { get; set; } = true;

        [MaxLength(500)]
        public string Impact { get; set; }

        [MaxLength(50)]
        public string PropertyCode { get; set; } = AppConstants.DEFAULT_PROPERTY_CODE;

        public RevenueApprovalStatus ApprovalStatus { get; set; } = RevenueApprovalStatus.NotStarted;

        [MaxLength(100)]
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedDate { get; set; }

        [MaxLength(100)]
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        [MaxLength(500)]
        public string ApprovedNote { get; set; }

        [MaxLength(100)]
        public string FinalizedBy { get; set; }
        public DateTime? FinalizedDate { get; set; }

        [MaxLength(100)]
        public string ClosedBy { get; set; }
        public DateTime? ClosedDate { get; set; }

        [MaxLength(100)]
        public string InputSource { get; set; }

        public DateTime CreatedDate { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

        [MaxLength(50)]
        public string ModifiedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        public virtual OwnerPayout OwnerPayout { get; set; } // foreign key
    }
}
