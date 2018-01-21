using System;
using System.IO;
using System.Globalization;
using System.Linq;
using OfficeOpenXml;
using Senstay.Dojo.Models;
using System.Collections.Generic;
using Senstay.Dojo.Fantastic.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class AirbnbCustomStayProvider
    {
        private readonly DojoDbContext _context;
        private const int _dateCol = 1;

        public AirbnbCustomStayProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public List<FantasticCustomStayModel> ImportCustomStays(Stream excelData)
        {
            const int sheetIndex = 2;
            var models = new List<FantasticCustomStayModel>();
            List<int> listingIds = null;
            try
            {
                using (var package = new ExcelPackage(excelData))
                {
                    ExcelWorkbook workBook = package.Workbook;
                    int startRow = 1;
                    int priceStartRow = 3;
                    if (workBook != null)
                    {
                        if (workBook.Worksheets.Count > 0)
                        {
                            ExcelWorksheet currentWorksheet = workBook.Worksheets[sheetIndex];

                            for (int row = startRow; row <= currentWorksheet.Dimension.End.Row; row++) // excel row index starts from 1
                            {
                                // the first row is property row
                                if (row == startRow)
                                {
                                    var properties = ParsePropertyRow(currentWorksheet.Cells, row, currentWorksheet.Dimension.End.Column);
                                    listingIds = MapPropertyToIds(properties);
                                    if (properties.Count != listingIds.Count)
                                        throw new Exception("Not all properties have matching listing IDs in the import file. Please check the name of the property code.");
                                    else if (properties.Count == 0)
                                        throw new Exception("There is no property found in the import file. Please check the format of the file.");
                                }
                                else if (row >= priceStartRow)
                                {
                                    try
                                    {
                                        var minStays = ParseMinStayRow(currentWorksheet.Cells, row, currentWorksheet.Dimension.End.Column, listingIds);
                                        models.AddRange(minStays);
                                    }
                                    catch
                                    {
                                        throw; // exit when there is input error
                                    }
                                }
                            }
                        }
                    }
                }
                return OptimizePCustomStayModels(models);
            }
            catch
            {
                throw;  // exit when there is input error
            }
        }

        private List<FantasticCustomStayModel> OptimizePCustomStayModels(List<FantasticCustomStayModel> models)
        {
            var optimizedModels = new List<FantasticCustomStayModel>();
            if (models == null || models.Count == 0) return optimizedModels;

            var orderedModels = models.OrderBy(x => x.ListingId).ThenBy(x => x.StartDate).ToList();
            var currentModel = orderedModels[0];
            var lastModel = currentModel;
            foreach (var model in orderedModels)
            {
                // optimize the API call to bundle consecutive dates that has same minimum stay into one call
                if (currentModel.ListingId == model.ListingId && // same listing id 
                    (model.StartDate.Date - lastModel.StartDate.Date).Days <= 1 && // in consecutive or same date
                    currentModel.MinStay == model.MinStay) // having same minimum stay
                {
                    lastModel = model;
                    continue;
                }
                optimizedModels.Add(new FantasticCustomStayModel
                {
                                        ListingId = currentModel.ListingId,
                                        StartDate = currentModel.StartDate,
                                        EndDate = lastModel.StartDate,
                                        MinStay = currentModel.MinStay,
                });
                currentModel = model;
                lastModel = model;
            }

            // last one
            optimizedModels.Add(new FantasticCustomStayModel
            {
                ListingId = currentModel.ListingId,
                StartDate = currentModel.StartDate,
                EndDate = lastModel.StartDate,
                MinStay = currentModel.MinStay,
            });

            return optimizedModels;
        }

        private List<FantasticCustomStayModel> ParseMinStayRow(ExcelRange cells, int row, int totalCols, List<int> listingIds)
        {
            var models = new List<FantasticCustomStayModel>();
            DateTime startDate = new DateTime(2050, 12, 31); // arbitrary future date
            int index = 0;
            try
            {
                for (int col = _dateCol; col <= totalCols; col++) // excel column index starts from 1
                {
                    if (col == _dateCol)
                    {
                        if (DateTime.TryParseExact(cells[row, col].Text, "MM/dd/yy", new CultureInfo("en-US"), DateTimeStyles.None, out startDate) == false)
                            throw new Exception(string.Format("Input error at row {0:d} for date.", row));
                    }
                    else
                    {
                        int minStay = 0;
                        if (int.TryParse(cells[row, col].Text, out minStay) == true)
                        {
                            var model = new FantasticCustomStayModel
                            {
                                ListingId = listingIds[index++],
                                StartDate = startDate,
                                EndDate = startDate,
                                MinStay = minStay
                            };
                            models.Add(model);
                        }
                        else
                            throw new Exception(string.Format("Input error at row {0:d} for minimum stay.", row));
                    }
                }
                return models;
            }
            catch
            {
                throw;
            }
        }

        private List<string> ParsePropertyRow(ExcelRange cells, int row, int totalCols)
        {
            var properties = new List<string>();

            if (cells[row, _dateCol].Text != string.Empty && cells[row, _dateCol+1].Text != string.Empty)
            {
                for (int col = _dateCol+1; col <= totalCols; col++) // excel column index starts from 1
                {
                    properties.Add(GetSafeCellString(cells[row, col].Value));
                }
            }
            return properties;
        }

        private List<int> MapPropertyToIds(List<string> properties)
        {
            var ids = new List<int>();
            foreach(string property in properties)
            {
                var id = MapProperty(property);
                if (id != 0) ids.Add(id);
            }
            return ids;
        }

        private int MapProperty(string property)
        {
            // TODO: create Fantastic listing Id to property table
            if (property == "SD011")
                return 1157;
            else if (property == "SD012")
                return 1158;
            else // no matching listing Id
                return 0;
        }

        private string GetSafeCellString(object cellValue, string defaultValue = "")
        {
            return cellValue == null ? defaultValue : cellValue.ToString();
        }
    }
}
