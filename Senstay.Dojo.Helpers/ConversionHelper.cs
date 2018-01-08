using System;
using System.Globalization;

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