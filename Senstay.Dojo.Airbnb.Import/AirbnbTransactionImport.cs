using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.FtpClient;
using System.Collections.Specialized;
using System.Data.Entity;

namespace Senstay.Dojo.Airbnb.Import
{
    public class AirbnbImportService
    {
        private DojoDbContext _context;

        private const int _totalCols = 14;
        private const int _totalGrossEarningCols = 16;
        private const int _startRow = 2; // starting row for reservation data
        private const int _payoutDateCol = 0;
        private const int _typeCol = 1;
        private const int _confirmationCodeCol = 2;
        private const int _checkinDateCol = 3;
        private const int _nightCol = 4;
        private const int _guestCol = 5;
        private const int _listingCol = 6;
        private const int _detailCol = 7;
        private const int _referenceCol = 8;
        private const int _currencyCol = 9;
        private const int _amountCol = 10;
        private const int _payoutCol = 11;
        private const int _hostFeeCol = 12;
        private const int _cleaningFeeCol = 13;
        private const int _grossEarningCol = 14;
        private const int _occupancyTaxCol = 15;
        private const string _channel = "Airbnb";

        public AirbnbImportService()
        {
            _context = new DojoDbContext();
        }

        public AirbnbImportService(DojoDbContext context)
        {
            _context = context;
        }

        #region Transaction imports
        public void ImportTransactions(FtpTransactionType transactionType)
        {
            string tempFolder = Path.GetTempPath(); // this only works for Windows program

            AirbnbFtpService ftpService = new AirbnbFtpService();

            if (transactionType == FtpTransactionType.Completed)
            {
                var payoutProvider = new OwnerPayoutService(_context);

                NameValueCollection ftpDirectories = ftpService.GetFolderList(AirbnbFtpConfig.CompletedFtpUrl);

                // only process the csv files in lastest transaction date directory
                DateTime importDate = DateTime.Today;
                string latestTransactionDir = GetLatestTransactionDirectory(ftpDirectories.AllKeys, ref importDate);

                // each airbnb transaction file set constains csv files corresponding to senstay payout accounts;
                // we get the list of these files first, and then process them one by one
                NameValueCollection ftpFileUrls = ftpService.GetFileList(ftpDirectories[latestTransactionDir]);
                foreach (string fileName in ftpFileUrls.AllKeys)
                {
                    // download file from ftp site
                    string csvFileUrl = ftpFileUrls[fileName];
                    string localFile = Path.Combine(tempFolder, fileName);
                    ftpService.Download(csvFileUrl, localFile);

                    // process only those data that is later than the most recent imported payout date in db
                    DateTime? startPayoutDate = payoutProvider.GetMostRecentPayoutDate(fileName);
                    ImportCompletedAirbnbTransactions(localFile, importDate.ToString("yyyy-MM-dd"), startPayoutDate);

                    DeleteFileIfAllowed(localFile);
                }
            }
            else if (transactionType == FtpTransactionType.Future)
            {
                var resevationProvider = new ReservationService(_context);

                NameValueCollection ftpDirectories = ftpService.GetFolderList(AirbnbFtpConfig.FutureFtpUrl);

                var reservationProvider = new ReservationService(_context);
                string mostRecentDateString = reservationProvider.GetMostRecentFutureDate();

                string sortableDate = string.Empty;
                foreach (var dirName in ftpDirectories.AllKeys)
                {
                    sortableDate = ParseDateFromTransactionDirectoryName(dirName);
                    if (string.Compare(sortableDate, mostRecentDateString) > 0)
                    {
                        // each airbnb download set constains csv files corresponding to senstay payout accounts
                        NameValueCollection ftpFileUrls = ftpService.GetFileList(ftpDirectories[dirName]);
                        foreach (string fileName in ftpFileUrls.AllKeys)
                        {
                            // download file from ftp site
                            string csvFileUrl = ftpFileUrls[fileName];
                            string localFile = Path.Combine(tempFolder, fileName);
                            ftpService.Download(csvFileUrl, localFile);
                            ImportFutureAirbnbTransactions(localFile, sortableDate);

                            DeleteFileIfAllowed(localFile);
                        }
                    }
                }
            }
            else if (transactionType == FtpTransactionType.Gross)
            {
                var resevationProvider = new ReservationService(_context);

                NameValueCollection ftpDirectories = ftpService.GetFolderList(AirbnbFtpConfig.GrossEarningsFtpUrl);

                var reservationProvider = new ReservationService(_context);
                string mostRecentDateString = reservationProvider.GetMostRecentGrossDate();

                string sortableDate = string.Empty;
                foreach (var dirName in ftpDirectories.AllKeys)
                {
                    sortableDate = ParseDateFromTransactionDirectoryName(dirName);
                    if (string.Compare(sortableDate, mostRecentDateString) > 0)
                    {
                        // each airbnb download set constains csv files corresponding to senstay payout accounts
                        NameValueCollection ftpFileUrls = ftpService.GetFileList(ftpDirectories[dirName]);
                        foreach (string fileName in ftpFileUrls.AllKeys)
                        {
                            // download file from ftp site
                            string csvFileUrl = ftpFileUrls[fileName];
                            string localFile = Path.Combine(tempFolder, fileName);
                            ftpService.Download(csvFileUrl, localFile);
                            ImportGrossEarningTransactions(localFile, sortableDate);

                            DeleteFileIfAllowed(localFile);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Import completed transactions from ftp site for those transactions that later than the most recent imported completed
        /// transactions in db. If error occurrred, errors are logged to InputErrors table and no transaction is saved to db.
        /// </summary>
        /// <param name="csvFile">transcation csv file url to be downloaded from ftp site</param>
        /// <param name="importDate">the date of the transaction files set</param>
        public int ImportCompletedAirbnbTransactions(string csvFile, string importDate, DateTime? payoutStartDate)
        {
            int errorCount = 0;
            string account = GetAccountFromFilename(csvFile);
            string inputSource = string.Format("{0} {1}", importDate, GetInputSourceFromFilePath(csvFile));

            if (CompeletedInputSourceAlreadyProcessed(inputSource)) return -1; // skip

            using (StreamReader sr = new StreamReader(csvFile))
            {
                bool isOwnerRow = false;
                bool isRowError = false;
                List<InputError> errorRows = new List<InputError>();

                char delimiter = (char)0;
                OwnerPayout payout = null;
                List<Reservation> reservations = null;
                List<Resolution> resolutions = null;
                DateTime today = DateTime.UtcNow;
                var propertyProvider = new PropertyService(_context);

                int row = 0;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    row++;
                    if (row < _startRow) continue; // first row is header

                    if (delimiter == (char)0) delimiter = (line.Count(x => x == '\t') > 2 ? '\t' : ',');
                    string[] columns = ParseLine(line, delimiter, _totalCols);

                    if (columns.Count() != _totalCols)
                    {
                        LogError(errorRows, row, inputSource, "Parse", line, 
                                 string.Format("Error: The total number of columns {0:d} does not match {1:d} in {2} at row = {3:d}", columns.Count(), _totalCols, inputSource, row));
                        errorCount++;
                        sr.Close();
                        break; // there is input file format error; we stop process the file
                    }

                    try
                    {
                        // exit loop if there is no transaction date on a row
                        if (columns[_payoutDateCol] == null)
                        {
                            if (payout != null && isOwnerRow && !isRowError)
                            {
                                QueuePayout(payout, reservations, resolutions);
                                payout = null; // start a new input section cycle
                            }
                            break;
                        }

                        string type = columns[_typeCol];
                        string confirmationCode = columns[_confirmationCodeCol];
                        var payoutType = type.StartsWith("Payout") ? PayoutType.Payout :
                                            (type.StartsWith("Reservation") ? PayoutType.Reservation :
                                            (type.StartsWith("Resolution") ? PayoutType.Resolution : PayoutType.Other));

                        if (payoutType == PayoutType.Payout)
                        {
                            // payout row following a reservation or resolution row start an input cycle, we queue up the payout
                            if (payout != null && !isOwnerRow && !isRowError)
                            {
                                if (payoutStartDate == null || payout.PayoutDate.Value.Date > payoutStartDate.Value.Date)
                                {
                                    QueuePayout(payout, reservations, resolutions);
                                }
                                else if (payout.PayoutDate.Value.Date == payoutStartDate.Value.Date && (reservations.Count > 0 || resolutions.Count > 0))
                                {
                                    QueuePayout(payout, reservations, resolutions);
                                }
                                else
                                {
                                    sr.Close();
                                    break; // payout date comes in in ascending order; so we can skip dates before payoutStartDate
                                }
                                payout = null; // start a new input section cycle
                            }
                            // owner row following another owner row is a payout split, we add the amount up
                            else if (payout != null && isOwnerRow && !isRowError)
                            {
                                payout.PayoutAmount += GetSafeNumber(columns[_payoutCol]);
                                continue;
                            }
                            // this is the case that resevations/resolutions exist without payout
                            else if (payout == null && ((reservations != null && reservations.Count > 0) || (resolutions != null && resolutions.Count > 0)))
                            {
                                if (reservations != null && reservations.Count > 0)
                                {
                                    foreach(var r in reservations)
                                    {
                                        LogError(errorRows, row, inputSource, "Reservation", line,
                                                 string.Format("Warning: Reservation date {0} for Property {1} Confirmation {2} does not have an associated payout at row {3:d}.", 
                                                 (r.TransactionDate == null ? "" : r.TransactionDate.Value.ToString("MM/dd/yyyy")), r.PropertyCode, r.ConfirmationCode, row));
                                    }
                                }
                                if (resolutions != null && resolutions.Count > 0)
                                {
                                    foreach (var r in resolutions)
                                    {
                                        LogError(errorRows, row, inputSource, "Reservation", line,
                                                 string.Format("Warning: Resolution date {0} of amount {1} does not have an associated payout at row {2:d}.",
                                                 (r.ResolutionDate == null ? "" : r.ResolutionDate.Value.ToString("MM/dd/yyyy")), r.ResolutionAmount.ToString("G"), account, row));
                                    }
                                }
                            }

                            isOwnerRow = true;
                            isRowError = false;
                            payout = new OwnerPayout();
                            reservations = new List<Reservation>();
                            resolutions = new List<Resolution>();

                            MapPayout(ref payout, columns, inputSource, account, today);
                        }
                        else if (payoutType == PayoutType.Resolution || payoutType == PayoutType.Other)
                        {
                            isOwnerRow = false;
                            Resolution resolution = CreateResolution(payout, columns, inputSource, today);

                            // resolution has confirmation code associated with
                            if (!string.IsNullOrEmpty(resolution.ConfirmationCode))
                            {
                                string listingTitle = columns[_listingCol];
                                string propertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(listingTitle));
                                resolution.PropertyCode = propertyCode;
                                if (string.IsNullOrEmpty(resolution.ResolutionDescription))
                                {
                                    resolution.ResolutionDescription = listingTitle;
                                }
                            }

                            if (payoutStartDate != null && payout.PayoutDate.Value.Date == payoutStartDate.Value.Date && ResolutionExist(resolution)) continue;

                            // for future payout, link to 
                            if (resolutions != null) resolutions.Add(resolution);
                        }
                        else if (payoutType == PayoutType.Reservation)
                        {
                            isOwnerRow = false;
                            Reservation r = CreateReservation(payout, columns, inputSource, account, today);
                            r.PropertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(r.ListingTitle));

                            // if property is not found, we use a placehoder property so it won't break the foreign key reference
                            if (string.IsNullOrEmpty(r.PropertyCode))
                            {
                                LogError(errorRows, row, inputSource, "Reservation", line,
                                         string.Format("Warning: Property from listing title '{0}' and account '{1}' does not exist", r.ListingTitle, account));

                                r.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
                            }

                            if (r.TotalRevenue < 0)
                            {
                                LogError(errorRows, row, inputSource, "Reservation", "Excel row",
                                         string.Format("Warning: Total Revenue is negative for [PropertyCode] = '{0}' and [ConfirmationCode] = '{1}' [TotalRevenue] = {2}",
                                                       r.PropertyCode, r.ConfirmationCode, r.TotalRevenue.ToString("C2")));
                            }

                            if (payoutStartDate != null && payout.PayoutDate.Value.Date == payoutStartDate.Value.Date && ReservationExist(r)) continue;

                            if (reservations != null && !string.IsNullOrEmpty(r.PropertyCode))
                            {
                                reservations.Add(r);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(errorRows, row, inputSource, "Exception", line, "Error: Data parse exception - " + ex.Message);
                        isRowError = true;
                        errorCount++;
                    }
                }

                try
                {
                    // last payout is not processed inside the loop; do it here
                    if (payout != null && !isRowError)
                    {
                        if (payoutStartDate == null || payout.PayoutDate.Value.Date > payoutStartDate.Value.Date)
                        {
                            QueuePayout(payout, reservations, resolutions);
                        }
                    }

                    // save OwnerPayouts, Reservations, and Resolutions only there is no error
                    if (errorCount == 0) SaveToDb();

                    // input error logging
                    if (errorRows.Count > 0)
                    {
                        _context.InputErrors.AddRange(errorRows);
                        SaveToDb(); // save input errors
                    }
                }
                catch (Exception ex)
                {
                    LogError(errorRows, row, inputSource, "Exception", "Database saving", "Error: " + ex.Message + ". " + ex.InnerException.Message);
                    var errorContext = new DojoDbContext();
                    errorContext.InputErrors.AddRange(errorRows);
                    errorContext.SaveChanges();
                    errorContext.Dispose();
                    errorCount = 100000; // a large number
                }

                sr.Close();
            }

            return errorCount > 0 ? 1 : 0;
        }

        /// <summary>
        /// Import future transactions from ftp site for those transactions that later than the most recent imported future
        /// transactions in db. If error occurrred, errors are logged to InputErrors table and no transaction is saved to db.
        /// </summary>
        /// <param name="csvFile">transcation csv file url to be downloaded from ftp site</param>
        /// <param name="importDate">the date of the transaction files set</param>
        public int ImportFutureAirbnbTransactions(string csvFile, string importDate)
        {
            int errorCount = 0;
            string account = GetAccountFromFilename(csvFile);
            string inputSource = string.Format("{0} {1}", importDate, GetInputSourceFromFilePath(csvFile));

            if (FutureInputSourceProcessed(inputSource)) return -1;

            using (StreamReader sr = new StreamReader(csvFile))
            {
                List<FutureReservation> reservations = new List<FutureReservation>();
                List<FutureResolution> resolutions = new List<FutureResolution>(); ;
                List<InputError> errorRows = new List<InputError>();

                char delimiter = (char)0;
                DateTime today = DateTime.UtcNow;
                var propertyProvider = new PropertyService(_context);
                int row = 0;

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    row++;
                    if (row < _startRow) continue; // first row is header

                    if (delimiter == (char)0) delimiter = (line.Count(x => x == '\t') > 2 ? '\t' : ',');
                    string[] columns = ParseLine(line, delimiter, _totalCols);

                    if (columns.Count() != _totalCols)
                    {
                        LogError(errorRows, row, inputSource, "Parse-Future", line,
                                 string.Format("Error: The total number of columns {0:d} does not match {1:d} in {2} at row = {3:d}", columns.Count(), _totalCols, inputSource, row));
                        errorCount++;
                        sr.Close();
                        break; // there is input file format error; we stop process the file
                    }

                    // skip those days that are earlier than today as they are not future day
                    //var payoutDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_payoutDateCol]));
                    //if (payoutDate == null || payoutDate.Value.Date < today.Date) continue;

                    try
                    {
                        string type = columns[_typeCol];
                        string confirmationCode = columns[_confirmationCodeCol];
                        var payoutType = type.StartsWith("Payout") ? PayoutType.Payout :
                                            (type.StartsWith("Reservation") ? PayoutType.Reservation :
                                            (type.StartsWith("Resolution") ? PayoutType.Resolution : PayoutType.Other));

                        if (payoutType == PayoutType.Resolution || payoutType == PayoutType.Other)
                        {
                            FutureResolution resolution = CreateFutureResolution(columns, inputSource, today);

                            // resolution has confirmation code associated with
                            if (!string.IsNullOrEmpty(resolution.ConfirmationCode))
                            {
                                string listingTitle = columns[_listingCol];
                                resolution.PropertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(listingTitle));
                                // if property is not fund, we use a placehoder property so it can be fixed later
                                if (string.IsNullOrEmpty(resolution.PropertyCode))
                                {
                                    LogError(errorRows, row, inputSource, "Resolution-Future", line,
                                             string.Format("Warning: Property for resolution from list title '{0}' and account '{1}' does not exist", listingTitle, account));

                                    resolution.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
                                }

                                if (string.IsNullOrEmpty(resolution.ResolutionDescription))
                                    resolution.ResolutionDescription = listingTitle;
                            }
                            else
                            {
                                resolution.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
                            }

                            if (resolutions != null) resolutions.Add(resolution);
                        }
                        else if (payoutType == PayoutType.Reservation)
                        {
                            FutureReservation r = CreateFutureReservation(columns, inputSource, account, today);

                            r.PropertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(r.ListingTitle));

                            // if property is not fund, we use a placehoder property so it can be fixed later
                            if (string.IsNullOrEmpty(r.PropertyCode))
                            {
                                LogError(errorRows, row, inputSource, "Reservation-Future", line,
                                         string.Format("Warning: Property from listing title '{0}' and account '{1}' does not exist for transaction date {2}", 
                                                        r.ListingTitle, account, (r.TransactionDate == null ? string.Empty : r.TransactionDate.Value.ToShortDateString())));

                                r.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
                            }

                            if (r.TotalRevenue < 0)
                            {
                                LogError(errorRows, row, inputSource, "Reservation-Future", "Excel row",
                                         string.Format("Warning: Total Revenue is negative for [PropertyCode] = '{0}' and [ConfirmationCode] = '{1}' [TotalRevenue] = {2}",
                                                       r.PropertyCode, r.ConfirmationCode, r.TotalRevenue.ToString("C2")));
                            }

                            if (reservations != null && !string.IsNullOrEmpty(r.PropertyCode))
                            {
                                reservations.Add(r);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(errorRows, row, inputSource, "Exception-Future", line, "Error: Data parse exception - " + ex.Message);
                        errorCount++;
                    }
                }

                try
                {
                    // save reservations and resoutions for future payout if there is no error
                    if (errorCount == 0)
                    {
                        if (resolutions != null && resolutions.Count() > 0)
                            _context.FutureResolutions.AddRange(resolutions);

                        if (reservations != null && reservations.Count() > 0)
                            _context.FutureReservations.AddRange(reservations);

                        SaveToDb(); // Save Reservations and resolutions
                    }

                    // save process errors for resolution
                    if (errorRows.Count > 0)
                    {
                        _context.InputErrors.AddRange(errorRows);
                        SaveToDb(); // save errors
                    }
                }
                catch (Exception ex)
                {
                    LogError(errorRows, row, inputSource, "Exception-Future", "Database saving", "Error: " + ex.Message + ". " + ex.InnerException.Message);
                    var errorContext = new DojoDbContext();
                    errorContext.InputErrors.AddRange(errorRows);
                    errorContext.SaveChanges();
                    errorContext.Dispose();
                    errorCount = 100000; // a large number
                }

                sr.Close();
            }

            return errorCount > 0 ? 1 : 0;
        }

        /// <summary>
        /// Import gross transactions from ftp site into db. If error occurrred, errors are logged to InputErrors table and no 
        /// transaction is saved to db.
        /// </summary>
        /// <param name="csvFile">transcation csv file url to be downloaded from ftp site</param>
        /// <param name="importDate">the date of the transaction files set</param>
        public int ImportGrossEarningTransactions(string csvFile, string importDate)
        {
            int errorCount = 0;
            string account = GetAccountFromFilename(csvFile);
            string inputSource = string.Format("{0} {1}", importDate, GetInputSourceFromFilePath(csvFile));

            if (GrossInputSourceProcessed(inputSource)) return -1;

            using (StreamReader sr = new StreamReader(csvFile))
            {
                List<GrossEarning> reservations = new List<GrossEarning>();
                List<InputError> errorRows = new List<InputError>();

                char delimiter = (char)0;
                DateTime today = DateTime.UtcNow;
                var propertyProvider = new PropertyService(_context);
                int row = 0;

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    row++;
                    if (row < _startRow) continue; // first row is header

                    if (delimiter == (char)0) delimiter = (line.Count(x => x == '\t') > 2 ? '\t' : ',');
                    string[] columns = ParseLine(line, delimiter, _totalGrossEarningCols);

                    if (columns.Count() != _totalGrossEarningCols)
                    {
                        LogError(errorRows, row, inputSource, "Parse-Gross", line,
                                 string.Format("Error: The total number of columns {0:d} does not match {1:d} in {2} at row = {3:d}", columns.Count(), _totalCols, inputSource, row));
                        errorCount++;
                        sr.Close();
                        break; // there is input file format error; we stop process the file
                    }

                    // skip those days that are earlier than today as they are not future day
                    var payoutDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_payoutDateCol]));
                    if (payoutDate == null) continue;

                    try
                    {
                        string type = columns[_typeCol];
                        string confirmationCode = columns[_confirmationCodeCol];
                        var payoutType = (type.StartsWith("Reservation") ? PayoutType.Reservation :
                                         (type.StartsWith("Resolution") ? PayoutType.Resolution : PayoutType.Other));

                        if (payoutType == PayoutType.Reservation)
                        {
                            GrossEarning r = CreateGrossEarningReservation(columns, inputSource, account, today);

                            r.PropertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(r.ListingTitle));

                            // if property is not found, we use a placehoder property so it can be fixed later
                            if (string.IsNullOrEmpty(r.PropertyCode))
                            {
                                LogError(errorRows, row, inputSource, "Reservation-Gross", line,
                                         string.Format("Warning: Property from listing title '{0}' and account '{1}' does not exist", r.ListingTitle, account));

                                r.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
                            }

                            if (r.GrossTotal < 0)
                            {
                                LogError(errorRows, row, inputSource, "Reservation-Gross", "Excel row",
                                         string.Format("Warning: Total Revenue is negative for [PropertyCode] = '{0}' and [ConfirmationCode] = '{1}' [TotalRevenue] = {2}",
                                                       r.PropertyCode, r.ConfirmationCode, r.GrossTotal.Value.ToString("C2")));
                            }

                            if (reservations != null && !string.IsNullOrEmpty(r.PropertyCode))
                            {
                                reservations.Add(r);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(errorRows, row, inputSource, "Exception-Gross", line, "Error: Data parse exception - " + ex.Message);
                        errorCount++;
                    }
                }

                try
                {
                    // save reservations if there is no error
                    if (errorCount == 0)
                    {

                        if (reservations != null && reservations.Count() > 0)
                            _context.GrossEarnings.AddRange(reservations);

                        SaveToDb(); // Save Reservations
                    }

                    // save process errors for reservation
                    if (errorRows.Count > 0)
                    {
                        _context.InputErrors.AddRange(errorRows);
                        SaveToDb(); // save errors
                    }
                }
                catch (Exception ex)
                {
                    LogError(errorRows, row, inputSource, "Exception-Gross", "Database saving", "Error: " + ex.Message + ". " + ex.InnerException.Message);
                    var errorContext = new DojoDbContext();
                    errorContext.InputErrors.AddRange(errorRows);
                    errorContext.SaveChanges();
                    errorContext.Dispose();
                    errorCount = 100000; // a large number
                }

                sr.Close();
            }

            return errorCount > 0 ? 1 : 0;
        }

        public int LogMissingCompletedAirbnbTransactions(string csvFile, string importDate, DateTime? payoutStartDate)
        {
            int errorCount = 0;
            string account = GetAccountFromFilename(csvFile);
            string inputSource = string.Format("{0} {1}", importDate, GetInputSourceFromFilePath(csvFile));

            using (StreamReader sr = new StreamReader(csvFile))
            {
                bool isOwnerRow = false;
                bool isRowError = false;
                List<InputError> errorRows = new List<InputError>();

                char delimiter = (char)0;
                OwnerPayout payout = null;
                List<Reservation> reservations = null;
                List<Resolution> resolutions = null;
                DateTime today = DateTime.UtcNow;
                var propertyProvider = new PropertyService(_context);

                int row = 0;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    row++;
                    if (row < _startRow) continue; // first row is header

                    if (delimiter == (char)0) delimiter = (line.Count(x => x == '\t') > 2 ? '\t' : ',');
                    string[] columns = ParseLine(line, delimiter, _totalCols);

                    if (columns.Count() != _totalCols)
                    {
                        LogError(errorRows, row, inputSource, "Parse", line,
                                 string.Format("Error: The total number of columns {0:d} does not match {1:d} in {2} at row = {3:d}", columns.Count(), _totalCols, inputSource, row));
                        errorCount++;
                        sr.Close();
                        break; // there is input file format error; we stop process the file
                    }

                    try
                    {
                        // exit loop if there is no transaction date on a row
                        if (columns[_payoutDateCol] == null)
                        {
                            if (payout != null && isOwnerRow && !isRowError)
                            {
                                QueuePayout(payout, reservations, resolutions);
                                payout = null; // start a new input section cycle
                            }
                            break;
                        }

                        string type = columns[_typeCol];
                        string confirmationCode = columns[_confirmationCodeCol];
                        var payoutType = type.StartsWith("Payout") ? PayoutType.Payout :
                                            (type.StartsWith("Reservation") ? PayoutType.Reservation :
                                            (type.StartsWith("Resolution") ? PayoutType.Resolution : PayoutType.Other));

                        if (payoutType == PayoutType.Payout)
                        {
                            // payout row following a reservation or resolution row start an input cycle, we queue up the payout
                            if (payout != null && !isOwnerRow && !isRowError)
                            {
                                // payout date comes in in ascending order; so we can skip dates before payoutStartDate
                                if (payoutStartDate != null && payout.PayoutDate != null && payout.PayoutDate.Value.Date <= payoutStartDate.Value.Date)
                                {
                                    sr.Close();
                                    break; 
                                }
                                payout = null; // start a new input section cycle
                            }
                            // owner row following another owner row is a payout split, we add the amount up
                            else if (payout != null && isOwnerRow && !isRowError)
                            {
                                continue;
                            }
                            // this is the case that resevations/resolutions exist without payout
                            else if (payout == null && ((reservations != null && reservations.Count > 0) || (resolutions != null && resolutions.Count > 0)))
                            {
                                if (reservations != null && reservations.Count > 0)
                                {
                                    foreach (var r in reservations)
                                    {
                                        LogError(errorRows, row, inputSource, "Reservation", line,
                                                 string.Format("Warning: Reservation date {0} for Property {1} Confirmation {2} does not have an associated payout at row {3:d}.",
                                                 (r.TransactionDate == null ? "" : r.TransactionDate.Value.ToString("MM/dd/yyyy")), r.PropertyCode, r.ConfirmationCode, row));
                                        errorCount++;
                                    }
                                }
                                if (resolutions != null && resolutions.Count > 0)
                                {
                                    foreach (var r in resolutions)
                                    {
                                        LogError(errorRows, row, inputSource, "Reservation", line,
                                                 string.Format("Warning: Resolution date {0} of amount {1} does not have an associated payout at row {2:d}.",
                                                 (r.ResolutionDate == null ? "" : r.ResolutionDate.Value.ToString("MM/dd/yyyy")), r.ResolutionAmount.ToString("G"), account, row));
                                        errorCount++;
                                    }
                                }
                            }

                            isOwnerRow = true;
                            isRowError = false;
                            payout = new OwnerPayout();
                            reservations = new List<Reservation>();
                            resolutions = new List<Resolution>();

                            MapPayout(ref payout, columns, inputSource, account, today);
                        }
                        else if (payoutType == PayoutType.Resolution || payoutType == PayoutType.Other)
                        {
                            isOwnerRow = false;
                            Resolution resolution = CreateResolution(payout, columns, inputSource, today);

                            // resolution has confirmation code associated with
                            if (!string.IsNullOrEmpty(resolution.ConfirmationCode))
                            {
                                string listingTitle = columns[_listingCol];
                                string propertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(listingTitle));
                                resolution.PropertyCode = propertyCode;
                                if (string.IsNullOrEmpty(resolution.ResolutionDescription))
                                {
                                    resolution.ResolutionDescription = listingTitle;
                                }
                            }

                            if (payoutStartDate != null && payout.PayoutDate.Value.Date == payoutStartDate.Value.Date && ResolutionExist(resolution)) continue;

                            // for future payout, link to 
                            if (resolutions != null) resolutions.Add(resolution);
                        }
                        else if (payoutType == PayoutType.Reservation)
                        {
                            isOwnerRow = false;
                            Reservation r = CreateReservation(payout, columns, inputSource, account, today);
                            r.PropertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(r.ListingTitle));

                            // if property is not found, we use a placehoder property so it won't break the foreign key reference
                            if (string.IsNullOrEmpty(r.PropertyCode))
                            {
                                LogError(errorRows, row, inputSource, "Reservation", line,
                                         string.Format("Warning: Property from listing title '{0}' and account '{1}' does not exist", r.ListingTitle, account));

                                r.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
                            }

                            if (r.TotalRevenue < 0)
                            {
                                LogError(errorRows, row, inputSource, "Reservation", "Excel row",
                                         string.Format("Warning: Total Revenue is negative for [PropertyCode] = '{0}' and [ConfirmationCode] = '{1}' [TotalRevenue] = {2}",
                                                       r.PropertyCode, r.ConfirmationCode, r.TotalRevenue.ToString("C2")));
                            }

                            if (payoutStartDate != null && payout.PayoutDate.Value.Date == payoutStartDate.Value.Date && ReservationExist(r)) continue;

                            if (reservations != null && !string.IsNullOrEmpty(r.PropertyCode))
                            {
                                reservations.Add(r);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(errorRows, row, inputSource, "Exception", line, "Error: Data parse exception - " + ex.Message);
                        isRowError = true;
                        errorCount++;
                    }
                }

                try
                {
                    // input error logging
                    if (errorRows.Count > 0)
                    {
                        _context.InputErrors.AddRange(errorRows);
                        SaveToDb(); // save input errors
                    }
                }
                catch (Exception ex)
                {
                    LogError(errorRows, row, inputSource, "Exception", "Database saving", "Error: " + ex.Message + ". " + ex.InnerException.Message);
                    var errorContext = new DojoDbContext();
                    errorContext.InputErrors.AddRange(errorRows);
                    errorContext.SaveChanges();
                    errorContext.Dispose();
                    errorCount++;
                }

                sr.Close();
            }

            return errorCount;
        }

        #endregion

        #region Transaction validation

        public void ValidateTransactions(FtpTransactionType transactionType, DateTime startDate, DateTime endDate)
        {
            string tempFolder = Path.GetTempPath(); // this only works for Windows program

            AirbnbFtpService ftpService = new AirbnbFtpService();

            if (transactionType == FtpTransactionType.Completed)
            {
                var payoutProvider = new OwnerPayoutService(_context);

                NameValueCollection ftpDirectories = ftpService.GetFolderList(AirbnbFtpConfig.CompletedFtpUrl);

                DateTime importDate = startDate;
                while (importDate <= endDate)
                {
                    string transactioDirectory = importDate.ToString("MMMM d yyyy");

                    // each airbnb transaction file set constains csv files corresponding to senstay payout accounts;
                    // we get the list of these files first, and then process them one by one
                    NameValueCollection ftpFileUrls = ftpService.GetFileList(ftpDirectories[transactioDirectory]);
                    foreach (string fileName in ftpFileUrls.AllKeys)
                    {
                        // download file from ftp site
                        string csvFileUrl = ftpFileUrls[fileName];
                        string localFile = Path.Combine(tempFolder, fileName);
                        ftpService.Download(csvFileUrl, localFile);
                        ValidateCompletedAirbnbTransactions(localFile, importDate);
                        DeleteFileIfAllowed(localFile);
                    }

                    importDate = importDate.AddDays(1);
                }
            }
        }

        public int ValidateCompletedAirbnbTransactions(string csvFile, DateTime importDate)
        {
            int errorCount = 0;
            string account = GetAccountFromFilename(csvFile);
            string inputSource = string.Format("{0} {1}", importDate.ToString("yyyy-MM-dd"), GetInputSourceFromFilePath(csvFile));

            using (StreamReader sr = new StreamReader(csvFile))
            {
                List<InputError> errorRows = new List<InputError>();
                char delimiter = (char)0;
                var propertyProvider = new PropertyService(_context);

                int row = 0;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    row++;
                    if (row < _startRow) continue; // first row is header

                    if (delimiter == (char)0) delimiter = (line.Count(x => x == '\t') > 2 ? '\t' : ',');
                    string[] columns = ParseLine(line, delimiter, _totalCols);

                    if (columns.Count() != _totalCols)
                    {
                        LogError(errorRows, row, inputSource, "Parse", line,
                                 string.Format("Error: Airbnb Transaction Validation - The total number of columns {0:d} does not match {1:d}", columns.Count(), _totalCols));
                        errorCount++;
                        sr.Close();
                        break; // there is input file format error; we stop process the file
                    }

                    try
                    {
                        // read until the first payout row is reached
                        if (columns[_payoutDateCol] == null) continue;

                        string type = columns[_typeCol];
                        var payoutType = type.StartsWith("Payout") ? PayoutType.Payout :
                                            (type.StartsWith("Reservation") ? PayoutType.Reservation :
                                            (type.StartsWith("Resolution") ? PayoutType.Resolution : PayoutType.Other));

                        if (payoutType == PayoutType.Payout)
                        {
                            DateTime? payoutDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_payoutDateCol]));
                            if (payoutDate != null && payoutDate.Value.Date >= importDate.Date)
                            {
                                LogError(errorRows, row, inputSource, "Airbnb Transaction Validation", line, "Error: Transaction date " + columns[_payoutDateCol] + " is on or after the file date.");
                                errorCount++;
                            }
                            break; // payout date comes in in ascending order; so we can stop further process
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(errorRows, row, inputSource, "Airbnb Transaction Validation", line, "Error: Data parse exception - " + ex.Message);
                        errorCount++;
                    }
                }

                sr.Close();

                if (errorRows.Count > 0)
                {
                    try
                    {
                        _context.InputErrors.AddRange(errorRows);
                        SaveToDb();
                    }
                    catch (Exception ex)
                    {
                        LogError(errorRows, row, inputSource, "Exception-Validation", "Database saving", "Error: " + ex.Message + ". " + ex.InnerException.Message);
                        _context.InputErrors.AddRange(errorRows);
                        SaveToDb();
                    }
                }
            }

            return errorCount > 0 ? 1 : 0;
        }
        #endregion

        #region Completed Transaction Backfill for missing transactions
        public int BackfillCompletedAirbnbTransactions(Stream fileData, string csvFile, DateTime importDate)
        {
            int errorCount = 0;
            var filename = Path.GetFileNameWithoutExtension(csvFile);
            string account = filename.Substring(11, filename.LastIndexOf('-') - 11);
            string inputSource = string.Format("{0} {1}", importDate.ToString("yyyy-MM-dd"), account);

            using (StreamReader sr = new StreamReader(fileData))
            {
                bool isOwnerRow = false;
                bool isRowError = false;
                List<InputError> errorRows = new List<InputError>();

                char delimiter = (char)0;
                OwnerPayout payout = null;
                List<Reservation> reservations = null;
                List<Resolution> resolutions = null;
                DateTime today = DateTime.UtcNow;
                var propertyProvider = new PropertyService(_context);

                int row = 0;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    row++;
                    if (row < _startRow) continue; // first row is header

                    if (delimiter == (char)0) delimiter = (line.Count(x => x == '\t') > 2 ? '\t' : ',');
                    string[] columns = ParseLine(line, delimiter, _totalCols);

                    if (columns.Count() != _totalCols)
                    {
                        LogError(errorRows, row, inputSource, "Parse", line,
                                 string.Format("Error: The total number of columns {0:d} does not match {1:d}", columns.Count(), _totalCols));
                        errorCount++;
                        sr.Close();
                        break; // there is input file format error; we stop process the file
                    }

                    try
                    {
                        // exit loop if there is no transaction date on a row
                        if (columns[_payoutDateCol] == null)
                        {
                            if (payout != null && isOwnerRow && !isRowError)
                            {
                                QueuePayout(payout, reservations, resolutions);
                                payout = null; // start a new input section cycle
                            }
                            break;
                        }

                        string type = columns[_typeCol];
                        string confirmationCode = columns[_confirmationCodeCol];
                        var payoutType = type.StartsWith("Payout") ? PayoutType.Payout :
                                            (type.StartsWith("Reservation") ? PayoutType.Reservation :
                                            (type.StartsWith("Resolution") ? PayoutType.Resolution : PayoutType.Other));

                        if (payoutType == PayoutType.Payout)
                        {
                            // payout row following a reservation or resolution row start an input cycle, we queue up the payout
                            if (payout != null && !isOwnerRow && !isRowError)
                            {
                                QueuePayout(payout, reservations, resolutions);
                                payout = null; // start a new input section cycle
                            }
                            // owner row following another owner row is a payout split, we add the amount up
                            else if (payout != null && isOwnerRow && !isRowError)
                            {
                                payout.PayoutAmount += GetSafeNumber(columns[_payoutCol]);
                                continue;
                            }

                            isOwnerRow = true;
                            isRowError = false;
                            payout = new OwnerPayout();
                            reservations = new List<Reservation>();
                            resolutions = new List<Resolution>();

                            MapPayout(ref payout, columns, inputSource, account, today);
                        }
                        else if (payoutType == PayoutType.Resolution || payoutType == PayoutType.Other)
                        {
                            isOwnerRow = false;
                            Resolution resolution = CreateResolution(payout, columns, inputSource, today);

                            // resolution has confirmation code associated with
                            if (!string.IsNullOrEmpty(resolution.ConfirmationCode))
                            {
                                string listingTitle = columns[_listingCol];
                                string propertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(listingTitle));
                                resolution.PropertyCode = propertyCode;
                                if (string.IsNullOrEmpty(resolution.ResolutionDescription))
                                    resolution.ResolutionDescription = listingTitle;
                            }

                            // for future payout, link to 
                            if (resolutions != null) resolutions.Add(resolution);
                        }
                        else if (payoutType == PayoutType.Reservation)
                        {
                            isOwnerRow = false;

                            Reservation r = CreateReservation(payout, columns, inputSource, account, today);
                            r.PropertyCode = propertyProvider.GetPropertyCodeByListing(account, Unquote(r.ListingTitle));

                            // if property is not fund, we use a placehoder property so it can be fixed later
                            if (string.IsNullOrEmpty(r.PropertyCode))
                            {
                                LogError(errorRows, row, inputSource, "Reservation", line,
                                         string.Format("Warning: Property from listing title '{0}' and account '{1}' does not exist", r.ListingTitle, account));

                                r.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
                            }

                            if (r.TotalRevenue < 0)
                            {
                                LogError(errorRows, row, inputSource, "Reservation", "Excel row",
                                         string.Format("Warning: Total Revenue is negative for [PropertyCode] = '{0}' and [ConfirmationCode] = '{1}' [TotalRevenue] = {2}",
                                                       r.PropertyCode, r.ConfirmationCode, r.TotalRevenue.ToString("C2")));
                            }

                            if (reservations != null && !string.IsNullOrEmpty(r.PropertyCode))
                            {
                                reservations.Add(r);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(errorRows, row, inputSource, "Exception", line, "Error: Data parse exception - " + ex.Message);
                        isRowError = true;
                        errorCount++;
                    }
                }

                try
                {
                    // last payout is not processed inside the loop; do it here
                    if (payout != null && !isRowError)
                    {
                        QueuePayout(payout, reservations, resolutions);
                    }

                    // save OwnerPayouts, Reservations, and Resolutions only there is no error
                    if (errorCount == 0) SaveToDb();

                    // input error logging
                    if (errorRows.Count > 0)
                    {
                        _context.InputErrors.AddRange(errorRows);
                        SaveToDb(); // save input errors
                    }
                }
                catch (Exception ex)
                {
                    LogError(errorRows, row, inputSource, "Exception", "Database saving", "Error: " + ex.Message + ". " + ex.InnerException.Message);
                    var errorContext = new DojoDbContext();
                    errorContext.InputErrors.AddRange(errorRows);
                    errorContext.SaveChanges();
                    errorContext.Dispose();
                    errorCount = 100000; // a large number
                }

                sr.Close();
            }

            return errorCount > 0 ? 1 : 0;
        }
        #endregion

        #region Airbnb transsaction parsing methods

        private string GetAccountFromFilename(string filePath)
        {
            var filename = Path.GetFileNameWithoutExtension(filePath);
            string[] tokens = filename.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 1)
                return tokens[tokens.Length-2].Trim();
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
                    payout.DiscrepancyAmount = (float?)Math.Round((double)(payout.PayoutAmount - totalAmount), 2);
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
            if (index >= 0)
            {
                return accountText.Substring(index + searchText.Length).Trim();
            }
            else
                return accountText;
        }

        private string[] ParseLine(string line, char delimiter, int totalCols)
        {
            string[] columns = line.Split(new char[] { delimiter }, StringSplitOptions.None);
            if(columns.Count() == totalCols - 1)
            {
                var columnList = columns.ToList();
                columnList.Add("");
                columns = columnList.ToArray();
                return columns;
            }
            else if(columns.Count() == totalCols)
            {
                return columns;
            }


            // attempt to combine strings that are separated by the delimiter within a pair of double quotes
            List<string> normalizedCols = new List<string>();
            string doubleQuote = "\"";
            int i = 0;
            while (i < columns.Count())
            {
                if (columns[i].StartsWith(doubleQuote))
                {
                    string joinColumns = columns[i];
                    int j = i + 1;
                    while (j < columns.Count() && !columns[j].EndsWith(doubleQuote))
                    {
                        joinColumns = string.Format("{0}{1}{2}", joinColumns, delimiter, columns[j]);
                        j++;
                    }
                    joinColumns = string.Format("{0}{1}{2}", joinColumns, delimiter, columns[j]);
                    normalizedCols.Add(Unquote(joinColumns));
                    i = j + 1;
                }
                else
                {
                    normalizedCols.Add(columns[i]);
                    i++;
                }
            }

            if (normalizedCols.Count() == totalCols - 1)
            {
                normalizedCols.Add("");
            }
            return normalizedCols.ToArray();
        }

        private string Unquote(string text)
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

        private bool CompeletedInputSourceAlreadyProcessed(string inputSource)
        {
            // completed transactions input source is later or equal than the given, then it has been processed
            return _context.OwnerPayouts.Count(p => p.InputSource.Substring(0, inputSource.Length) == inputSource) > 0;
        }

        public void DeleteFileIfAllowed(string filename)
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

        private void LogError(List<InputError> errorRows, int row, string inputSource, string section, string line, string message)
        {
            errorRows.Add(new InputError
            {
                InputSource = inputSource,
                Row = row,
                Section = section,
                Message = message,
                OriginalText = line,
                CreatedTime = DateTime.UtcNow
            });
        }

        private void MapPayout(ref OwnerPayout payout, string[] columns, string inputSource, string account, DateTime today)
        {
            payout.PayoutDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_payoutDateCol]));
            payout.AccountNumber = GetAccountNumber(columns[_detailCol]);
            payout.Source = account;
            payout.PayoutAmount = GetSafeNumber(columns[_payoutCol]);
            payout.CreatedDate = today;
            payout.ModifiedDate = today;
            payout.InputSource = inputSource;
        }

        private Resolution CreateResolution(OwnerPayout payout, string[] columns,string inputSource, DateTime today)
        {
            Resolution resolution = new Resolution();
            resolution.ResolutionDate = (payout != null && payout.PayoutDate != null) ?
                                         payout.PayoutDate :
                                         ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_payoutDateCol]));
            resolution.ResolutionType = columns[_typeCol];
            resolution.ResolutionDescription = columns[_detailCol];
            resolution.ResolutionAmount = GetSafeNumber(columns[_amountCol]);
            resolution.ConfirmationCode = columns[_confirmationCodeCol];
            resolution.Impact = string.Empty; // fill in later
            resolution.CreatedDate = today;
            resolution.ModifiedDate = today;
            resolution.InputSource = inputSource;
            resolution.ApprovalStatus = RevenueApprovalStatus.NotStarted;

            return resolution;
        }

        private Reservation CreateReservation(OwnerPayout payout, string[] columns, string inputSource, string account, DateTime today)
        {
            Reservation r = new Reservation();

            r.ListingTitle = columns[_listingCol];
            r.TransactionDate = (payout != null && payout.PayoutDate != null) ?
                                 payout.PayoutDate :
                                 ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_payoutDateCol]));
            r.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
            r.ConfirmationCode = columns[_confirmationCodeCol];
            r.CheckinDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_checkinDateCol]));
            r.Nights = (int)GetSafeNumber(columns[_nightCol]);
            r.CheckoutDate = r.CheckinDate.Value.AddDays(r.Nights);
            r.TotalRevenue = GetSafeNumber(columns[_amountCol]);
            r.GuestName = columns[_guestCol];
            r.Reference = columns[_referenceCol];
            CurrencyType currency;
            if (Enum.TryParse(columns[_currencyCol], true, out currency) == true)
                r.Currency = currency;
            else
                r.Currency = CurrencyType.USD;

            // non-input fields
            r.Source = account;
            r.Channel = _channel;
            r.LocalTax = 0;
            r.DamageWaiver = 0;
            r.AdminFee = 0;
            r.PlatformFee = 0;
            r.TaxRate = 0;
            r.IsFutureBooking = false;
            r.IncludeOnStatement = true;
            r.ApprovalStatus = RevenueApprovalStatus.NotStarted;

            // house keeping fields
            r.CreatedDate = today;
            r.ModifiedDate = today;
            r.InputSource = inputSource;

            return r;
        }

        private FutureResolution CreateFutureResolution(string[] columns, string inputSource, DateTime today)
        {
            var resolution = new FutureResolution();
            resolution.ResolutionDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_payoutDateCol]));
            resolution.ResolutionType = columns[_typeCol];
            resolution.ResolutionDescription = columns[_detailCol];
            resolution.ResolutionAmount = GetSafeNumber(columns[_amountCol]);
            resolution.ConfirmationCode = columns[_confirmationCodeCol];
            resolution.Impact = string.Empty; // fill in later
            resolution.CreatedDate = today;
            resolution.ModifiedDate = today;
            resolution.InputSource = inputSource;
            resolution.ApprovalStatus = RevenueApprovalStatus.NotStarted;

            return resolution;
        }

        private FutureReservation CreateFutureReservation(string[] columns, string inputSource, string account, DateTime today)
        {
            var r = new FutureReservation();

            r.ListingTitle = columns[_listingCol];
            r.TransactionDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_payoutDateCol]));
            r.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
            r.ConfirmationCode = columns[_confirmationCodeCol];
            r.CheckinDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_checkinDateCol]));
            r.Nights = (int)GetSafeNumber(columns[_nightCol]);
            r.CheckoutDate = r.CheckinDate.Value.AddDays(r.Nights);
            r.TotalRevenue = GetSafeNumber(columns[_amountCol]);
            r.GuestName = columns[_guestCol];

            // non-input fields
            r.Source = account;
            r.Channel = _channel;

            // house keeping fields
            r.CreatedDate = today;
            r.ModifiedDate = today;
            r.InputSource = inputSource;

            return r;
        }

        private GrossEarning CreateGrossEarningReservation(string[] columns, string inputSource, string account, DateTime today)
        {
            var r = new GrossEarning();

            r.GrossDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_payoutDateCol]));
            r.GrossType = "Reservation";
            r.ConfirmationCode = columns[_confirmationCodeCol];
            r.CheckinDate = ConversionHelper.EnsureUtcDate(GetSafeDate(columns[_checkinDateCol]));
            r.Nights = (int)GetSafeNumber(columns[_nightCol]);
            r.GuestName = columns[_guestCol];
            r.ListingTitle = columns[_listingCol];
            r.Details = columns[_detailCol];
            r.Reference = columns[_referenceCol];
            r.Currency = (CurrencyType)Enum.Parse(typeof(CurrencyType), columns[_currencyCol], true);
            r.Amount = GetSafeNumber(columns[_amountCol]);
            r.HostFee = GetSafeNumber(columns[_hostFeeCol]);
            r.CleaningFee = GetSafeNumber(columns[_cleaningFeeCol]);
            r.GrossTotal = GetSafeNumber(columns[_grossEarningCol]);
            r.OccupancyTax = GetSafeNumber(columns[_occupancyTaxCol]);

            r.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;
            r.CheckoutDate = r.CheckinDate.Value.AddDays(r.Nights);

            // non-input fields
            r.Channel = _channel;

            // house keeping fields
            r.CreatedDate = today;
            r.ModifiedDate = today;
            r.InputSource = inputSource;

            return r;
        }


        private void SaveToDb()
        {
            _context.SaveChanges();
        }

        #endregion

        #region Completed transaction specific helpers

        private string GetLatestTransactionDirectory(string[] fitDirectories, ref DateTime latestDate)
        {
            latestDate = new DateTime(2000, 1, 1); // arbitrary early date
            string latestPath = string.Empty;
            foreach (string dirName in fitDirectories)
            {
                try
                {
                    // directory name comes in as date format of 'MMMM d yyyy'; convert it to yyyy-MM-dd so that we can compare them
                    var dirDate = DateTime.ParseExact(dirName, "MMMM d yyyy", new CultureInfo("en-US"));
                    if (dirDate > latestDate)
                    {
                        latestDate = dirDate;
                        latestPath = dirName;
                    }
                }
                catch
                {
                    // ignore and continue
                }
            }

            return latestPath;
        }

        private bool ReservationExist(Reservation reservation)
        {
            return _context.Reservations.Where(x => DbFunctions.TruncateTime(x.TransactionDate) == DbFunctions.TruncateTime(reservation.TransactionDate) && 
                                                    x.ConfirmationCode == reservation.ConfirmationCode && 
                                                    x.PropertyCode == reservation.PropertyCode).Count() > 0;
        }

        private bool ResolutionExist(Resolution resolution)
        {
            return _context.Resolutions.Where(x => DbFunctions.TruncateTime(x.ResolutionDate) == DbFunctions.TruncateTime(resolution.ResolutionDate) && 
                                                   x.ResolutionDescription == resolution.ResolutionDescription).Count() > 0;
        }
        #endregion

        #region Future transaction specific helpers

        public string ParseDateFromTransactionDirectoryName(string dirUrl)
        {
            // for Future transactions, the directory name comes in as 'Future Transactions - MMMM d yyyy' format; we extract 'MMMM d yyyy' part
            string transactionDirName = dirUrl; // assume comes in as 'MMMM d yyyy'
            int index = dirUrl.LastIndexOf("-");
            if (index > 0) transactionDirName = dirUrl.Substring(index + 1).Trim(); // keep only 'MMMM d yyyy' part
            try
            {
                var dirDate = DateTime.ParseExact(transactionDirName, "MMMM d yyyy", new CultureInfo("en-US"));
                return dirDate.ToString("yyyy-MM-dd"); // sortable format
            }
            catch
            {
                // ignore
            }
            return string.Empty;
        }

        public string ParseDateFromTransactionFileUrl(string dirUrl)
        {
            // for Future transactions, the directory name comes in as 'Future Transactions - MMMM d yyyy' format; we extract 'MMMM d yyyy' part
            string transactionDirName = dirUrl; // assume comes in as 'MMMM d yyyy'
            int index = dirUrl.IndexOf("-");
            if (index > 0)
            {
                int endIndex = dirUrl.LastIndexOf("/");
                transactionDirName = dirUrl.Substring(index + 1, endIndex - index - 1).Trim(); // keep only 'MMMM d yyyy' part
            }

            try
            {
                var dirDate = DateTime.ParseExact(transactionDirName, "MMMM d yyyy", new CultureInfo("en-US"));
                return dirDate.ToString("yyyy-MM-dd"); // sortable format
            }
            catch
            {
                // ignore
            }
            return string.Empty;
        }

        private bool FutureInputSourceProcessed(string inputSource)
        {
            // future transactions processed daily; if the given input source is already in db, it has been processed
            return _context.FutureReservations.Count(p => p.InputSource == inputSource) > 0 ||
                   _context.FutureResolutions.Count(p => p.InputSource == inputSource) > 0;
        }

        private bool GrossInputSourceProcessed(string inputSource)
        {
            // future transactions processed daily; if the given input source is already in db, it has been processed
            return _context.GrossEarnings.Count(p => p.InputSource == inputSource) > 0 ||
                   _context.GrossEarnings.Count(p => p.InputSource == inputSource) > 0;
        }

        #endregion
    }
}