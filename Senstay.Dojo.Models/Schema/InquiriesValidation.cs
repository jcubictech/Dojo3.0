using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("InquiriesValidation")]
    public partial class InquiriesValidation
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "{0} is required.")]
        public string GuestName { get; set; }
        [Required(ErrorMessage = "{0} is required.")]
        public string InquiryTeam { get; set; }
        [DisplayName("Airbnb Listing Title")]
        public string AirBnBListingTitle { get; set; }
        [Required(ErrorMessage = "{0} is required.")]
        public string PropertyCode { get; set; }
        [Required(ErrorMessage = "{0} is required.")]
        public string Channel { get; set; }
        public string BookingGuidelines { get; set; }
        [DisplayName("Summary")]
        public string AdditionalInfo_StatusofInquiry { get; set; }
        [DisplayName("Airbnb URL")]
        public string AirBnBURL { get; set; }
        public Nullable<int> Bedrooms { get; set; }
        public string Account { get; set; }
        public Nullable<DateTime> InquiryDate { get; set; }
        [DisplayName("Inquiry Time (PST)")]
        public Nullable<DateTime> InquiryTime__PST_ { get; set; }
        [DisplayName("Check-in Date")]
        public Nullable<DateTime> Check_inDate { get; set; }
        [DisplayName("Check-in Weekday")]
        public string Check_InDay { get; set; }
        [DisplayName("Check-out Date")]
        public Nullable<DateTime> Check_outDate { get; set; }
        [DisplayName("Check-out Weekday")]
        public string Check_OutDay { get; set; }
        [RegularExpression(@"\s*[0-9]*\.?[0-9]*\s*", ErrorMessage = "{0} is an invalid number")]
        public Nullable<decimal> TotalPayout { get; set; }
        [DisplayName("Weekday Orphan Days")]
        [RegularExpression(@"(\s*[0-9]+)", ErrorMessage = "{0} is an invalid natural number")]
        public Nullable<int> Weekdayorphandays { get; set; }
        public Nullable<int> DaysOut { get; set; }
        public Nullable<int> DaysOutPoints { get; set; }
        [DisplayName("Length of Stay")]
        public Nullable<int> LengthofStay { get; set; }
        [DisplayName("Length of Stay Points")]
        public Nullable<int> LengthofStayPoints { get; set; }
        public Nullable<int> OpenWeekdaysPoints { get; set; }
        public Nullable<decimal> NightlyRate { get; set; }
        [DisplayName("Cleaning Fee")]
        public Nullable<decimal> Cleaning_Fee { get; set; }
        [DisplayName("Require Pricing Approval?")]
        public string Doesitrequire2pricingteamapprovals { get; set; }
        public Nullable<int> TotalPoints { get; set; }
        public string OwnerApprovalNeeded { get; set; }
        [DisplayName("Approval by Owner")]
        public string ApprovedbyOwner { get; set; }
        public string Approvedby2PricingTeamMember { get; set; }
        [DisplayName("Pricing Approver")]
        public string PricingApprover1 { get; set; }
        [DisplayName("Pricing Decision")]
        public string PricingDecision1 { get; set; }
        [DisplayName("Pricing Reason")]
        public string PricingReason1 { get; set; }
        public string PricingApprover2 { get; set; }
        public string PricingDecision2 { get; set; }
        public string PricingReason2 { get; set; }
        public string PricingTeamTimeStamp { get; set; }
        public Nullable<int> InquiryAge { get; set; }
        [DisplayName("Day Still Checkin")]
        public string Daystillcheckin { get; set; }
        public Nullable<DateTime> InquiryCreatedTimestamp { get; set; }
        public virtual CPL CPL { get; set; }

        // house keeping fields
        public Nullable<DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
