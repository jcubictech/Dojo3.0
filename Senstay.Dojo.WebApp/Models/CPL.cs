using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("CPL")]
    public partial class CPL
    {
        public CPL()
        {
            this.InquiriesValidations = new List<InquiriesValidation>();
        }

        [DisplayName("Airbnb Home Name")]
        public string AirBnBHomeName { get; set; }
        [Key]
        [Required(ErrorMessage = "{0} is required.")]
        public string PropertyCode { get; set; }
        public string Address { get; set; }
        public string PropertyStatus { get; set; }
        [DisplayName("Product")]
        public string Vertical { get; set; }
        [DisplayName("Owner Contact")]
        public string Owner { get; set; }
        public Nullable<bool> NeedsOwnerApproval { get; set; }
        public Nullable<DateTime> ListingStartDate { get; set; }
        public string StreamlineHomeName { get; set; }
        public string StreamlineUnitID { get; set; }
        public string Account { get; set; }
        public string City { get; set; }
        public string Market { get; set; }
        public string State { get; set; }
        public string Area { get; set; }
        public string Neighborhood { get; set; }
        public string BookingGuidelines { get; set; }
        public string Floor { get; set; }
        [RegularExpression(@"(\s*[0-9]{0,2})", ErrorMessage = "{0} must be a natural number (max 99)")]
        public Nullable<int> Bedrooms { get; set; }
        [RegularExpression(@"[0-9]*\.?[0-9]+", ErrorMessage = "{0} must be a Number.")]
        public Nullable<float> Bathrooms { get; set; }
        public string BedsDescription { get; set; }
        [RegularExpression(@"(\s*[0-9]{0,2})", ErrorMessage = "{0} must be a natural number (max 99)")]
        public Nullable<int> MaxOcc { get; set; }
        [RegularExpression(@"(\s*[0-9]{0,2})", ErrorMessage = "{0} must be a natural number (max 99)")]
        public Nullable<int> StdOcc { get; set; }
        public string Elevator { get; set; }
        [DisplayName("AC")]
        public string A_C { get; set; }
        public string Parking { get; set; }
        [DisplayName("WiFi Network")]
        public string WiFiNetwork { get; set; }
        [DisplayName("WiFi Password")]
        public string WiFiPassword { get; set; }
        [RegularExpression(@"\s*[0-9]*\.?[0-9]+", ErrorMessage = "{0} must be a Number.")]
        public Nullable<float> ManagementFee { get; set; }
        [RegularExpression(@"\s*[0-9]*\.?[0-9]+", ErrorMessage = "Management Fee must be a Number.")]
        public Nullable<float> Ownership { get; set; }
        [RegularExpression(@"\s*[0-9]*\.?[0-9]+", ErrorMessage = "{0} must be a Number.")]
        public Nullable<decimal> MonthlyRent { get; set; }
        [RegularExpression(@"\s*[0-9]*\.?[0-9]+", ErrorMessage = "{0} must be a Number.")]
        public Nullable<decimal> DailyRent { get; set; }
        [RegularExpression(@"(\s*[0-9]{0,4})", ErrorMessage = "{0} must be a natural number (max 9999)")]
        public Nullable<int> CleaningFees { get; set; }
        [DisplayName("Airbnb ID")]
        public string AIrBnBID { get; set; }
        [DisplayName("Airbnb Export Link")]
        public string AirbnbiCalexportlink { get; set; }
        [DisplayName("Walk-through Checklist Link")]
        public string Amenities { get; set; }
        public string Zipcode { get; set; }
        public string OldListingTitle { get; set; }
        [DisplayName("Copy Document")]
        public string GoogleDrivePicturesLink { get; set; }
        public Nullable<int> SquareFootage { get; set; }
        public string Password { get; set; }
        public Nullable<bool> InquiryLeadApproval { get; set; }
        public Nullable<bool> RevTeam2xApproval { get; set; }
        public Nullable<DateTime> PendingContractDate { get; set; }
        public Nullable<DateTime> PendingOnboardingDate { get; set; }
        public string SecurityDeposit { get; set; }
        public string Pool { get; set; }
        [DisplayName("Airbnb Link")]
        public string AirBnb { get; set; }
        public string FlipKey { get; set; }
        public string Expedia { get; set; }
        public Nullable<DateTime> Inactive { get; set; }
        public Nullable<DateTime> Dead { get; set; }
        public string Currency { get; set; }
        public string CrossStreets { get; set; }
        public string SellingPoints { get; set; }
        [DisplayName("Check-in Type")]
        public string CheckInType { get; set; }
        [DisplayName("Home Away Property Code")]
        public string HomeAway { get; set; }
        public string BeltDesignation { get; set; }

        // house keeping fields
        public Nullable<DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }

        public virtual ICollection<InquiriesValidation> InquiriesValidations { get; set; }

        #region Obsolete fields: to be removed when new schema is installed

        // ====================================================================================
        // TODO: remove these 4 fields after PropertyAccount, PropertyEntity are added
        // ====================================================================================
        [RegularExpression(@"\s*[-+]?[0-9]*\.?[0-9]+", ErrorMessage = "{0} must be a Number.")]
        public Nullable<float> OutstandingBalance { get; set; }
        public string OwnerEntity { get; set; }
        public string OwnerPayout { get; set; }
        public string PaymentEmail { get; set; }
        // new field 03/21/2017

        #endregion
    }
}
