using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using OfficeOpenXml;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Models.HelperClass;
using Senstay.Dojo.Models.Grid;

namespace Senstay.Dojo.Data.Providers
{
    public class OwnerPayoutProvider : CrudProviderBase<OwnerPayout>
    {
        private readonly DojoDbContext _context;

        public OwnerPayoutProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        #region custom methods

        public List<OwnerPayout> All()
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[0];
                List<OwnerPayout> data = _context.Database.SqlQuery<OwnerPayout>("RetrieveReservations", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<OwnerPayout> Get(string porpertyCode)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@PropertyCode", SqlDbType.VarChar);
                sqlParams[0].Value = porpertyCode;

                List<OwnerPayout> data = _context.Database.SqlQuery<OwnerPayout>("RetreivewOwnerPayouts @PropertyCode", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<OwnerPayout> Retrieve(DateTime beginDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = beginDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                List<OwnerPayout> data = _context.Database.SqlQuery<OwnerPayout>("RetrieveOwnerPayouts @StartDate, @EndDate", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool Exist(string propertyCode)
        {
            bool exist = false;
            var count = _context.Reservations.Where(x => x.PropertyCode == propertyCode)
                                             .Count();
            exist = count > 0;
            return exist;
        }

        public bool UpdateOwnerPayoutMatchStatus(int ownerPayoutId)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@OwnerPayoutId", SqlDbType.Int);
                sqlParams[0].Value = ownerPayoutId;

                SqlResult result = _context.Database.SqlQuery<SqlResult>("UpdateOwnerPayoutMatchStatus @OwnerPayoutId", sqlParams).FirstOrDefault();
                return result.Count > 0;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public void MapData(OwnerPayoutRevenueModel model, OwnerPayout entity, bool isNew = false)
        {
            // map form fields
            entity.AccountNumber = model.PayToAccount;
            entity.PayoutAmount = model.PayoutAmount;
            entity.PayoutDate = ConversionHelper.EnsureUtcDate(model.PayoutDate);
            entity.Source = model.Source;

            if (isNew)
            {
                entity.IsDeleted = false;
                entity.InputSource = AppConstants.MANUAL_INPUT_SOURCE;
                entity.DiscrepancyAmount = 0;
                entity.IsAmountMatched = true;
            }
            else
            {
                // DiscrepancyAmount field are calcuated on-the-fly in owner payout query
                // IsAmountMatched is obsolete 
            }
        }

        #endregion

        #region Import methods

        public string SaveToExcel(string csvfile)
        {
            try
            {
                char delimeter = GetDelimiterInFile(csvfile);
                string excelFile = Path.ChangeExtension(csvfile, "xlsx");
                using (ExcelPackage package = new ExcelPackage())
                {
                    ExcelTextFormat format = new ExcelTextFormat();
                    format.Delimiter = delimeter;
                    bool hasHeader = true;
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("OwnerPayout");
                    worksheet.Cells["A1"].LoadFromText(new FileInfo(csvfile), format, OfficeOpenXml.Table.TableStyles.None, hasHeader);
                    package.SaveAs(new FileInfo(excelFile));
                }
                return excelFile;
            }
            catch(Exception ex)
            {
                string s = ex.Message;
            }
            return string.Empty;
        }

        public int ImportFromExcel(string excelFile, DateTime reportDate, bool isCompleted)
        {
            int errorCount = 0;
            int payoutCount = 0;
            int reservationCount = 0;
            int resolutionCount = 0;
            string channel = "Airbnb";
            string account = GetAccountFromFilename(excelFile);
            string inputSource = string.Format("{0} {1}", reportDate.ToString("yyyy-MM-dd"), GetInputSourceFromFilePath(excelFile));
            string excelPath = Path.Combine(UrlHelper.DataRootUrl(), excelFile);

            if (!File.Exists(excelPath))
            {
                var inputError = new InputError
                {
                    InputSource = inputSource,
                    Row = 0,
                    Section = "Input File Check",
                    Message = string.Format("Input file '{0}' does not exist.", excelPath),
                    OriginalText = excelPath,
                    CreatedTime = DateTime.UtcNow
                };
                _context.InputErrors.Add(inputError);
                _context.SaveChanges(); // save errors

                return -1000000; // large number of error
            }

            FileInfo excelFileInfo = new FileInfo(excelPath);
            using (var package = new ExcelPackage(excelFileInfo))
            {
                ExcelWorkbook workBook = package.Workbook;
                if (workBook != null)
                {
                    if (workBook.Worksheets.Count > 0)
                    {
                        int totalCols = 14;
                        int startRow = 2; // starting row for reservation data
                        int payoutDateCol = 1;
                        int typeCol = 2;
                        int confirmationCodeCol = 3;
                        int checkinDateCol = 4;
                        int nightCol = 5;
                        int guestCol = 6;
                        int listingCol = 7;
                        int detailCol = 8;
                        int referenceCol = 9;
                        int currencyCol = 10;
                        int amountCol = 11;
                        int payoutCol = 12;
                        //int hostFeeCol = 13;
                        //int cleanFeeCol = 14;

                        ExcelWorksheet currentWorksheet = workBook.Worksheets[1];

                        bool isOwnerRow = false;
                        bool isRowError = false;
                        List<InputError> errorRows = new List<InputError>();

                        OwnerPayout payout = null;
                        List<Reservation> reservations = null;
                        List<Resolution> resolutions = null;
                        DateTime today = DateTime.UtcNow;
                        PropertyProvider propertyProvider = new PropertyProvider(_context);
                        for (int row = startRow; row <= currentWorksheet.Dimension.End.Row; row++)
                        {
                            if (currentWorksheet.Dimension.End.Column != totalCols)
                            {
                                errorRows.Add(new InputError
                                {
                                    InputSource = inputSource,
                                    Row = row,
                                    Section = "Parse",
                                    Message = string.Format("The total number of columns {0:d} does not match {1:d}", currentWorksheet.Dimension.End.Column, totalCols),
                                    OriginalText = "Excel row",
                                    CreatedTime = DateTime.UtcNow
                                });
                            }

                            try
                            {
                                if (isCompleted && currentWorksheet.Cells[row, payoutDateCol].Value == null)
                                {
                                    if (payout != null && isOwnerRow && !isRowError)
                                    {
                                        QueuePayout(payout, reservations, resolutions);
                                        payoutCount++;
                                    }
                                    break; // exit loop at the end of row
                                }

                                string type = GetSafeCellString(currentWorksheet.Cells[row, typeCol].Value);
                                var payoutType = type.StartsWith("Payout") ? PayoutType.Payout :
                                                 (type.StartsWith("Reservation") ? PayoutType.Reservation :
                                                 (type.StartsWith("Resolution") ? PayoutType.Resolution : PayoutType.Other));

                                if (payoutType == PayoutType.Payout)
                                {
                                    if (isCompleted && payout != null && !isOwnerRow && !isRowError)
                                    {
                                        QueuePayout(payout, reservations, resolutions);
                                        payoutCount++;
                                    }

                                    isOwnerRow = true;
                                    isRowError = false;
                                    payout = new OwnerPayout();
                                    reservations = new List<Reservation>();
                                    resolutions = new List<Resolution>();

                                    payout.PayoutDate = GetSafeDate(currentWorksheet.Cells[row, payoutDateCol].Value);
                                    payout.PayoutDate = ConversionHelper.EnsureUtcDate(payout.PayoutDate);
                                    payout.AccountNumber = GetAccountNumber(currentWorksheet.Cells[row, detailCol].Value);
                                    payout.Source = account;
                                    payout.PayoutAmount = GetSafeNumber(currentWorksheet.Cells[row, payoutCol].Value);
                                    payout.CreatedDate = today;
                                    payout.ModifiedDate = today;
                                    payout.InputSource = inputSource;
                                }
                                else if (payoutType == PayoutType.Resolution)
                                {
                                    // business rule: one resolution per reservastion
                                    resolutionCount++;
                                    isOwnerRow = false;
                                    Resolution resolution = new Resolution();
                                    resolution.ResolutionDate = GetSafeDate(currentWorksheet.Cells[row, payoutDateCol].Value);
                                    resolution.ResolutionDate = ConversionHelper.EnsureUtcDate(resolution.ResolutionDate);
                                    resolution.ResolutionType = GetSafeCellString(currentWorksheet.Cells[row, typeCol].Value);
                                    resolution.ResolutionDescription = GetSafeCellString(currentWorksheet.Cells[row, detailCol].Value);
                                    resolution.ResolutionAmount = GetSafeNumber(currentWorksheet.Cells[row, amountCol].Value);
                                    resolution.ConfirmationCode = string.Empty;
                                    resolution.Impact = string.Empty; // fill in later
                                    resolution.CreatedDate = today;
                                    resolution.ModifiedDate = today;
                                    resolution.InputSource = inputSource;

                                    if (!isCompleted) resolution.OwnerPayoutId = 0; // link to Owner Payout placeholder record for future payout

                                    if (resolutions != null) resolutions.Add(resolution);
                                }
                                else if (payoutType == PayoutType.Reservation)
                                {
                                    if (!isCompleted && reservations == null) reservations = new List<Reservation>();

                                    reservationCount++;
                                    isOwnerRow = false;
                                    Reservation r = new Reservation();
                                    r.ListingTitle = GetSafeCellString(currentWorksheet.Cells[row, listingCol].Value);
                                    string propertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(r.ListingTitle));
                                    r.TransactionDate = GetSafeDate(currentWorksheet.Cells[row, payoutDateCol].Value);
                                    r.TransactionDate = ConversionHelper.EnsureUtcDate(r.TransactionDate);
                                    r.PropertyCode = propertyCode;
                                    r.ConfirmationCode = GetSafeCellString(currentWorksheet.Cells[row, confirmationCodeCol].Value);
                                    r.CheckinDate = GetSafeDate(currentWorksheet.Cells[row, checkinDateCol].Value);
                                    r.CheckinDate = ConversionHelper.EnsureUtcDate(r.CheckinDate);
                                    r.Nights = (int)GetSafeNumber(currentWorksheet.Cells[row, nightCol].Value);
                                    r.CheckoutDate = r.CheckinDate.Value.AddDays(r.Nights);
                                    r.TotalRevenue = GetSafeNumber(currentWorksheet.Cells[row, amountCol].Value);
                                    //r.HostFee = GetSafeNumber(currentWorksheet.Cells[row, hostFeeCol].Value); // not needed per Jason
                                    //r.CleanFee = GetSafeNumber(currentWorksheet.Cells[row, cleanFeeCol].Value); // not needed per Jason
                                    r.GuestName = GetSafeCellString(currentWorksheet.Cells[row, guestCol].Value);
                                    r.Reference = GetSafeCellString(currentWorksheet.Cells[row, referenceCol].Value);
                                    CurrencyType currency;
                                    if (Enum.TryParse(GetSafeCellString(currentWorksheet.Cells[row, currencyCol].Value), true, out currency) == true)
                                        r.Currency = currency;
                                    else
                                        r.Currency = CurrencyType.USD;

                                    // non-input fields
                                    r.Source = account;
                                    r.Channel = channel;
                                    r.LocalTax = 0;
                                    r.DamageWaiver = 0;
                                    r.AdminFee = 0;
                                    r.PlatformFee = 0;
                                    r.TaxRate = 0;
                                    r.IsFutureBooking = isCompleted ? false : true;
                                    r.IncludeOnStatement = false;
                                    r.ApprovalStatus = RevenueApprovalStatus.NotStarted;

                                    // house keeping fields
                                    r.CreatedDate = today;
                                    r.ModifiedDate = today;
                                    r.InputSource = inputSource;

                                    if (!isCompleted) r.OwnerPayoutId = 0; // link to Owner Payout placeholder record for future payout

                                    // if property is not fund, we use a placehoder property so it can be fixed later
                                    if (string.IsNullOrEmpty(propertyCode))
                                    {
                                        errorRows.Add(new InputError
                                        {
                                            InputSource = inputSource,
                                            Row = row,
                                            Section = "Reservation",
                                            Message = string.Format("Property from listing title '{0}' and account '{1}' does not exist", Unquote(r.ListingTitle), account),
                                            OriginalText = "Excel row",
                                            CreatedTime = DateTime.UtcNow
                                        });
                                        r.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
                                        //isRowError = true;
                                        //errorCount++;
                                    }

                                    if (reservations != null && !string.IsNullOrEmpty(r.PropertyCode))
                                    {
                                        reservations.Add(r);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                errorRows.Add(new InputError
                                {
                                    InputSource = inputSource,
                                    Row = row,
                                    Section = "Exception",
                                    Message = "Data parse exception: " + ex.Message,
                                    OriginalText = "Excel row",
                                    CreatedTime = DateTime.UtcNow
                                });
                                isRowError = true;
                                errorCount++;
                            }
                        }

                        try
                        {
                            // save reservations and resoutions for future payout if there is no error
                            if (errorCount == 0 && !isCompleted)
                            {
                                if (resolutions != null && resolutions.Count() > 0)
                                    _context.Resolutions.AddRange(resolutions);

                                if (reservations != null && reservations.Count() > 0)
                                    _context.Reservations.AddRange(reservations);

                                _context.SaveChanges(); // Save Reservations
                            }
                            else if (errorCount == 0 && isCompleted)
                            {
                                _context.SaveChanges(); // save OwnerPayouts, Reservations, and Resolutions
                            }

                            if (errorRows.Count > 0)
                            {
                                _context.InputErrors.AddRange(errorRows);
                                _context.SaveChanges(); // Save errors
                            }
                        }
                        catch (Exception ex)
                        {
                            var inputError = new InputError
                            {
                                InputSource = inputSource,
                                Row = 0,
                                Section = "Exception",
                                Message = "Data saving error: " + ex.Message,
                                OriginalText = "Database saving",
                                CreatedTime = DateTime.UtcNow
                            };
                            _context.InputErrors.Add(inputError);
                            _context.SaveChanges(); // save errors
                            errorCount = 100000; // a large number
                        }
                    }
                }
            }

            try
            {
                excelFileInfo.Delete();
            }
            catch
            {
                // ignore if cannot delete
            }

            if (errorCount > 0)
                return -errorCount;
            else if (isCompleted)
                return payoutCount;
            else
                return reservationCount;
        }

        public int ImportFromCsv(string csvFile, DateTime reportDate, bool isCompleted, DateTime? payoutStartDate)
        {
            int errorCount = 0;
            int payoutCount = 0;
            int reservationCount = 0;
            int resolutionCount = 0;
            string channel = "Airbnb";
            string account = GetAccountFromFilename(csvFile);
            string inputSource = string.Format("{0} {1}", reportDate.ToString("yyyy-MM-dd"), GetInputSourceFromFilePath(csvFile));

            if (DuplicateInputSource(inputSource))
            {
                DeleteFileIfAllowed(csvFile);
                return 0;
            }

            if (!File.Exists(csvFile))
            {
                var inputError = new InputError
                {
                    InputSource = inputSource,
                    Row = 0,
                    Section = "Input File Check",
                    Message = string.Format("Input file '{0}' does not exist.", csvFile),
                    OriginalText = csvFile,
                    CreatedTime = DateTime.UtcNow
                };
                _context.InputErrors.Add(inputError);
                _context.SaveChanges(); // save errors

                return -1000000; // large number of errors
            }

            using (StreamReader sr = new StreamReader(csvFile))
            {
                int totalCols = 14;
                int startRow = 2; // starting row for reservation data
                int payoutDateCol = 0;
                int typeCol = 1;
                int confirmationCodeCol = 2;
                int checkinDateCol = 3;
                int nightCol = 4;
                int guestCol = 5;
                int listingCol = 6;
                int detailCol = 7;
                int referenceCol = 8;
                int currencyCol = 9;
                int amountCol = 10;
                int payoutCol = 11;

                bool isOwnerRow = false;
                bool isRowError = false;
                List<InputError> errorRows = new List<InputError>();

                char delimiter = (char)0;
                OwnerPayout payout = null;
                List<Reservation> reservations = null;
                List<Resolution> resolutions = null;
                DateTime today = DateTime.UtcNow;
                var propertyProvider = new PropertyProvider(_context);

                int row = 0;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    row++;
                    if (row < startRow) continue; // first row is header

                    if (delimiter == (char)0) delimiter = (line.Count(x => x == '\t') > 2 ? '\t' : ',');
                    string[] columns = ParseLine(line, delimiter, totalCols);

                    if (columns.Count() != totalCols)
                    {
                        errorRows.Add(new InputError
                        {
                            InputSource = inputSource,
                            Row = row,
                            Section = "Parse",
                            Message = string.Format("The total number of columns {0:d} does not match {1:d}", columns.Count(), totalCols),
                            OriginalText = line,
                            CreatedTime = DateTime.UtcNow
                        });
                    }

                    try
                    {
                        // exit loop if there is no transaction date on a row
                        if (isCompleted && columns[payoutDateCol] == null)
                        {
                            if (payout != null && isOwnerRow && !isRowError)
                            {
                                QueuePayout(payout, reservations, resolutions);
                                payoutCount++;
                                payout = null; // start a new input section cycle
                            }
                            break;
                        }

                        string type = columns[typeCol];
                        string confirmationCode = columns[confirmationCodeCol];
                        var payoutType = type.StartsWith("Payout") ? PayoutType.Payout :
                                            (type.StartsWith("Reservation") ? PayoutType.Reservation :
                                            (type.StartsWith("Resolution") ? PayoutType.Resolution : PayoutType.Other));

                        if (payoutType == PayoutType.Payout)
                        {
                            // payout row following a reservation or resolution row start an input cycle, we queue up the payout
                            if (isCompleted && payout != null && !isOwnerRow && !isRowError)
                            {
                                if (payoutStartDate == null || payout.PayoutDate.Value.Date > payoutStartDate.Value.Date)
                                {
                                    QueuePayout(payout, reservations, resolutions);
                                    payoutCount++;
                                }
                                else
                                {
                                    sr.Close();
                                    break; // payout date comes in in ascending order; so we can skip dates before payoutStartDate
                                }
                                payout = null; // start a new input section cycle
                            }
                            // owner row following another owner row is a payout split, we add the amount up
                            else if (isCompleted && payout != null && isOwnerRow && !isRowError)
                            {
                                payout.PayoutAmount += GetSafeNumber(columns[payoutCol]);
                                continue;
                            }

                            isOwnerRow = true;
                            isRowError = false;
                            payout = new OwnerPayout();
                            reservations = new List<Reservation>();
                            resolutions = new List<Resolution>();

                            payout.PayoutDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[payoutDateCol]));
                            payout.AccountNumber = GetAccountNumber(columns[detailCol]);
                            payout.Source = account;
                            payout.PayoutAmount = GetSafeNumber(columns[payoutCol]);
                            payout.CreatedDate = today;
                            payout.ModifiedDate = today;
                            payout.InputSource = inputSource;
                        }
                        else if (payoutType == PayoutType.Resolution || payoutType == PayoutType.Other)
                        {
                            if (!isCompleted && resolutions == null) resolutions = new List<Resolution>();

                            resolutionCount++;
                            isOwnerRow = false;

                            Resolution resolution = new Resolution();
                            resolution.ResolutionDate = (payout != null && payout.PayoutDate != null) ?
                                                         payout.PayoutDate :
                                                         ConversionHelper.EnsureUtcDate(GetSafeDate(columns[payoutDateCol]));
                            resolution.ResolutionType = columns[typeCol];
                            resolution.ResolutionDescription = columns[detailCol];
                            resolution.ResolutionAmount = GetSafeNumber(columns[amountCol]);
                            resolution.ConfirmationCode = columns[confirmationCodeCol];
                            resolution.Impact = string.Empty; // fill in later
                            resolution.CreatedDate = today;
                            resolution.ModifiedDate = today;
                            resolution.InputSource = inputSource;
                            resolution.ApprovalStatus = RevenueApprovalStatus.NotStarted;

                            // resolution has confirmation code associated with
                            if (!string.IsNullOrEmpty(resolution.ConfirmationCode))
                            {
                                string listingTitle = columns[listingCol];
                                string propertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(listingTitle));
                                resolution.PropertyCode = propertyCode;
                                if (string.IsNullOrEmpty(resolution.ResolutionDescription))
                                    resolution.ResolutionDescription = listingTitle;
                            }

                            // for future payout, link to 
                            if (!isCompleted) resolution.OwnerPayoutId = 0;

                            if (resolutions != null)
                                resolutions.Add(resolution);
                        }
                        else if (payoutType == PayoutType.Reservation)
                        {
                            if (!isCompleted && reservations == null) reservations = new List<Reservation>();

                            reservationCount++;
                            isOwnerRow = false;

                            Reservation r = new Reservation();
                            r.ListingTitle = columns[listingCol];
                            string propertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(r.ListingTitle));
                            r.TransactionDate = (payout != null && payout.PayoutDate != null) ?
                                                 payout.PayoutDate :
                                                 ConversionHelper.EnsureUtcDate(GetSafeDate(columns[payoutDateCol]));
                            r.PropertyCode = propertyCode;
                            r.ConfirmationCode = columns[confirmationCodeCol];
                            r.CheckinDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[checkinDateCol]));
                            r.Nights = (int)GetSafeNumber(columns[nightCol]);
                            r.CheckoutDate = r.CheckinDate.Value.AddDays(r.Nights);
                            r.TotalRevenue = GetSafeNumber(columns[amountCol]);
                            r.GuestName = columns[guestCol];
                            r.Reference = columns[referenceCol];
                            CurrencyType currency;
                            if (Enum.TryParse(columns[currencyCol], true, out currency) == true)
                                r.Currency = currency;
                            else
                                r.Currency = CurrencyType.USD;

                            // non-input fields
                            r.Source = account;
                            r.Channel = channel;
                            r.LocalTax = 0;
                            r.DamageWaiver = 0;
                            r.AdminFee = 0;
                            r.PlatformFee = 0;
                            r.TaxRate = 0;
                            r.IsFutureBooking = isCompleted ? false : true;
                            r.IncludeOnStatement = true;
                            r.ApprovalStatus = RevenueApprovalStatus.NotStarted;

                            // house keeping fields
                            r.CreatedDate = today;
                            r.ModifiedDate = today;
                            r.InputSource = inputSource;

                            if (!isCompleted) r.OwnerPayoutId = 0; // link to Owner Payout placeholder record for future payout

                            // if property is not fund, we use a placehoder property so it can be fixed later
                            if (string.IsNullOrEmpty(propertyCode))
                            {
                                errorRows.Add(new InputError
                                {
                                    InputSource = inputSource,
                                    Row = row,
                                    Section = "Reservation",
                                    Message = string.Format("Property from listing title '{0}' and account '{1}' does not exist", r.ListingTitle, account),
                                    OriginalText = line,
                                    CreatedTime = DateTime.UtcNow
                                });
                                r.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
                                //isRowError = true;
                                //errorCount++;
                            }

                            if (r.TotalRevenue < 0)
                            {
                                errorRows.Add(new InputError
                                {
                                    InputSource = inputSource,
                                    Row = row,
                                    Section = "Reservation",
                                    Message = string.Format("[PropertyCode] = '{0}' and [ConfirmationCode] = '{1}' [TotalRevenue] = {2}", 
                                                            r.PropertyCode, r.ConfirmationCode, r.TotalRevenue.ToString("C2")),
                                    OriginalText = "Excel row",
                                    CreatedTime = DateTime.UtcNow
                                });
                            }

                            if (reservations != null && !string.IsNullOrEmpty(r.PropertyCode))
                            {
                                reservations.Add(r);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorRows.Add(new InputError
                        {
                            InputSource = inputSource,
                            Row = row,
                            Section = "Exception",
                            Message = "Data parse exception: " + ex.Message,
                            OriginalText = line,
                            CreatedTime = DateTime.UtcNow
                        });
                        isRowError = true;
                        errorCount++;
                    }
                }

                try
                {
                    // last one
                    if (isCompleted & payout != null && !isRowError)
                    {
                        if (payoutStartDate == null || payout.PayoutDate.Value.Date > payoutStartDate.Value.Date)
                        {
                            QueuePayout(payout, reservations, resolutions);
                            payoutCount++;
                        }
                    }

                    // save reservations and resoutions for future payout if there is no error
                    if (errorCount == 0 && !isCompleted)
                    {
                        if (resolutions != null && resolutions.Count() > 0)
                            _context.Resolutions.AddRange(resolutions);

                        if (reservations != null && reservations.Count() > 0)
                            _context.Reservations.AddRange(reservations);

                        _context.SaveChanges(); // Save Reservations
                    }
                    else if (errorCount == 0 && isCompleted)
                    {
                        _context.SaveChanges(); // save OwnerPayouts, Reservations, and Resolutions
                    }

                    // save process errors for resolution
                    if (errorRows.Count > 0)
                    {
                        _context.InputErrors.AddRange(errorRows);
                        _context.SaveChanges(); // save errors
                    }
                }
                catch (Exception ex)
                {
                    var inputError = new InputError
                    {
                        InputSource = inputSource,
                        Row = 0,
                        Section = "Exception",
                        Message = "Data saving error: " + ex.Message,
                        OriginalText = "Database saving",
                        CreatedTime = DateTime.UtcNow
                    };
                    _context.InputErrors.Add(inputError);
                    _context.SaveChanges(); // save errors
                    errorCount = 100000; // a large number
                }

                sr.Close();
            }

            DeleteFileIfAllowed(csvFile);

            if (errorCount > 0)
                return -errorCount;
            else if (isCompleted)
                return payoutCount;
            else
                return reservationCount;
        }

        public DateTime? GetMostRecentPayoutDate(string accountFile)
        {
            var source = accountFile.Substring(0, accountFile.ToLower().LastIndexOf("-airbnb"));
            var lastDate = _context.OwnerPayouts.Where(x => x.Source == source)
                                                .OrderByDescending(x => x.PayoutDate)
                                                .Select(x => x.PayoutDate).FirstOrDefault();

            return lastDate != null ? lastDate.Value.Date : lastDate;
        }

        #endregion

        #region private methods

        private string GetAccountFromFilename(string filePath)
        {
            var filename = Path.GetFileNameWithoutExtension(filePath);
            string[] tokens = filename.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 1)
                return tokens[0];
            else
                return string.Empty;
        }

        private void QueuePayout(OwnerPayout payout, List<Reservation> reservations, List<Resolution> resolutions)
        {
            if (payout != null)
            {
                float totalAmount = 0;

                foreach (Reservation r in reservations)
                {
                    totalAmount += r.TotalRevenue;
                }

                foreach (Resolution r in resolutions)
                {
                    totalAmount += r.ResolutionAmount;
                }

                if (totalAmount != payout.PayoutAmount)
                {
                    payout.DiscrepancyAmount = payout.PayoutAmount - totalAmount;
                    payout.IsAmountMatched = false;
                }
                else
                {
                    payout.DiscrepancyAmount = 0;
                    payout.IsAmountMatched = true;
                }
            }

            payout.Reservations = new List<Reservation>();
            payout.Reservations.AddRange(reservations);
            payout.Resolutions = new List<Resolution>();
            payout.Resolutions.AddRange(resolutions);

            _context.OwnerPayouts.Add(payout);
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

        private string GetAccountNumber(object cellvalue)
        {
            var accountText = GetSafeCellString(cellvalue, string.Empty);
            string searchText = "Transfer to";
            int index = accountText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
            if(index >= 0)
            {
                return accountText.Substring(index + searchText.Length).Trim();
            }
            else
                return accountText;
        }

         private string[] ParseLine(string line, char delimiter, int totalCols)
        {
            string[] columns = line.Split(new char[] { delimiter }, StringSplitOptions.None);
            if (columns.Count() == totalCols) return columns;

            // attempt to combine strings that are separated by the delimiter within a pair of "
            List<string> normalizedCols = new List<string>();
            bool skipNext = false;
            string doubleQuote = "\"";
            for (int i = 0; i < columns.Count(); i++)
            {
                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }

                if (columns[i].StartsWith(doubleQuote) && columns[i + 1].EndsWith(doubleQuote))
                {
                    string joinColumns = string.Format("{0}{1}{2}", columns[i], delimiter, columns[i + 1]);
                    normalizedCols.Add(Unquote(joinColumns));
                    skipNext = true;
                }
                else
                    normalizedCols.Add(columns[i]);
            }

            return normalizedCols.ToArray();
        }

        private string  Unquote(string text)
        {
            // strip off double quotes
            if (text.StartsWith("\"") && text.EndsWith("\""))
                return text.Substring(1, text.Length - 2);
            else
                return text;
        }

        private char GetDelimiterInFile(string csvFile)
        {
            char delimiter = ','; // default
            using (StreamReader sr = new StreamReader(csvFile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    delimiter = (line.Count(x => x == '\t') > 2 ? '\t' : ',');
                    break;
                }
            }
            return delimiter;
        }

        private string GetInputSourceFromFilePath(string filePath)
        {
            string filename = Path.GetFileNameWithoutExtension(filePath);
            int index = filename.LastIndexOf('-');
            if (index >= 0)
                return filename.Substring(0, index);
            else
                return filename;
        }

        private bool DuplicateInputSource(string inputSource)
        {
            return _context.OwnerPayouts.Count(p => p.InputSource == inputSource) > 0;
        }

        private int ResetOwnerPayoutDb()
        {
            try
            {
                var resultCode = new SqlParameter("@ResultCode", SqlDbType.Int);
                resultCode.Direction = ParameterDirection.Output;
                _context.Database.ExecuteSqlCommand("InitOwnerPayout @ResultCode OUT", resultCode);
                return (int)resultCode.Value;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        private void DeleteFileIfAllowed(string filename)
        {

            try
            {
                File.Delete(filename);
            }
            catch
            {
                // ignore if cannot delete
            }
        }

        #endregion
    }
}
