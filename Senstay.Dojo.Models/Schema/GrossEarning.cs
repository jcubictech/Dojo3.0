using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("GrossEarning")]
    public class GrossEarning
    {
        [Key]
        public int GrossEarningId { get; set; }

        public string GrossType { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public DateTime? GrossDate { get; set; }

        [Required(ErrorMessage = "{0} is required."), MaxLength(50)]
        public string PropertyCode { get; set; } // foreign key

        [Index(IsUnique = false)]
        [MaxLength(50)]
        public string ConfirmationCode { get; set; }

        [Index(IsUnique = false)]
        [Required(ErrorMessage = "{0} is required.")]
        public DateTime? CheckinDate { get; set; }

        public DateTime? CheckoutDate { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public int Nights { get; set; }

        [Required(ErrorMessage = "{0} is required."), MaxLength(100)]
        public String GuestName { get; set; }

        [MaxLength(200)]
        public string ListingTitle { get; set; }

        public double? CleaningFee { get; set; }

        public double? HostFee { get; set; }

        public double? Amount { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public double? GrossTotal { get; set; }

        public double? OccupancyTax { get; set; }

        public CurrencyType Currency { get; set; } = CurrencyType.USD;

        public string Details { get; set; }

        [MaxLength(100)]
        public string Reference { get; set; }

        [MaxLength(50)]
        public string Channel { get; set; }

        [MaxLength(100)]
        public string InputSource { get; set; }

        public DateTime CreatedDate { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

        [MaxLength(50)]
        public string ModifiedBy { get; set; }
    }
}
