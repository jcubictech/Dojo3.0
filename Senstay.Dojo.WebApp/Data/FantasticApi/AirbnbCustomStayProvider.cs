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
    public class AirbnbCustomStayProvider : FantasticServiceBase
    {
        private readonly DojoDbContext _context;

        public AirbnbCustomStayProvider(DojoDbContext dbContext) : base(dbContext)
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
                return OptimizeCustomStayModels(models);
            }
            catch
            {
                throw;  // exit when there is input error
            }
        }

        private List<FantasticCustomStayModel> OptimizeCustomStayModels(List<FantasticCustomStayModel> models)
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
    }
}
