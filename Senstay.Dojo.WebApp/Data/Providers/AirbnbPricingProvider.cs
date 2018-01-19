using System.IO;
using OfficeOpenXml;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class AirbnbPricingProvider
    {
        private readonly DojoDbContext _context;

        public AirbnbPricingProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public void UploadPrices(Stream excelData)
        {
            try
            {
                using (var package = new ExcelPackage(excelData))
                {
                    ExcelWorkbook workBook = package.Workbook;
                    OffAirbnbRow currentRow = null;
                    if (workBook != null)
                    {
                        if (workBook.Worksheets.Count > 0)
                        {
                            ExcelWorksheet currentWorksheet = workBook.Worksheets[1];
                        }

                    }
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
