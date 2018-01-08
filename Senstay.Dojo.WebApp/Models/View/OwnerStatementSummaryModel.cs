using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using Senstay.Dojo.Data.Providers;

namespace Senstay.Dojo.Models.View
{
    public class OwnerStatementSummaryModel
    {
        public OwnerStatementSummaryModel(DojoDbContext context = null)
        {
            StatementMonth = DateTime.Now; // default to current month
            if (context != null) OwnerName = ClaimProvider.GetFriendlyName(context);

            ItemTotal = new OwnerStatementSummaryItem();
            SummaryItems = new List<OwnerStatementSummaryItem>();
        }

        // banner model
        public DateTime StatementMonth { get; set; }
        public string OwnerName { get; set; } = "";
        public double TotalPayout { get; set; } = 0;
        public double PaidPayout { get; set; } = 0;

        public string SummaryNotes { get; set; } = "";

        public bool IsPrint { get; set; } = false;
        public DateTime Month { get; set; }
        public string PayoutMethod { get; set; }

        public bool IsModified { get; set; } = false;

        public bool IsEditFreezed { get; set; } = false;

        public OwnerStatementSummaryItem ItemTotal { get; set; }

        public List<OwnerStatementSummaryItem> SummaryItems { get; set; }

        public OwnerStatementSummaryItem SumTotal(List<OwnerStatementSummaryItem> summaryItems)
        {
            OwnerStatementSummaryItem itemTotal = new OwnerStatementSummaryItem();
            foreach (var item in summaryItems)
            {
                itemTotal.PropertyID = "TOTALS";
                itemTotal.Address = "TOTALS";
                itemTotal.AdvancePayments += Math.Round(item.AdvancePayments, 2);
                itemTotal.BeginBalance += Math.Round(item.BeginBalance, 2);
                itemTotal.CleaningFees += Math.Round(item.CleaningFees, 2);
                itemTotal.EndingBalance += Math.Round(item.EndingBalance, 2);
                itemTotal.ManagementFees += Math.Round(item.ManagementFees, 2);
                itemTotal.TaxCollected += Math.Round(item.TaxCollected, 2);
                itemTotal.TotalRevenue += Math.Round(item.TotalRevenue, 2);
                itemTotal.UnitExpenseItems += Math.Round(item.UnitExpenseItems, 2);
            }
            return itemTotal;
        }

        public void SetSumTotal()
        {
            ItemTotal = SumTotal(SummaryItems);
            TotalPayout = ItemTotal.EndingBalance;
        }

        public void SetSumTotal(OwnerStatement summaryRow)
        {
            ItemTotal.PropertyID = "TOTALS";
            ItemTotal.Address = "TOTALS";
            ItemTotal.AdvancePayments = summaryRow.AdvancePayments;
            ItemTotal.BeginBalance = summaryRow.BeginBalance;
            ItemTotal.CleaningFees = summaryRow.CleaningFees;
            ItemTotal.ManagementFees = summaryRow.ManagementFees;
            ItemTotal.TaxCollected = summaryRow.TaxCollected;
            ItemTotal.TotalRevenue = summaryRow.TotalRevenue;
            ItemTotal.UnitExpenseItems += Math.Round(summaryRow.UnitExpenseItems, 2);
            ItemTotal.EndingBalance = summaryRow.Balance;
            TotalPayout = ItemTotal.EndingBalance;
        }

        public bool IsFinalized { get; set; } = false;

        public bool IsRebalanced { get; set; } = true;
    }

    public class OwnerStatementSummaryItem
    {
        public string PropertyID { get; set; }
        public string Address { get; set; }
        public string EntityName { get; set; }
        public string PayoutMethod { get; set; }
        public double BeginBalance { get; set; } = 0;
        public double TotalRevenue { get; set; } = 0;
        public double TaxCollected { get; set; } = 0;
        public double CleaningFees { get; set; } = 0;
        public double ManagementFees { get; set; } = 0;
        public double UnitExpenseItems { get; set; } = 0;
        public double AdvancePayments { get; set; } = 0;
        public double Payout { get; set; } = 0;
        public double EndingBalance { get; set; } = 0;
    }

    public class OwnerSummaryRebalanceModel
    {
        [Required]
        public string FromProperty { get; set; }

        [Required]
        public string ToProperty { get; set; }

        [Required]
        public double RebalanceAmount { get; set; }

        public DateTime RebalanceDate { get; set; }

        public string PayoutMethod { get; set; }

        public int PayoutMonth { get; set; }

        public int PayoutYear { get; set; }

        public List<SelectListItem> SummaryProperties { get; set; }

        public List<PropertyBalanceModel> PropertyBalances { get; set; }
    }

    public class PropertyBalanceModel
    {
        public string PropertyCode { get; set; }

        public double PropertyBalance { get; set; }
    }

    public class RebalanceTransactionModel
    {
        [Required]
        public string FromProperty { get; set; }

        public string ToProperty { get; set; }

        public string FromAddress { get; set; }

        public string ToAddress { get; set; }

        public double TransactionAmount { get; set; }
    }

    public class SummaryRebalanceTransactionModel
    {
        public string PayoutMethod { get; set; }

        public int PayoutMonth { get; set; }

        public int PayoutYear { get; set; }

        public List<RebalanceTransactionModel> Transactions { get; set; }
    }

    public class OverpaymentRedistributionModel
    {
        public string PayoutMethodName { get; set; }

        public double RedistributionAmount { get; set; }

        public DateTime RedistributionDate { get; set; }

        public string PayoutMethod { get; set; }

        public int OverpaymentMonth { get; set; }

        public int PayOverpaymentYear { get; set; }

        public List<SelectListItem> SummaryProperties { get; set; }

        public List<PropertyBalanceModel> PropertyBalances { get; set; }
    }
}