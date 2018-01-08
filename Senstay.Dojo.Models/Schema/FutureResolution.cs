using Senstay.Dojo.Helpers;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("FutureResolution")]
    public class FutureResolution
    {
        [Key]
        public int FutureResolutionId { get; set; }

        public DateTime? ResolutionDate { get; set; }

        [MaxLength(50)]
        public string ConfirmationCode { get; set; }

        [MaxLength(100)]
        public string ResolutionType { get; set; }

        [MaxLength(500)]
        public string ResolutionDescription { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public float ResolutionAmount { get; set; } = 0;

        [MaxLength(500)]
        public string Impact { get; set; }

        [MaxLength(50)]
        public string PropertyCode { get; set; } = AppConstants.DEFAULT_PROPERTY_CODE;

        public RevenueApprovalStatus ApprovalStatus { get; set; } = RevenueApprovalStatus.NotStarted;

        [MaxLength(100)]
        public string InputSource { get; set; }

        public DateTime CreatedDate { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

        [MaxLength(50)]
        public string ModifiedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        public virtual CPL Property { get; set; } // foreign key
    }
}
