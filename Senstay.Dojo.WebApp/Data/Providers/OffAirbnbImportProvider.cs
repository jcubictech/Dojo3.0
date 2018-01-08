using System;
using System.IO;
using System.Collections.Generic;
using OfficeOpenXml;
using System.Linq;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class OffAirbnbImportProvider
    {
        private readonly DojoDbContext _context;
        private const string OFF_AIRBNB_PAYTO_ACCOUNT = "00250069421"; // provided by Jason on 7/27/17
        private const string OFF_AIRBNB_SOURCE = "Off-Airbnb";

        // off airbnb import xlsx column indices (07/27/17 version)
        private const int _leaseIdCol = 1;
        private const int _payoutDateCol = 2;
        private const int _checkinDateCol = 3;
        private const int _checkoutDateCol = 4;
        private const int _nightCol = 5;
        private const int _statusCol = 8;
        private const int _unitNameCol = 11;
        private const int _guestLastNameCol = 13;
        private const int _guestFirstNameCol = 14;
        private const int _grossTotalCol = 23;

        public OffAirbnbImportProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public int ImportExcel(Stream excelData)
        {
            int errorCount = 0;
            int notFoundCount = 0;
            int ownerPayoutCount = 0;
            int totalCols = 29;
            int startRow = 2; // starting row for off-airbnb reservation data
            string inputSource = "Off Airbnb Excel";
            DateTime today = DateTime.UtcNow;

            using (var package = new ExcelPackage(excelData))
            {
                ExcelWorkbook workBook = package.Workbook;
                OffAirbnbRow currentRow = null;
                if (workBook != null)
                {
                    if (workBook.Worksheets.Count > 0)
                    {
                        ExcelWorksheet currentWorksheet = workBook.Worksheets[1];

                        // storage for parsed data
                        var errorRows = new List<InputError>();
                        var ownerPayouts = new List<OwnerPayout>();

                        for (int row = startRow; row <= currentWorksheet.Dimension.End.Row; row++)
                        {
                            if (currentWorksheet.Dimension.End.Column != totalCols)
                            {
                                var message = string.Format("The total number of columns {0:d} does not match {1:d}", currentWorksheet.Dimension.End.Column, totalCols);
                                var inputError = CreateInputError(inputSource, row, "Parse", message, "Excel row");
                                errorRows.Add(inputError);
                                errorCount++;
                            }

                            try
                            {
                                OffAirbnbRow r = ParseOffAirbnbExcelRow(currentWorksheet.Cells, row);
                                if (r == null) continue; // skip the row that does not have checkin date or night

                                currentRow = r;

                                // if property is not fund, we write to input error table to deal with it.
                                if (!EnsurePropertyCode(r.PropertyCode))
                                {
                                    var message = string.Format("Property {0} does not exist.", r.PropertyCode);
                                    var inputError = CreateInputError(inputSource, row, "Off-Airbnb", message , "Excel row");
                                    errorRows.Add(inputError);
                                    notFoundCount++;
                                }
                                else
                                {
                                    // create an owner payout for each reservation
                                    var ownerPayout = MapToOwnerPayout(r);
                                    ownerPayout.Reservations.Add(MapToReservation(r));
                                    ownerPayouts.Add(ownerPayout);
                                    ownerPayoutCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                var message = "Data parse exception: " + ex.Message;
                                var inputError = CreateInputError(inputSource, row, "Exception", message, "Excel row");
                                errorRows.Add(inputError);
                                errorCount++;
                            }
                        }

                        try
                        {
                            // save reservations and resoutions for future payout if there is no error
                            if (errorCount == 0)
                            {
                                if (ownerPayouts != null && ownerPayouts.Count > 0)
                                    _context.OwnerPayouts.AddRange(ownerPayouts);

                                if (errorRows.Count() > 0)
                                    _context.InputErrors.AddRange(errorRows);

                                _context.SaveChanges(); // Save owner payouts and reservations
                            }
                            else
                            {
                                _context.InputErrors.AddRange(errorRows);
                                _context.SaveChanges(); // save errors
                            }
                        }
                        catch (Exception ex)
                        {
                            var message = "Data saving error: " + ex.Message;
                            var inputError = CreateInputError(inputSource, 0, "Exception", message, "Database saving");
                            _context.InputErrors.Add(inputError);
                            _context.SaveChanges(); // save errors
                            errorCount = 100000; // a large number
                        }
                    }
                }
            }

            return errorCount == 0 ? ownerPayoutCount * 10000 + notFoundCount : -errorCount;
        }

        #region private methods

        private OffAirbnbRow ParseOffAirbnbExcelRow(ExcelRange cells, int row)
        {
            if (cells[row, _payoutDateCol].Text == string.Empty &&
                cells[row, _checkinDateCol].Text == string.Empty &&
                cells[row, _nightCol].Text == string.Empty)
                return null;

            var r = new OffAirbnbRow();
            r.ConfirmationCode = GetSafeCellString(cells[row, _leaseIdCol].Value);
            r.PayoutDate = GetSafeDate(cells[row, _payoutDateCol].Text);
            r.CheckinDate = GetSafeDate(cells[row, _checkinDateCol].Text);
            r.Nights = (int)GetSafeNumber(cells[row, _nightCol].Value);
            r.CheckoutDate = r.CheckinDate.Value.AddDays(r.Nights);
            r.Channel = GetChannel(GetSafeCellString(cells[row, _statusCol].Value));
            r.PropertyCode = ParsePropertyCode(GetSafeCellString(cells[row, _unitNameCol].Value));
            r.LastName = GetSafeCellString(cells[row, _guestLastNameCol].Value);
            r.FirstName = GetSafeCellString(cells[row, _guestFirstNameCol].Value);
            r.GrossTotal = GetSafeNumber(cells[row, _grossTotalCol].Value);

            return r;
        }

        private InputError CreateInputError(string inputSource, int row, string section, string message, string origin)
        {
            return new InputError
            {
                InputSource = inputSource,
                Row = row,
                Section = section,
                Message = message,
                OriginalText = origin,
                CreatedTime = DateTime.UtcNow
            };
        }

        private bool EnsurePropertyCode(string propertyCode)
        {
            return _context.CPLs.Where(p => p.PropertyCode == propertyCode).Count() > 0;
        }

        private string GetChannel(string status)
        {
            status = status.ToLower();
            return status.StartsWith("sta") ? "Direct" :
                   (status.StartsWith("bp") ? "Booking.com" :
                   (status.StartsWith("ha") ? "HomeAway" :
                   (status.StartsWith("abl") ? "Maintenance" :
                   (status.StartsWith("priv") ? "Privé" :
                   (status.StartsWith("own") || status.StartsWith("npg") ? "Owner" :
                    "FlipKey")))));
        }

        private string ParsePropertyCode(string unitName)
        {
            string[] tokens = unitName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 0)
                return tokens[0];
            else
                return string.Empty;
        }

        private OwnerPayout MapToOwnerPayout(OffAirbnbRow row)
        {
            var p = new OwnerPayout();
            p.PayoutDate = ConversionHelper.EnsureUtcDate(row.PayoutDate);
            p.DiscrepancyAmount = 0;
            p.IsAmountMatched = true;
            p.InputSource = OFF_AIRBNB_SOURCE;
            p.Source = OFF_AIRBNB_SOURCE;
            p.AccountNumber = OFF_AIRBNB_PAYTO_ACCOUNT;
            p.CreatedDate = ConversionHelper.EnsureUtcDate(DateTime.Now);
            p.ModifiedDate = p.CreatedDate;
            p.Reservations = new List<Reservation>();

            return p;
        }

        private Reservation MapToReservation(OffAirbnbRow row)
        {
            var r = new Reservation();
            r.ConfirmationCode = row.ConfirmationCode;
            r.CheckinDate = ConversionHelper.EnsureUtcDate(row.CheckinDate);
            r.CheckoutDate = ConversionHelper.EnsureUtcDate(row.CheckoutDate);
            r.Nights = row.Nights;
            r.Channel = row.Channel;
            r.PropertyCode = row.PropertyCode;
            r.TransactionDate = ConversionHelper.EnsureUtcDate(row.PayoutDate);
            r.GuestName = string.Format("{0} {1}", row.FirstName, row.LastName);

            r.Source = OFF_AIRBNB_SOURCE;
            r.InputSource = OFF_AIRBNB_SOURCE;
            r.IncludeOnStatement = true;
            r.CreatedDate = ConversionHelper.EnsureUtcDate(DateTime.Now);
            r.ModifiedDate = r.CreatedDate;

            r.TaxRate = 0;
            r.DamageWaiver = 0;
            r.IsTaxed = true; // default for off-airbnb is taxed
            r.TotalRevenue = row.GrossTotal;
            if (r.CheckinDate != null)
            {
                var utcMonth = ConversionHelper.EnsureUtcDate(r.CheckinDate.Value.AddDays(1));
                var feeAndTax = _context.PropertyFees.Where(x => x.PropertyCode == row.PropertyCode && x.EntryDate < utcMonth)
                                                     .OrderByDescending(x => x.EntryDate)
                                                     .FirstOrDefault();
                if (feeAndTax != null && feeAndTax.CityTax != null && feeAndTax.CityTax.Value > 0)
                {
                    var taxRate = feeAndTax.CityTax != null ? feeAndTax.CityTax.Value : 0;
                    var damageWaiver = feeAndTax.DamageWaiver != null ? feeAndTax.DamageWaiver.Value : 0;

                    // calculate total revenue using formular: (gross - damange wavier) / (1.14 + tax rate)
                    if (row.GrossTotal - damageWaiver > 0)
                        r.TotalRevenue = (float)Math.Round((row.GrossTotal - damageWaiver) / (1.14 + taxRate), 2);
                    else // per rule
                        r.TotalRevenue = 0;
                }
            }

            return r;
        }

        private DateTime? GetSafeDate(object data)
        {
            if (data != null)
            {
                DateTime date;
                if (DateTime.TryParse(data.ToString(), out date))
                {
                    return date;
                }
            }
            return null;
        }

        private float GetSafeNumber(object data)
        {
            if (data != null && data.ToString() != string.Empty)
            {
                float number;
                if (float.TryParse(data.ToString(), out number))
                {
                    return number;
                }
            }
            return 0;
        }

        private string GetSafeCellString(object cellValue, string defaultValue = "")
        {
            return cellValue == null ? defaultValue : cellValue.ToString();
        }

        #endregion
    }

    public class OffAirbnbRow
    {
        public string ConfirmationCode { get; set; }
        public DateTime? PayoutDate { get; set; }
        public DateTime? CheckinDate { get; set; }
        public DateTime? CheckoutDate { get; set; }
        public int Nights { get; set; }
        public string Channel { get; set; }
        public string PropertyCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public float GrossTotal { get; set; }
    }
}