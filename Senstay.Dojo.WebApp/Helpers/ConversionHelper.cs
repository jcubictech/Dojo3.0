using System;
using System.Globalization;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Helpers
{
    public static class ConversionHelper
    {
        public static DateTime ToUtcFromUs(DateTime pst)
        {
            return pst.Date.AddHours(11);
        }

        public static decimal? RoundToZn(decimal? a, int round)
        {
            if (a.HasValue)
            {
                decimal rez = decimal.Round(a.Value, round);
                return rez;
            }
            else { return null; };

        }

        public static bool? ToNullBool(this string s)
        {
            if(s == "Y" || s == "Yes") { return true; }
            else if (s == "N" || s == "No") { return false; } else
            { return null; };
        }

        public static string DaysLater(this DateTime? date)
        {
            
            if (date == null)
            {
                return null;
            }
            DateTime finder = date.Value;
            TimeSpan time = DateTime.Now - finder;
            int daysRez = time.Days;
            if (daysRez > 30)
            {
                return null;
            } else
            {
                return "New";
            }
        }

        public static int? ToNullableInt32(this string s)
        {
            int i;
            if (Int32.TryParse(s, out i)) return i;
            return null;
        }

        public static decimal? ToNullabledecimal(this string s)
        {
            decimal i;
            NumberStyles style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
            if (decimal.TryParse(s, style, CultureInfo.CurrentCulture, out i)) return i;
            return null;
        }

        public static float? ToNullableFloat(this string s)
        {
            float i;
            if (float.TryParse(s, out i)) return i;
            return null;
        }

        public static DateTime? AddNulldableDate(DateTime? d1, DateTime? d2)
        {
            if (d1 == null & d2 == null) { return null; } else
                if (!d1.HasValue) { return d2; } else
                if (!d2.HasValue) { return d1; } else
            {
                TimeSpan timespan = new TimeSpan(d2.Value.Hour, d2.Value.Minute, 0);
                DateTime a = d1.Value.Add(timespan);
                return a;
            };
        }

        public static DateTime? ToHours(this string s)
        {
            if (s.Contains("N/A") || s == "") { return null; }
            DateTime dt = new DateTime();
            if (DateTime.TryParseExact(s, "h:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) { return dt; }
            return dt;
        }

        public static string CurrencyParse(this string s)
        {
            if (s == null) { return "$"; };
            if (s.ToLower().Contains("usd")) { return "$"; };
            if (s.ToLower().Contains("real")) { return "R$"; };
            if (s.ToLower().Contains("Euro")) { return "€"; };
            if (s.ToLower().Contains("Pounds")) { return "£"; };
            return "$";
        }

        public static DateTime? ToDateTime(this string s)
        {
            if (String.IsNullOrWhiteSpace(s) || s.Contains("N/A")) {
                return null; 
            }
            DateTime dt = DateTime.Now;
            bool result = DateTime.TryParseExact(s, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);

            if (result)
            {
                return dt;
            }

            result = DateTime.TryParseExact(s, "M/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
            if (result)
            {
                return dt;
            }

            result = DateTime.TryParseExact(s, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
            if (result)
            {
                return dt;
            }

            result = DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt);
            if (result)
            {
                return dt;
            }

            return dt;
            //throw new Exception();
        }

        public static AirbnbAccount ToAirbnbAccounts(this string[] s)
        {
            AirbnbAccount AA = new AirbnbAccount();
            AA.Email = s[0];
            AA.Password = s[1];
            AA.Gmailpassword = s[2];
            AA.Status = s[3];
            AA.DateAdded = s[4].ToDateTime();
            AA.SecondaryAccountEmail = s[5];
            AA.AccountAdmin = s[6];
            AA.Vertical = s[7];
            AA.Owner_Company = s[8];
            AA.Name = s[9];
            AA.PhoneNumber1 = s[10];
            AA.PhoneNumberOwner = s[11];
            AA.DOB1 = s[12].ToDateTime();
            AA.Payout_Method = s[13];
            AA.PointofContact = s[14];
            AA.PhoneNumber2 = s[15];
            AA.DOB2 = s[16].ToDateTime();
            AA.EmailAddress = s[17];
            AA.ActiveListings = s[18].ToNullableInt32();
            AA.Pending_Onboarding = s[19].ToNullableInt32();
            AA.In_activeListings = s[20].ToNullableInt32();
            AA.ofListingsinLAMarket = s[21].ToNullableInt32();
            AA.ofListingsinNYCMarket = s[22].ToNullableInt32();
            AA.ProxyIP = s[23];
            AA.C2ndProxyIP = s[24];

            return AA;
        }

        public static InquiriesValidation ToInquiriesValidation(this string[] s)
        {
            InquiriesValidation IV = new InquiriesValidation();
            //IV.IsUpload = true; TODO: check this one
            IV.GuestName = s[0];
            IV.InquiryTeam = s[1];
            IV.AirBnBListingTitle = s[2];
            IV.PropertyCode = s[3];
            IV.Channel = s[4];
            IV.BookingGuidelines = s[5];
            IV.AdditionalInfo_StatusofInquiry = s[6];
            IV.AirBnBURL = s[7];
            IV.Bedrooms = s[8].ToNullableInt32();
            IV.Account = s[9];
            IV.InquiryCreatedTimestamp = AddNulldableDate(s[10].ToDateTime(), s[11].ToDateTime());
            //IV.InquiryTime__PST_ = s[11].ToDateTime();
            IV.Check_inDate = s[12].ToDateTime();
            IV.Check_InDay = s[13];
            IV.Check_outDate = s[14].ToDateTime();
            IV.Check_OutDay = s[15];
            IV.TotalPayout = s[16].ToNullabledecimal();
            IV.Weekdayorphandays = s[17].ToNullableInt32();
            
            IV.DaysOutPoints = s[19].ToNullableInt32();
            IV.LengthofStay = s[20].ToNullableInt32();
            IV.LengthofStayPoints = s[21].ToNullableInt32();
            IV.OpenWeekdaysPoints = s[22].ToNullableInt32();
            IV.NightlyRate = s[23].ToNullabledecimal();
            //IV.Cleaning_Fee = s[24].ToNullabledecimal();
            IV.Doesitrequire2pricingteamapprovals = s[25];
            //IV.DoesitrequireInquiryTeamLeadapproval = s[26];
            IV.TotalPoints = s[27].ToNullableInt32();
            IV.OwnerApprovalNeeded = s[28];
            IV.ApprovedbyOwner = s[29];
            IV.PricingApprover1 = s[31];
            IV.PricingDecision1 = s[32];
            IV.PricingReason1 = s[33];
            //IV.PricingApprover2 = s[33];
            //IV.PricingDecision2 = s[34];
            //IV.PricingReason2 = s[35];
            //IV.PricingTeamTimeStamp = s[36];
            //IV.InquiryAge = s[37].ToNullableInt32();
            //IV.Daystillcheckin = s[38];
            //calculated
            if (IV.Check_inDate.HasValue && IV.InquiryCreatedTimestamp.HasValue)
            {
                IV.DaysOut = (int) (IV.Check_inDate.Value - IV.InquiryCreatedTimestamp.Value).TotalDays;
            }
            else
            {
                IV.DaysOut = s[18].ToNullableInt32();
            }

            if (IV.Check_inDate.HasValue && IV.Check_outDate.HasValue)
            {
                IV.LengthofStay = (int)(IV.Check_outDate.Value - IV.Check_inDate.Value).TotalDays;
            }
            else
            {
                IV.LengthofStay = s[20].ToNullableInt32();
            }

            return IV;
        }

        public static CPL ToCPL(this string[] s)
        {
            CPL booking = new CPL();

            
            booking.AirBnBHomeName = s[0];
            booking.PropertyCode= s[1];
            booking.Address = s[2];
            booking.PropertyStatus = s[3];
            booking.Vertical = s[4];
            booking.Owner = s[5];
            booking.NeedsOwnerApproval = s[6].ToNullBool();
            booking.ListingStartDate = s[7].ToDateTime();
            booking.StreamlineHomeName = s[9];
            booking.StreamlineUnitID = s[10];
            booking.Account = s[11];
            booking.City = s[13];
            booking.Market = s[14];
            booking.State = s[15];
            booking.Area = s[16];
            booking.Neighborhood = s[17];
            booking.BookingGuidelines = s[19];
            //booking.Quarantine = s[20];
            //booking.Lease_ContractStartDate = s[21].ToDateTime();
            //booking.Lease_ContractEndDate = s[22];
            booking.Floor = s[23];
            booking.Bedrooms = s[24].ToNullableInt32();
            booking.Bathrooms = s[25].ToNullableFloat(); 
            booking.BedsDescription = s[26];
            booking.MaxOcc = s[27].ToNullableInt32();
            booking.StdOcc = s[28].ToNullableInt32();
            booking.Elevator = s[29];
            booking.A_C = s[30];
            booking.Parking = s[31];
            booking.WiFiNetwork = s[32];
            booking.WiFiPassword = s[33];
            booking.Ownership = s[34].ToNullableFloat();
            booking.MonthlyRent = s[35].ToNullabledecimal();
            booking.DailyRent = s[36].ToNullabledecimal();
            //booking.MethodofRent_Payment = s[37];
            booking.CleaningFees = s[38].ToNullableInt32();
            //booking.AirBnBURL = s[39];
            booking.AIrBnBID = s[40];
            //booking.HomeTripperURL = s[41];
            booking.AirbnbiCalexportlink = s[42];
            booking.Amenities = s[43];
            booking.Zipcode = s[44];
            booking.CheckInType = s[45];
            booking.OldListingTitle = s[46];
            booking.GoogleDrivePicturesLink = s[47];
            booking.SquareFootage = s[48].ToNullableInt32();
            booking.Password = s[49];
            booking.Currency = s[50].CurrencyParse();
            booking.InquiryLeadApproval = s[51].ToNullBool();
            //booking.RevTeam2xApproval = s[52].ToNullBool();
            booking.PendingContractDate = s[53].ToDateTime();
            booking.PendingOnboardingDate = s[54].ToDateTime();
            //booking.AccountActive = s[55];
            booking.SecurityDeposit = s[56];
            booking.Pool = s[57];
            booking.HomeAway = s[58];
            booking.AirBnb = s[59];
            booking.FlipKey = s[60];
            booking.Expedia = s[61];
            booking.Inactive = s[62].ToDateTime();
            
            return booking;
        }

        public static string FormatMoney(double money)
        {
            return money.ToString("c2").Replace("$", "");
        }

        public static string FormatMoney(float money)
        {
            return money.ToString("c2").Replace("$", "");
        }

        public static DateTime EnsureUtcDate(DateTime date)
        {
            return date.Date.AddHours(11);
        }

        public static DateTime? EnsureUtcDate(DateTime? date)
        {
            return date == null ? date : date.Value.Date.AddHours(11);
        }

        public static DateTime MonthWithLastDay(DateTime date)
        {
            return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        }

        public static bool ZeroMoneyValue(double d)
        {
            return Math.Abs(d) < 0.01;
        }

        public static bool MoneyEqual(double d1, double d2)
        {
            return Math.Abs(d1 - d2) < 0.01;
        }

        public static bool MoneyEqual(float d1, float d2)
        {
            return Math.Abs(d1 - d2) < 0.01;
        }
    }
}