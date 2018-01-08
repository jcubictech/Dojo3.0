using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("AirbnbAccount")]
    public partial class AirbnbAccount
    {
        [Key]
        public int Id { get; set; }
        [DisplayName("Arbnb Account Email"), Required(ErrorMessage = "{0} is required.")]
        public string Email { get; set; }
        public string Password { get; set; }
        [DisplayName("Gmail Password")]
        public string Gmailpassword { get; set; }
        public string Status { get; set; }
        public Nullable<DateTime> DateAdded { get; set; }
        [DisplayName("Gmail Recovery Email")]
        public string SecondaryAccountEmail { get; set; }
        public string AccountAdmin { get; set; }
        public string Vertical { get; set; }
        [DisplayName("Owner/Company")]
        public string Owner_Company { get; set; }
        public string Name { get; set; }
        [DisplayName("Owner Number")]
        public string PhoneNumber1 { get; set; }
        [DisplayName("Owner Number Source")]
        public string PhoneNumberOwner { get; set; }
        [DisplayName("Owner DOB")]
        public Nullable<DateTime> DOB1 { get; set; }
        [DisplayName("Payout Method")]
        public string Payout_Method { get; set; }
        [DisplayName("Point of Contact")]
        public string PointofContact { get; set; }
        [DisplayName("POC Number")]
        public string PhoneNumber2 { get; set; }
        [DisplayName("POC DOB")]
        public Nullable<DateTime> DOB2 { get; set; }
        [DisplayName("POC Email")]
        public string EmailAddress { get; set; }
        [RegularExpression(@"(\s*[0-9]{0,4})", ErrorMessage = "{0} must be a natural number (max 9999)")]
        public Nullable<int> ActiveListings { get; set; }
        [DisplayName("Pending Onboarding")]
        [RegularExpression(@"(\s*[0-9]{0,4})", ErrorMessage = "{0} must be a natural number (max 9999)")]
        public Nullable<int> Pending_Onboarding { get; set; }
        [DisplayName("Inactive Listings")]
        [RegularExpression(@"(\s*[0-9]{0,4})", ErrorMessage = "{0} must be a natural number (max 9999)")]
        public Nullable<int> In_activeListings { get; set; }
        [DisplayName("# of Listings in LA Market")]
        [RegularExpression(@"(\s*[0-9]{0,4})", ErrorMessage = "{0} must be a natural number (max 9999)")]
        public Nullable<int> ofListingsinLAMarket { get; set; }
        [DisplayName("# of Listings in NYC Market")]
        [RegularExpression(@"(\s*[0-9]{0,4})", ErrorMessage = "{0} must be a natural number (max 9999)")]
        public Nullable<int> ofListingsinNYCMarket { get; set; }
        [DisplayName("Proxy IP 1")]
        public string ProxyIP { get; set; }
        [DisplayName("Proxy IP 2")]
        public string C2ndProxyIP { get; set; }

        // house keeping fields
        public Nullable<DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
