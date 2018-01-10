using System;
using System.IO;
using System.Collections.Generic;
using OfficeOpenXml;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using System.ComponentModel.DataAnnotations.Schema;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Models.HelperClass;

namespace Senstay.Dojo.Data.Providers
{
    public class JobCostImportProvider
    {
        private readonly DojoDbContext _context;
        // Job Cost xlsx file column indices
        private const int _costTotalCol = 1;
        private const int _costPayeeCol = 2;
        private const int _costPropertyCol = 3;
        private const int _costProperty2Col = 4;
        private const int _costSkip1Col = 5;
        private const int _costSkip2Col = 6;
        private const int _costTypeCol = 7;
        private const int _costSkip3Col = 8;
        private const int _costDateCol = 9;
        private const int _costSkip4Col = 10;
        private const int _costNumberCol = 11;
        private const int _costSkip5Col = 12;
        private const int _costSourceCol = 13;
        private const int _costSkip6Col = 14;
        private const int _costMemoCol = 15;
        private const int _costSkip7Col = 16;
        private const int _costAccountCol = 17;
        private const int _costSkip8Col = 18;
        private const int _costClassCol = 19;
        private int _costSkip9Col = 20;
        private int _costBillingCol = 21;
        private int _costSkip10Col = 22;
        private int _costAmountCol = 23;
        private int _costSkip11Col = 24;
        private int _costBalanceCol = 25;

        public JobCostImportProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public int ImportExcel(Stream excelData, bool newVersion)
        {
            int startRow = 2; // starting row for reservation data
            int errorCount = 0;
            string currentProperty = string.Empty;
            string currentPayee = string.Empty;
            string inputSource = "Job Cost Excel";
            int totalCols = newVersion ? 25: 23;
            int billingStatusOffset = newVersion ? 0 : -2;
            _costSkip10Col += billingStatusOffset;
            _costAmountCol += billingStatusOffset;
            _costSkip11Col += billingStatusOffset;
            _costBalanceCol += billingStatusOffset;

            List<JobCost> jobCosts = new List<JobCost>();
            var propertyProvider = new PropertyProvider(_context);

            using (var package = new ExcelPackage(excelData))
            {
                ExcelWorkbook workBook = package.Workbook;
                if (workBook != null)
                {
                    if (workBook.Worksheets.Count > 0)
                    {
                        ExcelWorksheet currentWorksheet = workBook.Worksheets[1];

                        // storage for parsed data
                        List<InputError> errorRows = new List<InputError>();

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
                                JobCostRow costRow = ParseJobCostExcelRow(currentWorksheet.Cells, row, inputSource);

                                // the last row has 'Total' on the first column
                                if (IsLastRow(costRow)) break;

                                if (IsPropertyRow(costRow))
                                {
                                    currentProperty = costRow.PropertyCode != string.Empty ? costRow.PropertyCode : costRow.JobCostProperty2;
                                }
                                else if (IsOwnerRow(costRow))
                                {
                                    currentPayee = costRow.JobCostPayee;
                                }
                                else if (IsCostRow(costRow))
                                {
                                    costRow.OriginalPropertyCode = currentProperty;
                                    if (propertyProvider.PropertyExist(currentProperty))
                                        costRow.PropertyCode = currentProperty;
                                    else
                                        costRow.PropertyCode = AppConstants.DEFAULT_PROPERTY_CODE;

                                    costRow.JobCostPayoutTo = currentPayee;
                                    jobCosts.Add(MapJobCost(costRow));
                                }
                                else if (IsOwnerTotalRow(costRow) || IsSubTotalRow(costRow))
                                {
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                var message = "Data parse exception: " + ex.Message;
                                var inputError = CreateInputError(inputSource, row, "Exception", message, "Job Cost Excel row");
                                errorRows.Add(inputError);
                                errorCount++;
                            }
                        }

                        try
                        {
                            // save job cost if there is no error
                            if (errorCount == 0 && jobCosts.Count > 0)
                            {
                                _context.JobCosts.AddRange(jobCosts);
                                _context.SaveChanges(); // save job costs
                            }
                        }
                        catch (Exception ex)
                        {
                            var message = "Job Cost saving error: " + ex.Message;
                            var inputError = CreateInputError(inputSource, 0, "Exception", message, "Database saving");
                            _context.InputErrors.Add(inputError);
                            _context.SaveChanges(); // save errors
                            errorCount = 100000; // a large number
                        }
                    }
                }
            }

            return errorCount == 0 ? jobCosts.Count * 10000 : -errorCount;
        }

        public int CreateExpenses(int month, int year, int startJobCostId)
        {
            try
            {
                DateTime startDate = new DateTime(year, month, 1);
                DateTime endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@StartJobCostId", SqlDbType.Int);
                sqlParams[2].Value = startJobCostId;
                var result = _context.Database.SqlQuery<SqlResult>("CreateExpensesFromJobCosts @StartDate, @EndDate, @StartJobCostId", sqlParams).FirstOrDefault();
                if (result != null)
                    return result.Count;
                else
                    return 0;
            }
            catch
            {
                throw;
            }
        }

        #region private methods

        private JobCostRow ParseJobCostExcelRow(ExcelRange cells, int row, string inputSource)
        {
            var costRow = new JobCostRow();
            costRow.JobCostTotal = GetSafeCellString(cells[row, _costTotalCol].Value);
            costRow.JobCostPayee = GetSafeCellString(cells[row, _costPayeeCol].Value);
            costRow.PropertyCode = GetSafeCellString(cells[row, _costPropertyCol].Value);
            costRow.JobCostProperty2 = GetSafeCellString(cells[row, _costProperty2Col].Value);
            costRow.JobCostType = GetSafeCellString(cells[row, _costTypeCol].Value);
            costRow.JobCostDate = GetSafeDate(cells[row, _costDateCol].Text);
            costRow.JobCostNumber = GetSafeCellString(cells[row, _costNumberCol].Value);
            costRow.JobCostSource = GetSafeCellString(cells[row, _costSourceCol].Value);
            costRow.JobCostMemo = GetSafeCellString(cells[row, _costMemoCol].Value);
            costRow.JobCostAccount = GetSafeCellString(cells[row, _costAccountCol].Value);
            costRow.JobCostClass = GetSafeCellString(cells[row, _costClassCol].Value);
            costRow.JobCostBillable = string.Compare(GetSafeCellString(cells[row, _costBillingCol].Value), "Unbilled", true) == 0 ? true : false;
            costRow.JobCostAmount = GetSafeNumber(cells[row, _costAmountCol].Text);
            costRow.JobCostBalance = GetSafeNumber(cells[row, _costBalanceCol].Text);

            // temporary testing for billable and unbillable setting
            //if (costRow.JobCostDate != null)
            //{
            //    if (costRow.JobCostDate.Value.Day == 31) costRow.JobCostDate = costRow.JobCostDate.Value.AddDays(-1);
            //    costRow.JobCostDate = costRow.JobCostDate.Value.AddMonths(1);
            //}

            // house keeping fields
            costRow.JobCostInputSource = inputSource;

            return costRow;
        }

        private JobCost MapJobCost(JobCostRow costRow)
        {
            return new JobCost
            {
                PropertyCode = costRow.PropertyCode,
                JobCostPayoutTo = costRow.JobCostPayoutTo,
                JobCostType = costRow.JobCostType,
                JobCostDate = ConversionHelper.EnsureUtcDate(costRow.JobCostDate),
                JobCostNumber = costRow.JobCostNumber,
                JobCostSource = costRow.JobCostSource,
                JobCostMemo = costRow.JobCostMemo,
                JobCostAccount = costRow.JobCostAccount,
                JobCostClass = costRow.JobCostClass,
                JobCostBillable = costRow.JobCostBillable,
                JobCostAmount = costRow.JobCostAmount,
                JobCostBalance = costRow.JobCostBalance,
                OriginalPropertyCode = costRow.OriginalPropertyCode
            };
        }

        private bool IsLastRow(JobCostRow row)
        {
            return string.Compare(row.JobCostTotal, "total", true) == 0;
        }

        private bool IsCostRow(JobCostRow row)
        {
            return row.JobCostDate != null && row.JobCostAccount != string.Empty;
        }

        private bool IsPropertyRow(JobCostRow row)
        {
            return !string.IsNullOrEmpty(row.PropertyCode) || !string.IsNullOrEmpty(row.JobCostProperty2);
        }

        private bool IsSubTotalRow(JobCostRow row)
        {
            return !string.IsNullOrEmpty(row.PropertyCode) && !IsCostRow(row);
        }

        private bool IsOwnerTotalRow(JobCostRow row)
        {
            return !string.IsNullOrEmpty(row.JobCostPayee) && !IsCostRow(row);
        }

        private bool IsOwnerRow(JobCostRow row)
        {
            return !string.IsNullOrEmpty(row.JobCostPayee) && !IsCostRow(row) && row.JobCostBalance == 0 && row.JobCostAmount == 0;
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

    [NotMapped]
    public class JobCostRow : JobCost
    {
        public string JobCostTotal { get; set; }

        public string JobCostPayee { get; set; }

        public string JobCostProperty2 { get; set; }

        public string JobCostInputSource { get; set; }
    }
}