using System;
using System.IO;
using System.Text;
using OfficeOpenXml;

namespace DojoTools
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateSqlFromExcel();
        }

        /// <summary>
        /// Generate SQL update statement from an Excel file for property.
        /// The Excel file contains data update for Owner, OwnerEntity, OwnerPayout, and OutstandingBalance for active PropertyCode.
        /// </summary>
        /// <param name="excelFile">Excel file path</param>
        /// <returns>status of the operation</returns>
        /// <output>a SQL script containing the Update script for each PropertyCode in the Excel file</output>
        private static Status CreateSqlFromExcel()
        {
            try
            {
                Console.Write("Enter a Excel file path: ");
                string excelFile = Console.ReadLine();

                const string UPDATE_SQL = "Update [dbo].[CPL] set [OutstandingBalance] = {0}, [OwnerEntity] = '{1}', [OwnerPayout] = '{2}', [Owner] = '{3}' Where [PropertyCode] = '{4}'\r\n";
                var excelFileInfo = new FileInfo(excelFile);
                using (var package = new ExcelPackage(excelFileInfo))
                {
                    ExcelWorkbook workBook = package.Workbook;
                    if (workBook != null)
                    {
                        if (workBook.Worksheets.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder();
                            const int startRow = 2; // row 1 is header that we skip
                            ExcelWorksheet currentWorksheet = workBook.Worksheets[1]; // first sheet index starts from 1
                            for (int row = startRow; row <= currentWorksheet.Dimension.End.Row; row++)
                            {
                                PropertyChange pc = new PropertyChange();
                                pc.PropertyCode = GetSafeCellString(currentWorksheet.Cells[row, 1].Value);
                                pc.OutstandingBalance = GetSafeCellString(currentWorksheet.Cells[row, 2].Value);
                                pc.OwnerEntity = GetSafeCellString(currentWorksheet.Cells[row, 3].Value);
                                pc.OwnerPayout = GetSafeCellString(currentWorksheet.Cells[row, 4].Value);
                                pc.OwnerContact = GetSafeCellString(currentWorksheet.Cells[row, 5].Value);

                                if (!string.IsNullOrEmpty(pc.PropertyCode))
                                    sb.AppendFormat(UPDATE_SQL, pc.OutstandingBalance, pc.OwnerEntity, pc.OwnerPayout, pc.OwnerContact, pc.PropertyCode);
                            }

                            // write to output file if not empty
                            if (sb.Length > 0)
                            {
                                string outputFile = Path.ChangeExtension(excelFile, "sql");
                                File.WriteAllText(outputFile, sb.ToString());
                                Console.WriteLine("The SQL update statement is written to file '" + outputFile + "'.");
                                return Status.OutputWritten;
                            }
                        }
                    }
                    Console.WriteLine("There is no data in the Excel file.");
                    return Status.NoData;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Status.Error;
            }
        }

        public static string GetSafeCellString(object cellValue)
        {
            return cellValue == null ? string.Empty : cellValue.ToString();
        }
    }

    class PropertyChange
    {
        public string PropertyCode { get; set; }
        public string OutstandingBalance { get; set; }
        public string OwnerEntity { get; set; }
        public string OwnerPayout { get; set; }
        public string OwnerContact { get; set; }
    }

    enum Status
    {
        OutputWritten = 0,
        NoData = 1,
        Error = 2
    }
}
