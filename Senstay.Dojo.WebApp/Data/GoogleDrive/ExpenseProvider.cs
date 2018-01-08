using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Data.Entity;
using OfficeOpenXml;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class ExpenseProvider : CrudProviderBase<Expense>
    {
        private readonly DojoDbContext _context;

        public ExpenseProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public Expense GetRebalanceExpenseByKey(string propertyCode, string category, DateTime transactionDate)
        {
            var entity = _context.Expenses.Where(x => x.PropertyCode == propertyCode &&
                                                      x.Category == category &&
                                                      DbFunctions.TruncateTime(x.ExpenseDate) == transactionDate.Date &&
                                                      x.ParentId == 0)
                                          .OrderByDescending(x => x.ExpenseId)
                                          .FirstOrDefault();

            return entity;
        }

        public int GetGroupByKey(string propertyCode, string category, DateTime transactionDate)
        {
            var entity = _context.Expenses.Where(x => x.PropertyCode == propertyCode &&
                                                      x.Category == category &&
                                                      DbFunctions.TruncateTime(x.ExpenseDate) == transactionDate.Date &&
                                                      x.ParentId == x.ExpenseId)
                                          .OrderByDescending(x => x.ExpenseId)
                                          .FirstOrDefault();

            return entity != null ? entity.ExpenseId : 0;
        }

        public void CreateExpenseGroup(Expense entity)
        {
            try
            {
                var dataProvider = new ExpenseProvider(_context);
                dataProvider.Create(entity);
                dataProvider.Commit();

                if (entity.ExpenseId == 0)
                    entity.ExpenseId = dataProvider.GetGroupByKey(entity.PropertyCode, entity.Category, entity.ExpenseDate.Value);

                if (entity.ExpenseId > 0)
                {
                    entity.ParentId = entity.ExpenseId;
                    dataProvider.Update(entity.ExpenseId, entity);
                    dataProvider.Commit();
                }
            }
            catch
            {
                throw;
            }
        }

        public List<Expense> RetrieveCombinedExpenses(DateTime beginDate, DateTime endDate, string propertyCode = "")
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = beginDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;

                List<Expense> data = _context.Database.SqlQuery<Expense>("RetrieveCombinedExpenses @StartDate, @EndDate, @PropertyCode", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
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
