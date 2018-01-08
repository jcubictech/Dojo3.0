using System;
using System.IO;
using System.Collections.Generic;
using OfficeOpenXml;
using System.Linq;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyBalanceImportProvider
    {
        private readonly DojoDbContext _context;

        // import xlsx column indices
        private const int _PropertyCol = 1;
        private const int _BalanceCol = 2;

        public PropertyBalanceImportProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public int ImportExcel(Stream excelData, DateTime targetDate)
        {
            int errorCount = 0;
            int notFoundCount = 0;
            int propertyCount = 0;
            int totalCols = 2;
            int startRow = 2; // starting row to read data
            string inputSource = "Begin Balance Sweep";
            int month = targetDate.Month;

            // if target date given is not after 07/01/2017, set the month to the prior month of today
            if (targetDate < new DateTime(2017, 7, 1)) month = DateTime.Today.Date.AddMonths(-1).Month;

            using (var package = new ExcelPackage(excelData))
            {
                ExcelWorkbook workBook = package.Workbook;
                if (workBook != null)
                {
                    if (workBook.Worksheets.Count > 0)
                    {
                        ExcelWorksheet currentWorksheet = workBook.Worksheets[1];

                        // storage for parsed data
                        var errorRows = new List<InputError>();
                        var propertyBalance = new List<PropertyBalance>();

                        for (int row = startRow; row <= currentWorksheet.Dimension.End.Row; row++)
                        {
                            if (currentWorksheet.Dimension.End.Column != totalCols)
                            {
                                var message = string.Format("The total number of Begin Balance columns {0:d} does not match {1:d}", currentWorksheet.Dimension.End.Column, totalCols);
                                var inputError = CreateInputError(inputSource, row, "Parse", message, "Excel row");
                                errorRows.Add(inputError);
                                errorCount++;
                            }

                            try
                            {
                                var p = ParseExcelRow(currentWorksheet.Cells, row, month);

                                // if property is not fund, we use a placehoder property so it can be fixed later
                                if (!EnsurePropertyCode(p.PropertyCode))
                                {
                                    var message = string.Format("Property '{0}' does not exist.", p.PropertyCode);
                                    var inputError = CreateInputError(inputSource, row, inputSource, message , "Excel row");
                                    errorRows.Add(inputError);
                                    notFoundCount++;
                                }
                                else
                                {
                                    propertyBalance.Add(p);
                                    propertyCount++;
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
                                if (propertyBalance != null && propertyBalance.Count > 0)
                                    _context.PropertyBalances.AddRange(propertyBalance);

                                if (errorRows.Count() > 0)
                                    _context.InputErrors.AddRange(errorRows);

                                _context.SaveChanges(); // Save Reservations
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

            return errorCount == 0 ? propertyCount * 10000 + notFoundCount : -errorCount;
        }

        #region private methods

        private PropertyBalance ParseExcelRow(ExcelRange cells, int row, int month)
        {
            var balance = Math.Round(GetSafeNumber(cells[row, _BalanceCol].Value), 2, MidpointRounding.AwayFromZero);
            var p = new PropertyBalance()
            {
                PropertyCode = GetSafeCellString(cells[row, _PropertyCol].Value),
                Month = month,
                BeginningBalance = balance,
                AdjustedBalance = balance
            };
            return p;
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
}