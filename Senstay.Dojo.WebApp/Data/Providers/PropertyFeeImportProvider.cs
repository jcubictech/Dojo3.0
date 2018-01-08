using System;
using System.IO;
using System.Collections.Generic;
using OfficeOpenXml;
using System.Linq;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyFeeImportProvider
    {
        private readonly DojoDbContext _context;

        // import xlsx column indices
        private const int _propertyCol = 1;
        private const int _WaiverCol = 2;
        private const int _taxCol = 3;

        public PropertyFeeImportProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public int ImportExcel(Stream excelData)
        {
            int errorCount = 0;
            int notFoundCount = 0;
            int entityCount = 0;
            int totalCols = 3;
            int startRow = 2; // starting row to read data
            string inputSource = "Property Fees and Taxes";
            DateTime today = DateTime.UtcNow;

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
                        List<PropertyFee> propertyFees = new List<PropertyFee>();

                        for (int row = startRow; row <= currentWorksheet.Dimension.End.Row; row++)
                        {
                            if (currentWorksheet.Dimension.End.Column != totalCols)
                            {
                                var message = string.Format("The total number of Property Fees & Taxes columns {0:d} does not match {1:d}", currentWorksheet.Dimension.End.Column, totalCols);
                                var inputError = CreateInputError(inputSource, row, "Parse", message, "Excel row");
                                errorRows.Add(inputError);
                                errorCount++;
                            }

                            try
                            {
                                var p = ParseExcelRow(currentWorksheet.Cells, row, today, inputSource);

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
                                    propertyFees.Add(p);
                                    entityCount++;
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
                                if (propertyFees != null && propertyFees.Count > 0)
                                    _context.PropertyFees.AddRange(propertyFees);

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

            return errorCount == 0 ? entityCount * 10000 + notFoundCount : -errorCount;
        }

        #region private methods

        private PropertyFee ParseExcelRow(ExcelRange cells, int row, DateTime date, string inputSource)
        {
            var p = new PropertyFee();
            p.PropertyCode = GetSafeCellString(cells[row, _propertyCol].Value);
            p.DamageWaiver = GetSafeNumber(cells[row, _WaiverCol].Value);
            p.CityTax = Math.Round(GetSafeNumber(cells[row, _taxCol].Value), 2, MidpointRounding.AwayFromZero);

            // TODO: if PropertyFee file has these fields, read them in
            p.Cleanings = null;
            p.Consumables = null;
            p.Landscaping = null;
            p.Laundry = null;
            p.PoolService = null;
            p.TrashService = null;
            p.PestService = null;

            p.EntryDate = ConversionHelper.EnsureUtcDate(date);
            p.InputSource = inputSource;

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