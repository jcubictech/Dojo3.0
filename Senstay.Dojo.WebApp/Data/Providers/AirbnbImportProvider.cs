using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Data;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.FtpClient;

namespace Senstay.Dojo.Data.Providers
{
    public class AirbnbImportProvider
    {
        private readonly DojoDbContext _context;

        public AirbnbImportProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public AirbnbImportCalendarModel GetImportStatistics()
        {
            var calendarModel = new AirbnbImportCalendarModel();
            try
            {
                // get the most recent owner payout date from db
                DateTime importDate = DateTime.Today; // default to today
                var mostRecentPayout = _context.OwnerPayouts.Where(x => x.IsDeleted == false).OrderByDescending(x => x.PayoutDate).FirstOrDefault();
                if (mostRecentPayout != null) importDate = mostRecentPayout.PayoutDate.Value;

                DateTime startDate = importDate.Date.AddDays(-AirbnbImportCalendarModel.IMPORT_DISPLAY_DAYS); // retrieve IMPORT_DISPLAY_DAYS worth of data
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@Account", SqlDbType.NVarChar);
                sqlParams[1].Value = string.Empty; // get all accounts

                var importItems = _context.Database.SqlQuery<AirbnbImportAccountItemModel>("GetImportStatistics @StartDate, @Account", sqlParams).ToList();

                string lastDateText = importItems.OrderByDescending(x => x.ImportDate).Select(x => x.ImportDate).FirstOrDefault();
                if (lastDateText != null)
                {
                    var latestDate = DateTime.ParseExact(lastDateText, "yyyy-MM-dd", new CultureInfo("en-US"));
                    calendarModel.LatestDate = latestDate;
                    calendarModel.DisplayDays = AirbnbImportCalendarModel.IMPORT_DISPLAY_DAYS; // days to display

                    calendarModel.CalendarHeaderRow = MakeCalendarHeader(latestDate, calendarModel.DisplayDays);

                    var currentAccount = string.Empty;
                    AirbnbImportCalendarRowModel calendarRow = null;
                    foreach (var item in importItems)
                    {
                        int column = ComputeColumn(latestDate, item.ImportDate);
                        if (column >= 0 && column < calendarModel.DisplayDays)
                        {
                            if (currentAccount != item.Account)
                            {
                                if (calendarRow != null) calendarModel.calendarRows.Add(calendarRow);
                                calendarRow = new AirbnbImportCalendarRowModel(calendarModel.DisplayDays);
                                currentAccount = item.Account;
                                calendarRow.Account = item.Account.Substring(0, item.Account.IndexOf("@"));
                            }
                            if (calendarRow != null)
                            {
                                calendarRow.CalendarCols[column].Column = column;
                                calendarRow.CalendarCols[column].ReservationData = item.ReservationCount.ToString();
                                calendarRow.CalendarCols[column].ResolutionData = item.ResolutionCount.ToString();
                                calendarRow.CalendarCols[column].GrossData = string.Empty;
                            }
                        }
                    }
                    // the last row
                    if (calendarRow != null) calendarModel.calendarRows.Add(calendarRow);

                    // fill in missing active accounts from AirbnAccount
                    var airbnbAccounts = _context.AirbnbAccounts.Where(x => x.Status == "Active").Select(x => x.Email).ToList();
                    foreach (string account in airbnbAccounts)
                    {
                        var accountName = account.Substring(0, account.IndexOf("@"));
                        var row = calendarModel.calendarRows.Where(x => x.Account == accountName).FirstOrDefault();
                        if (row == null) // these files has no data for the display range
                        {
                            var accountRow = new AirbnbImportCalendarRowModel(calendarModel.DisplayDays);
                            accountRow.Account = accountName;
                            accountRow.HasInputFile = 0;
                            calendarModel.calendarRows.Add(accountRow);
                        }
                    }
                    calendarModel.calendarRows = calendarModel.calendarRows.OrderBy(x => x.Account).ToList();

                    // mark accounts for associated inpurt files
                    AirbnbFtpService ftpService = new AirbnbFtpService();
                    var ftpDirectories = ftpService.GetFolderList(AirbnbFtpConfig.CompletedFtpUrl);
                    string dirName = latestDate.ToString("MMMM d yyyy");
                    var ftpFileUrls = ftpService.GetFileList(ftpDirectories[dirName]);
                    foreach (string fileName in ftpFileUrls.AllKeys)
                    {
                        var accountName = fileName.Substring(0, fileName.IndexOf("@"));
                        var row = calendarModel.calendarRows.Where(x => x.Account == accountName).FirstOrDefault();
                        if (row != null) row.HasInputFile = 1; // has current date data file
                    }

                    // check date before most recent date for missing file
                    dirName = latestDate.AddDays(-1).ToString("MMMM d yyyy");
                    ftpFileUrls = ftpService.GetFileList(ftpDirectories[dirName]);
                    foreach (string fileName in ftpFileUrls.AllKeys)
                    {
                        var accountName = fileName.Substring(0, fileName.IndexOf("@"));
                        var row = calendarModel.calendarRows.Where(x => x.Account == accountName).FirstOrDefault();
                        if (row != null) row.HasInputFile ^= 2; // has previous date's data file
                    }

                    // check 2 date before most recent date for missing file
                    dirName = latestDate.AddDays(-2).ToString("MMMM d yyyy");
                    ftpFileUrls = ftpService.GetFileList(ftpDirectories[dirName]);
                    foreach (string fileName in ftpFileUrls.AllKeys)
                    {
                        var accountName = fileName.Substring(0, fileName.IndexOf("@"));
                        var row = calendarModel.calendarRows.Where(x => x.Account == accountName).FirstOrDefault();
                        if (row != null) row.HasInputFile ^= 4; // has previous's previous date's data file
                    }
                }
            }
            catch
            {
                throw;
            }

            return calendarModel;
        }

        public List<InputError> RetrieveImportLog()
        {
            DateTime cutOffDate = DateTime.Today.AddDays(-28);
            return _context.InputErrors.Where(x => x.CreatedTime > cutOffDate)
                                       .OrderByDescending(x => x.CreatedTime)
                                       .ToList();
        }

        private List<string> MakeCalendarHeader(DateTime latestDate, int days)
        {
            DateTime date = latestDate;
            var header = new List<string>();
            for (int i = 0; i < days; i++)
            {
                header.Add(date.ToString("MM/dd"));
                date = date.AddDays(-1);
            }

            return header;
        }

        private int ComputeColumn(DateTime latestDate, string importDateText)
        {
            DateTime importDate = DateTime.ParseExact(importDateText, "yyyy-MM-dd", new CultureInfo("en-US"));
            return (latestDate - importDate).Days;
        }
    }
}
