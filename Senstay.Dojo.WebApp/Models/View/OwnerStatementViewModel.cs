using Senstay.Dojo.Data.Providers;
using System;
using System.Collections.Generic;

namespace Senstay.Dojo.Models.View
{
    public class OwnerStatementViewModel
    {
        public OwnerStatementViewModel(DojoDbContext context = null)
        {
            StatementMonth = DateTime.Now; // default to current month
            if (context!= null) OwnerName = ClaimProvider.GetFriendlyName(context);

            ReservationDetails = new List<ReservationStatement>();
            AdvancePaymentDetails = new List<AdvancePaymentStatement>();
            UnitExpenseDetails = new List<UnitExpenseStatement>();
        }

        // banner model
        public DateTime StatementMonth { get; set; }
        public string OwnerName { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameWithProduct { get; set; }
        public bool IsFinalized { get; set; } = false;
        public string ApprovalSummary { get; set; } = string.Empty;

        // footer model
        public bool IsProductRS { get; set; } = false;

        // statement model
        public int NightsBooked { get; set; } = 0;
        public int ReservationCount { get; set; } = 0;
        public int ResolutionCount { get; set; } = 0;
        public double BeginBalance { get; set; } = 0;
        public double TotalRevenue { get; set; } = 0;
        public double TaxCollected { get; set; } = 0;
        public double CleaningFees { get; set; } = 0;
        public double ManagementFees { get; set; } = 0;
        public string ManagementFeePercentage { get; set; } = string.Empty;
        public double CityTaxRate { get; set; } = 0;
        public double DamageWaiver { get; set; } = 0;
        public double Payout { get; set; } = 0;
        public double EndingBalance { get; set; } = 0;
        public double ReservationsTotal { get; set; } = 0;
        public double ResolutionsTotal { get; set; } = 0;
        public double UnitExpensesTotal { get; set; } = 0;
        public double AdvancementPaymentsTotal { get; set; } = 0;

        public string StatementNotes { get; set; } = "";

        public bool IsPrint { get; set; } = false;

        public bool IsModified { get; set; } = false;

        public bool IsEditFreezed { get; set; } = false;

        // reservation model
        public List<ReservationStatement> ReservationDetails { get; set; }

        // resolution model
        public List<ResolutionStatement> ResolutionDetails { get; set; }

        // advance payment model
        public List<AdvancePaymentStatement> AdvancePaymentDetails { get; set; }

        // unit expenses model
        public List<UnitExpenseStatement> UnitExpenseDetails { get; set; }
    }

    public class ReservationStatement
    {
        public string Type { get; set; }
        public string Guest { get; set; }
        public DateTime Arrival { get; set; }
        public DateTime Departure { get; set; }
        public int Nights { get; set; }
        public float TotalRevenue { get; set; }
        public string ApprovedNote { get; set; }
        public string Channel { get; set; }
        public double? TaxRate { get; set; }
        public double? DamageWaiver { get; set; }
    }

    public class ResolutionStatement
    {
        public string Type { get; set; }
        public string Guest { get; set; }
        public DateTime Arrival { get; set; }
        public DateTime Departure { get; set; }
        public int Nights { get; set; }
        public float TotalRevenue { get; set; }
        public string ApprovedNote { get; set; }
    }

    public class AdvancePaymentStatement
    {
        public string Guest { get; set; }
        public DateTime PaymentDate { get; set; }
        public float Amount { get; set; }
    }

    public class UnitExpenseStatement
    {
        public string Category { get; set; }
        public float Amount { get; set; }
    }
}