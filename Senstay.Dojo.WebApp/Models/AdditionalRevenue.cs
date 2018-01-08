using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("AdditionalRevenue")]
    public class AdditionalRevenue
    {
        [Key]
        public int AdditionalRevenueId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public int PropertyId { get; set; }

        public DateTime? IssueDate { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public float RevenueAmount { get; set; } = 0;

        [MaxLength(500)]
        public string RevenueDescription { get; set; }

        public DateTime CreatedDate { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

        [MaxLength(50)]
        public string ModifiedBy { get; set; }

        public virtual CPL Property { get; set; } // foreign key
    }
}
