using OfficeOpenXml;
using System;
using System.IO;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class StreamlineProvider : CrudProviderBase<CPL>
    {
        private readonly DojoDbContext _context;

        public StreamlineProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public void Import(byte[] content)
        {
            MemoryStream excelStream = new MemoryStream(content);
            using (var package = new ExcelPackage(excelStream))
            {
                ExcelWorkbook workBook = package.Workbook;
                if (workBook != null)
                {
                    if (workBook.Worksheets.Count > 0)
                    {
                        int startRow = 2;
                        int startCol = 1;
                        int errorCount = 0;

                        ExcelWorksheet currentWorksheet = workBook.Worksheets[1];
                        for (int row = startRow; row <= currentWorksheet.Dimension.End.Row; row++)
                        {
                            try
                            {
                                if (currentWorksheet.Cells[row, startCol].Value == null) break;
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                // ignore and continue;
                            }
                        }
                        if (errorCount == 0) _context.SaveChanges();
                    }
                }
            }
        }
    }
}
