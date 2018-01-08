using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyBalanceProvider : CrudProviderBase<PropertyBalance>
    {
        private readonly DojoDbContext _context;

        public PropertyBalanceProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<PropertyBalance> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.PropertyCode).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<StatementCarryOverModel> RetrieveCarryOvers(int month, int year, string propertyCode = "")
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@Month", SqlDbType.Int);
                sqlParams[0].Value = month;
                sqlParams[1] = new SqlParameter("@Year", SqlDbType.Int);
                sqlParams[1].Value = year;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;
                var carryovers = _context.Database.SqlQuery<StatementCarryOverModel>("GetCarryOverForProperties @Month, @Year, @PropertyCode", sqlParams).ToList();

                var model = new List<StatementCarryOverModel>();
                if (propertyCode != string.Empty) // carryovers is the list of properties that the PropertyCode related to via Payout Method
                {
                    foreach (var item in carryovers)
                    {
                        if (item.PropertyCode != propertyCode) continue;

                        if (ConversionHelper.MoneyEqual(item.CarryOver, 0)) // all paid off
                        {
                            item.CarryOver = 0;
                        }
                        else if (item.CarryOver > 0) // positive balance -> each individual balance is carrried over
                        {
                            item.CarryOver = item.Balance;
                        }
                        else // negative balance
                        {
                            if (IsOverpaid(item)) // the overpaid amount will be added to next month as Advance Payout item
                            {
                                item.CarryOver = 0;
                            }
                            else
                            {
                                item.CarryOver = item.Balance; // rollover each ending balance
                            }
                        }
                        model.Add(item);
                    }
                }
                else {
                    // split carrovers into properties
                    var result = carryovers.GroupBy(g => new { g.PayoutMethodName, g.CarryOver })
                                           .Select(g => new GroupedCarryOverModel
                                           {
                                               PayoutMethodName = g.Key.PayoutMethodName,
                                               CarryOverAmount = g.Key.CarryOver,
                                               Balances = g.ToList()
                                           }).ToList();

                    foreach (var item in result)
                    {
                        if (ConversionHelper.MoneyEqual(item.CarryOverAmount, 0)) // all paid off
                        {
                            foreach (var balance in item.Balances)
                            {
                                balance.CarryOver = 0;
                                model.Add(balance);
                            }
                        }
                        //else if (item.CarryOverAmount > 0 && item.Balances.Count == 1) // positive balance for 1 property
                        //{
                        //    item.Balances[0].CarryOver = item.CarryOverAmount;
                        //    model.Add(item.Balances[0]);
                        //}
                        else if (item.CarryOverAmount > 0) // positive balance -> each individual positive balance is proportionately carrried over; negative set to 0
                        {
                            double ratio = item.CarryOverAmount / item.Balances.Where(x => x.Balance > 0).Sum(x => x.Balance);
                            foreach (var balance in item.Balances)
                            {
                                if (balance.Balance > 0)
                                    balance.CarryOver = Math.Round(balance.Balance * ratio, 2);
                                else
                                    balance.CarryOver = 0;

                                model.Add(balance);
                            }
                        }
                        else // negative balance
                        {
                            foreach (var balance in item.Balances)
                            {
                                if (IsOverpaid(balance)) // the overpaid amount will be added to next month as Advance Payout item
                                {
                                    balance.CarryOver = 0;
                                    model.Add(balance);
                                }
                                else
                                {
                                    balance.CarryOver = balance.Balance;
                                    model.Add(balance); // rollover the ending balance

                                    // this code is to rollover the average to non-zero ending balance properties
                                    //var averageCount = GetAverageCount(item.Balances);
                                    //double average = Math.Round(item.CarryOverAmount / averageCount, 2);
                                    //bool first = true;
                                    //if (!ConversionHelper.MoneyEqual(balance.Balance, 0))
                                    //{
                                    //    if (first)
                                    //    {
                                    //        balance.CarryOver = item.CarryOverAmount - average * (averageCount - 1);
                                    //        first = false;
                                    //    }
                                    //    else
                                    //        balance.CarryOver = average;
                                    //    model.Add(balance);
                                    //}
                                    //else
                                    //{
                                    //    balance.CarryOver = 0;
                                    //    model.Add(balance);
                                    //}
                                }
                            }
                        }
                    }
                }

                return model;
            }
            catch
            {
                throw;
            }
        }

        public void UpdateNextMonthBalances(DateTime month)
        {
            try
            {
                DateTime comingMonth = month.AddMonths(1);
                int nextMonth = comingMonth.Month;
                int nextYear = comingMonth.Year;

                // get the balance carryover from owner statements
                var balanceProvider = new PropertyBalanceProvider(_context);
                var carryovers = RetrieveCarryOvers(month.Month, month.Year);

                // insert or update property balances
                foreach (var carryover in carryovers)
                {
                    var balance = _context.PropertyBalances.Where(b => b.PropertyCode == carryover.PropertyCode && b.Month == nextMonth && b.Year == nextYear)
                                                           .FirstOrDefault();
                    if (balance == null)
                    {
                        balance = new PropertyBalance();
                        MapData(carryover, ref balance, nextMonth, nextYear);
                        balanceProvider.Create(balance);
                    }
                    else
                    {
                        MapData(carryover, ref balance, nextMonth, nextYear);
                        balanceProvider.Update(balance.PropertyBalanceId, balance);
                    }
                }

                // if there is no owner statement for this month but there is remaining balance for last month, we carry it over
                var lastMonthBalances = _context.PropertyBalances.Where(b => b.Month == month.Month && b.Year == month.Year).ToList();
                var nextMonthBalances = _context.PropertyBalances.Where(b => b.Month == nextMonth && b.Year == nextYear).ToList();
                foreach(var balance in lastMonthBalances)
                {
                    var matchBalance = nextMonthBalances.Where(x => x.PropertyCode == balance.PropertyCode).FirstOrDefault();
                    if (matchBalance == null)
                    {
                        var missingBalance = new PropertyBalance();
                        missingBalance.BeginningBalance = balance.AdjustedBalance;
                        missingBalance.AdjustedBalance = balance.AdjustedBalance;
                        missingBalance.PropertyCode = balance.PropertyCode;
                        missingBalance.Month = nextMonth;
                        missingBalance.Year = nextYear;
                        balanceProvider.Create(missingBalance);
                    }
                }

                balanceProvider.Commit();
            }
            catch
            {
                throw;
            }
        }

        public PropertyBalance Retrieve(string propertyCode, int month, int year)
        {
            return _context.PropertyBalances
                           .Where(p => p.PropertyCode == propertyCode && p.Month == month && p.Year == year)
                           .FirstOrDefault();
        }

        public void RedistributeBalance(List<PropertyBalance> balances)
        {
            try
            {
                int balanceMonth = 0,
                    balanceYear = 0;
                foreach (PropertyBalance balance in balances)
                {
                    if (balanceMonth == 0)
                    {
                        var rebalanceMonth = new DateTime(balance.Year, balance.Month, 15);
                        rebalanceMonth = rebalanceMonth.AddMonths(1);
                        balanceMonth = rebalanceMonth.Month;
                        balanceYear = rebalanceMonth.Year;
                    }

                    var entity = Retrieve(balance.PropertyCode, balanceMonth, balanceYear);
                    if (entity == null)
                    {
                        entity = new PropertyBalance
                        {
                            PropertyCode = balance.PropertyCode,
                            Month = balanceMonth,
                            Year = balanceYear,
                            BeginningBalance = balance.BeginningBalance,
                            AdjustedBalance = balance.AdjustedBalance,
                        };
                        Create(entity);
                    }
                    else
                    {
                        entity.AdjustedBalance = balance.AdjustedBalance;
                        Update(entity.PropertyBalanceId, entity);
                    }
                }
                Commit();
            }
            catch
            {
                throw;
            }
        }

        private bool IsOverpaid(StatementCarryOverModel model)
        {
            return Math.Abs(model.TotalPayment - model.TotalBalance) > 0.01 &&
                   //model.TotalPayment > 0 && model.CarryOver < 0;
                   model.TotalBalance > 0 && model.TotalPayment > 0 && model.CarryOver < 0;
        }

        private int GetAverageCount(List<StatementCarryOverModel> model)
        {
            int count = 0;
            foreach (StatementCarryOverModel item in model)
            {
                if (!ConversionHelper.MoneyEqual(item.Balance, 0)) count++;
            }
            return count;
        }

        private void MapData(StatementCarryOverModel model, ref PropertyBalance balance, int month, int year)
        {
            balance.BeginningBalance = model.CarryOver;
            balance.AdjustedBalance = model.CarryOver;
            balance.PropertyCode = model.PropertyCode;
            balance.Month = month;
            balance.Year = year;
        }
    }

    public class StatementCarryOverModel
    {
        public int OwnerStatementId { get; set; }

        public string PayoutMethodName { get; set; }

        public string PropertyCode { get; set; }

        public double Balance { get; set; }

        public double TotalBalance { get; set; }

        public double TotalPayment { get; set; }

        public double CarryOver { get; set; }
    }

    public class GroupedCarryOverModel
    {
        public string PayoutMethodName { get; set; }

        public double CarryOverAmount { get; set; }

        public List<StatementCarryOverModel> Balances { get; set; }
    }
}
