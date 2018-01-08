using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    public class ExportDbController : AppBaseController
    {
        private readonly DojoDbContext _dbContext;

        public ExportDbController(DojoDbContext context)
        {
            _dbContext = context;
        }

        // GET: ExportDb
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DownloadTable()
        {
            return View();
        }

        public ActionResult Download(string db)
        {
            if (db == "property")
                return DownloadCPL();
            else if (db == "airbnbaccount")
                return DownloadAA();
            else
                return DownloadInV();
        }

        public FileResult DownloadAA()
        {
            // use the submitted download token value to set a cookie in the response
            Response.AppendCookie(new HttpCookie(AppConstants.DOWNLOAD_COOKIE_NAME, "done"));

            StringBuilder accountTable = new StringBuilder();
            string separator = "\t";

            accountTable.Append("Email" + separator);
            accountTable.Append("Password" + separator);
            accountTable.Append("Gmail password" + separator);
            accountTable.Append("Status" + separator);
            accountTable.Append("DateAdded" + separator);
            accountTable.Append("Secondary Account Email" + separator);
            accountTable.Append("Account Admin" + separator);
            accountTable.Append("Vertical" + separator);
            accountTable.Append("Owner\\Company" + separator);
            accountTable.Append("Name" + separator);
            accountTable.Append("PhoneNumber" + separator);
            accountTable.Append("Phone Number Owner" + separator);
            accountTable.Append("DOB"  + separator);
            accountTable.Append("Payout Method" + separator);
            accountTable.Append("Point of Contact" + separator);
            accountTable.Append("Phone Number" + separator);
            accountTable.Append("DOB" + separator);
            accountTable.Append("Email Address" + separator);
            accountTable.Append("Active Listings" + separator);
            accountTable.Append("Pending\\Onboarding" + separator);
            accountTable.Append("In active Listings" + separator);
            accountTable.Append("# of Listingsin LA Market" + separator);
            accountTable.Append("# of Listingsin NY CMarket" + separator);
            accountTable.Append("ProxyIP" + separator);
            accountTable.Append("C2ndProxyIP");
            accountTable.Append("\n");

            //AirbnbAccounts AA = new AirbnbAccounts();
            foreach (AirbnbAccount AA in _dbContext.AirbnbAccounts)
            {
                accountTable.Append("\"" +AA.Email + "\"" + separator);
                accountTable.Append("\"" +AA.Password + "\"" + separator);
                accountTable.Append("\"" +AA.Gmailpassword + "\"" + separator);
                accountTable.Append("\"" +AA.Status + "\"" + separator);
                accountTable.Append("\"" +(AA.DateAdded != null ? AA.DateAdded.Value.ToString("d") : "") + "\"" + separator);
                accountTable.Append("\"" +AA.SecondaryAccountEmail + "\"" + separator);
                accountTable.Append("\"" +AA.AccountAdmin + "\"" + separator);
                accountTable.Append("\"" +AA.Vertical + "\"" + separator);
                accountTable.Append("\"" +AA.Owner_Company + "\"" + separator);
                accountTable.Append("\"" +AA.Name + "\"" + separator);
                accountTable.Append("\"" +AA.PhoneNumber1 + "\"" + separator);
                accountTable.Append("\"" +AA.PhoneNumberOwner + "\"" + separator);
                accountTable.Append("\"" +(AA.DOB1 != null ? AA.DOB1.Value.ToString("d") : "") + "\"" + separator);
                accountTable.Append("\"" +AA.Payout_Method + "\"" + separator);
                accountTable.Append("\"" +AA.PointofContact + "\"" + separator);
                accountTable.Append("\"" +AA.PhoneNumber2 + "\"" + separator);
                accountTable.Append("\"" +(AA.DOB2 != null ? AA.DOB2.Value.ToString("d") : "") + "\"" + separator);
                accountTable.Append("\"" +AA.EmailAddress + "\"" + separator);
                accountTable.Append("\"" +AA.ActiveListings + "\"" + separator);
                accountTable.Append("\"" +AA.Pending_Onboarding + "\"" + separator);
                accountTable.Append("\"" +AA.In_activeListings + "\"" + separator);
                accountTable.Append("\"" +AA.ofListingsinLAMarket + "\"" + separator);
                accountTable.Append("\"" +AA.ofListingsinNYCMarket + "\"" + separator);
                accountTable.Append("\"" +AA.ProxyIP + "\"" + separator);
                accountTable.Append("\"" +AA.C2ndProxyIP + "\"" + separator);
                accountTable.Append("\n");

            }

            var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), Guid.NewGuid().ToString() + "_" + "output.tsv");
            using (StreamWriter outfile = new StreamWriter(path))
            {
                outfile.Write(accountTable.ToString());
            }

            return File(path, System.Net.Mime.MediaTypeNames.Application.Octet, ("AirbnbAccounts_" + DateTime.Now.ToString("MM_dd_yyyy_hh_mm_ss_tt") + ".tsv"));
        }

        public FileResult DownloadCPL()
        {
            // use the submitted download token value to set a cookie in the response
            Response.AppendCookie(new HttpCookie(AppConstants.DOWNLOAD_COOKIE_NAME, "done"));

            List<CPL> allcpldb = new List<CPL>();
            StringBuilder accountTable = new StringBuilder();
            string separator = "\t";

            accountTable.Append("Created Date" + separator);
            accountTable.Append("Air BnB Home Name" + separator);
            accountTable.Append("Property Code" + separator);
            accountTable.Append("Address" + separator);
            accountTable.Append("Property Status" + separator);
            accountTable.Append("Vertical" + separator);
            accountTable.Append("Owner" + separator);
            accountTable.Append("Needs Owner Approval" + separator);
            accountTable.Append("Listing Start Date" + separator);
            accountTable.Append("Streamline Home Name" + separator);
            accountTable.Append("Streamline Unit ID" + separator);
            accountTable.Append("Account" + separator);
            accountTable.Append("City" + separator);
            accountTable.Append("Market" + separator);
            accountTable.Append("State" + separator);
            accountTable.Append("Area" + separator);
            accountTable.Append("Neighborhood" + separator);
            accountTable.Append("Booking Guidelines" + separator);
            //accountTable.Append( "Quarantine" + separator);
            //accountTable.Append( "Lease_ContractStartDate" + separator);
            //accountTable.Append( "Lease_ContractEndDate" + separator);
            accountTable.Append("Floor" + separator);
            accountTable.Append("Bedrooms" + separator);
            accountTable.Append("Bathrooms" + separator);
            accountTable.Append("Beds Description" + separator);
            accountTable.Append("Max Occ" + separator);
            accountTable.Append("Std Occ" + separator);
            accountTable.Append("Elevator" + separator);
            accountTable.Append("A\\C" + separator);
            accountTable.Append("Parking" + separator);
            accountTable.Append("Pool" + separator);
            accountTable.Append("Wi-Fi Network" + separator);
            accountTable.Append("Wi-Fi Password" + separator);
            accountTable.Append("Ownership" + separator);
            accountTable.Append("Monthly Rent" + separator);
            accountTable.Append("Daily Rent" + separator);
            //accountTable.Append( "MethodofRent_Payment" + separator);
            accountTable.Append("Cleaning Fees" + separator);
            accountTable.Append("Currency" + separator);
            //accountTable.Append( "AirBnBURL" + separator);
            accountTable.Append("Airbnb ID" + separator);
            //accountTable.Append( "HomeTripperURL" + separator);
            accountTable.Append("Airbnb iCal" + separator);
            accountTable.Append("Amenities" + separator);
            accountTable.Append("Zip code" + separator);
            accountTable.Append("Check-in Type" + separator);
            accountTable.Append("Old Listing Title" + separator);
            accountTable.Append("Google Drive Pictures Link" + separator);
            accountTable.Append("Square Footage" + separator);
            accountTable.Append("Password" + separator);
            //accountTable.Append( "CancellationPolicy" + separator);
            //accountTable.Append("InquiryLeadApproval" + separator);
            //accountTable.Append("Require 2x Rev Team Approval?" + separator);
            accountTable.Append("Pending Contract Date" + separator);
            accountTable.Append("Pending Onboarding Date" + separator);
            //accountTable.Append( "AccountActive" + separator);
            accountTable.Append("Security Deposit" + separator);
            accountTable.Append("Home Away" + separator);
            accountTable.Append("AirBnb" + separator);
            accountTable.Append("FlipKey" + separator);
            accountTable.Append("Expedia" + separator);
            accountTable.Append("Inactive" + separator);
            accountTable.Append("Belt Designation" + separator);
            accountTable.Append("\n");

            foreach (CPL AA in _dbContext.CPLs)
            {
                accountTable.Append("\""+ AA.CreatedDate + "\"" + separator);
                accountTable.Append("\""+ AA.AirBnBHomeName + "\"" + separator);
                accountTable.Append("\""+ AA.PropertyCode + "\"" + separator);
                accountTable.Append("\""+ AA.Address + "\"" + separator);
                accountTable.Append("\""+ AA.PropertyStatus + "\"" + separator);
                accountTable.Append("\""+ AA.Vertical + "\"" + separator);
                accountTable.Append("\""+ AA.Owner + "\"" + separator);
                accountTable.Append("\""+ AA.NeedsOwnerApproval + "\"" + separator);
                accountTable.Append("\""+(AA.ListingStartDate != null ? AA.ListingStartDate.Value.ToString("d") : "") + "\"" + separator);
                accountTable.Append("\""+ AA.StreamlineHomeName + "\"" + separator);
                accountTable.Append("\""+ AA.StreamlineUnitID + "\"" + separator);
                accountTable.Append("\""+ AA.Account + "\"" + separator);
                accountTable.Append("\""+ AA.City + "\"" + separator);
                accountTable.Append("\""+ AA.Market + "\"" + separator);
                accountTable.Append("\""+ AA.State + "\"" + separator);
                accountTable.Append("\""+ AA.Area + "\"" + separator);
                accountTable.Append("\""+ AA.Neighborhood + "\"" + separator);
                accountTable.Append("\""+ AA.BookingGuidelines + "\"" + separator);
                //accountTable.Append("\""+ AA.Quarantine + "\"" + separator);
                //accountTable.Append("\""+ AA.Lease_ContractStartDate + "\"" + separator);
                //accountTable.Append("\""+ AA.Lease_ContractEndDate + "\"" + separator);
                accountTable.Append("\""+ AA.Floor + "\"" + separator);
                accountTable.Append("\""+ AA.Bedrooms + "\"" + separator);
                accountTable.Append("\""+ AA.Bathrooms + "\"" + separator);
                accountTable.Append("\""+ AA.BedsDescription + "\"" + separator);
                accountTable.Append("\""+ AA.MaxOcc + "\"" + separator);
                accountTable.Append("\""+ AA.StdOcc + "\"" + separator);
                accountTable.Append("\""+ AA.Elevator + "\"" + separator);
                accountTable.Append("\""+ AA.A_C + "\"" + separator);
                accountTable.Append("\""+ AA.Parking + "\"" + separator);
                accountTable.Append("\""+ AA.Pool + "\"" + separator);
                accountTable.Append("\""+ AA.WiFiNetwork + "\"" + separator);
                accountTable.Append("\""+ AA.WiFiPassword + "\"" + separator);
                accountTable.Append("\""+ AA.Ownership + "\"" + separator);
                accountTable.Append("\""+ AA.MonthlyRent + "\"" + separator);
                accountTable.Append("\""+ AA.DailyRent + "\"" + separator);
                //accountTable.Append("\""+ AA.MethodofRent_Payment + "\"" + separator);
                accountTable.Append("\""+ AA.CleaningFees + "\"" + separator);
                accountTable.Append("\"" + AA.Currency + "\"" + separator);
                //accountTable.Append("\""+ AA.AirBnBURL + "\"" + separator);
                accountTable.Append("\""+ AA.AIrBnBID + "\"" + separator);
                //accountTable.Append("\""+ AA.HomeTripperURL + "\"" + separator);
                accountTable.Append("\""+ AA.AirbnbiCalexportlink + "\"" + separator);
                accountTable.Append("\""+ AA.Amenities + "\"" + separator);
                accountTable.Append("\""+ AA.Zipcode + "\"" + separator);
                accountTable.Append("\"" + AA.CheckInType + "\"" + separator);
                accountTable.Append("\""+ AA.OldListingTitle + "\"" + separator);
                accountTable.Append("\""+ AA.GoogleDrivePicturesLink + "\"" + separator);
                accountTable.Append("\""+ AA.SquareFootage + "\"" + separator);
                accountTable.Append("\""+ AA.Password + "\"" + separator);
                //accountTable.Append("\""+ AA.CancellationPolicy + "\"" + separator);
                //accountTable.Append("\""+ AA.InquiryLeadApproval + "\"" + separator);
                //accountTable.Append("\""+ AA.RevTeam2xApproval + "\"" + separator);
                accountTable.Append("\""+ AA.PendingContractDate + "\"" + separator);
                accountTable.Append("\""+ AA.PendingOnboardingDate + "\"" + separator);
                //accountTable.Append("\""+ AA.AccountActive + "\"" + separator);
                accountTable.Append("\""+ AA.SecurityDeposit + "\"" + separator);
                accountTable.Append("\""+ AA.HomeAway + "\"" + separator);
                accountTable.Append("\""+ AA.AirBnb + "\"" + separator);
                accountTable.Append("\""+ AA.FlipKey + "\"" + separator);
                accountTable.Append("\""+ AA.Expedia + "\"" + separator);
                accountTable.Append("\""+ (AA.Inactive != null ? AA.Inactive.Value.ToString("d") : "") + "\"" + separator);
                accountTable.Append("\"" + AA.BeltDesignation + "\"" + separator);

                accountTable.Append("\n");

            }

            var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), Guid.NewGuid().ToString() + "_" + "output.tsv");
            using (StreamWriter outfile = new StreamWriter(path))
            {
                outfile.Write(accountTable.ToString());
            };

            return File(path, System.Net.Mime.MediaTypeNames.Application.Octet, ("CPL_" + DateTime.Now.ToString("MM_dd_yyyy_hh_mm_ss_tt") + ".tsv"));
        }

        public FileResult DownloadInV()
        {
            // use the submitted download token value to set a cookie in the response
            Response.AppendCookie(new HttpCookie(AppConstants.DOWNLOAD_COOKIE_NAME, "done"));

            StringBuilder accountTable = new StringBuilder();
            string separator = "\t";

            accountTable.Append("Guest Name"  + separator);
            accountTable.Append("Inquiry Specialist" + separator);
            accountTable.Append("AirBnBListing Title"  + separator);
            accountTable.Append("Property Code"  + separator);
            accountTable.Append("Channel"  + separator);
            accountTable.Append("Booking Guidelines"  + separator);
            accountTable.Append("Summary" + separator);
            accountTable.Append("Airbnb Link" + separator);
            accountTable.Append("Bedrooms"  + separator);
            accountTable.Append("Account"  + separator);
            accountTable.Append("Inquiry Created Timestamp"  + separator);
            //accountTable.Append( "InquiryTime__PST_"  + separator);
            accountTable.Append("Check-in Date"  + separator);
            accountTable.Append("Check-in Day"  + separator);
            accountTable.Append("Check-out Date"  + separator);
            accountTable.Append("Check-out Day"  + separator);
            accountTable.Append("Total Payout"  + separator);
            accountTable.Append("Weekday orphan days"  + separator);
            accountTable.Append("Days Out"  + separator);
            accountTable.Append("Days Out Points"  + separator);
            accountTable.Append("Length of Stay"  + separator);
            accountTable.Append("Length of Stay Points"  + separator);
            accountTable.Append("Open Weekdays Points"  + separator);
            accountTable.Append("Nightly Rate"  + separator);
            //accountTable.Append("Doesitrequire2pricingteamapprovals"  + separator);
            //accountTable.Append( "DoesitrequireInquiryTeamLeadapproval"  + separator);
            accountTable.Append("Total Points"  + separator);
            accountTable.Append("Owner Approval Needed"  + separator);
            accountTable.Append("Approved by Owner"  + separator);
            accountTable.Append("Pricing Approver"  + separator);
            accountTable.Append("Pricing Decision"  + separator);
            accountTable.Append("Pricing Reason"  + separator);
            //accountTable.Append("PricingApprover2"  + separator);
            //accountTable.Append("PricingDecision2"  + separator);
            //accountTable.Append("PricingReason2"  + separator);
            accountTable.Append("Pricing Team Timestamp"  + separator);
            accountTable.Append("InquiryAge"  + separator);
            accountTable.Append("Days Till Check-In" + separator);
            accountTable.Append("\n");

            //AirbnbAccounts AA = new AirbnbAccounts();
            var inquires = _dbContext.InquiriesValidations.Where(i => i.Id > 0).ToList();
            foreach (InquiriesValidation aa in inquires)
            {
                accountTable.Append( "\"" +aa.GuestName + "\"" + separator);
                accountTable.Append( "\"" +aa.InquiryTeam + "\"" + separator);
                accountTable.Append( "\"" +aa.AirBnBListingTitle + "\"" + separator);
                accountTable.Append( "\"" +aa.PropertyCode + "\"" + separator);
                accountTable.Append( "\"" +aa.Channel + "\"" + separator);

                try
                {
                    accountTable.Append("\"" + aa.CPL.BookingGuidelines + "\"" + separator);
                }
                catch
                {
                    accountTable.Append("\"\"" + separator);
                }

                accountTable.Append( "\"" +aa.AdditionalInfo_StatusofInquiry + "\"" + separator);
                accountTable.Append( "\"" +aa.AirBnBURL + "\"" + separator);
                accountTable.Append( "\"" +aa.Bedrooms + "\"" + separator);
                accountTable.Append( "\"" +aa.Account + "\"" + separator);
                accountTable.Append( "\"" +aa.InquiryCreatedTimestamp + "\"" + separator);
                //accountTable.Append( "\"" +AA.InquiryTime__PST_ + "\"" + separator);
                accountTable.Append( "\"" +aa.Check_inDate + "\"" + separator);
                accountTable.Append( "\"" +aa.Check_InDay + "\"" + separator);
                accountTable.Append( "\"" +aa.Check_outDate + "\"" + separator);
                accountTable.Append( "\"" +aa.Check_OutDay + "\"" + separator);
                accountTable.Append( "\"" +aa.TotalPayout + "\"" + separator);
                accountTable.Append( "\"" +aa.Weekdayorphandays + "\"" + separator);
                accountTable.Append( "\"" +aa.DaysOut + "\"" + separator);
                accountTable.Append( "\"" +aa.DaysOutPoints + "\"" + separator);
                accountTable.Append( "\"" +aa.LengthofStay + "\"" + separator);
                accountTable.Append( "\"" +aa.LengthofStayPoints + "\"" + separator);
                accountTable.Append( "\"" +aa.OpenWeekdaysPoints + "\"" + separator);
                accountTable.Append( "\"" +aa.NightlyRate + "\"" + separator);
                //accountTable.Append( "\"" +AA.Doesitrequire2pricingteamapprovals + "\"" + separator);
                //accountTable.Append( "\"" +AA.DoesitrequireInquiryTeamLeadapproval + "\"" + separator);
                accountTable.Append( "\"" +aa.TotalPoints + "\"" + separator);
                accountTable.Append( "\"" +aa.OwnerApprovalNeeded + "\"" + separator);
                accountTable.Append( "\"" +aa.ApprovedbyOwner + "\"" + separator);
                accountTable.Append( "\"" +aa.PricingApprover1 + "\"" + separator);
                accountTable.Append( "\"" +aa.PricingDecision1 + "\"" + separator);
                accountTable.Append( "\"" +aa.PricingReason1 + "\"" + separator);
                //accountTable.Append( "\"" +AA.PricingApprover2 + "\"" + separator);
                //accountTable.Append( "\"" +AA.PricingDecision2 + "\"" + separator);
                //accountTable.Append( "\"" +AA.PricingReason2 + "\"" + separator);
                accountTable.Append( "\"" +aa.PricingTeamTimeStamp + "\"" + separator);
                accountTable.Append( "\"" +aa.InquiryAge + "\"" + separator);
                accountTable.Append( "\"" +aa.Daystillcheckin + "\"" + separator);
                accountTable.Append("\n");

            }

            var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), Guid.NewGuid().ToString() + "_" + "output.tsv");
            using (StreamWriter outfile = new StreamWriter(path))
            {
                outfile.Write(accountTable.ToString());
            }

            return File(path, System.Net.Mime.MediaTypeNames.Application.Octet, ("Inquiries_Validation_" + DateTime.Now.ToString("MM_dd_yyyy_hh_mm_ss_tt") + ".tsv"));
        }
    }
}