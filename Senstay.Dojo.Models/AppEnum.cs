namespace Senstay.Dojo.Models
{
    public enum SourceType
    {
        User = 1,
        Role = 2,
        UserRole = 3,
        Password = 4,
        Google = 5,
        Facebook = 6,
        Twitter = 7
    }

    public enum LookupType
    {
        Vertical = 1,
        City,
        Market,
        Channel,
        Area,
        Neighborhood,
        State,    // list of states
        Belt,     // Yellow Belt, Black Belt, White Belt
        Currency, // USD ($), Real (R$), Euro (?), Pound (?)
        YesNo,    // Yes, No
        YesNoNa,  // Yes, No, N/A
        PropertyStatus, // Acive, Inactive, Pending Onboarding
        Approval, // Yes, No, N/A, Pending
        PriceDecision,
        PaymentMethod,
        Boolean,
        AbbreviatedState,
        InquiryTeam,
        PriceApprover,
        ExpenseCategory,
        Impact,
        SenStayAccount,
        Cause,
    }

    public enum PayoutType
    {
        Payout = 1,
        Reservation = 2,
        Resolution = 3,
        OffAirbnb = 4,
        Other = 5
    }

    public enum PayoutMethodType
    {
        Checking = 1,
        Paypal = 2
    }

    public enum CurrencyType
    {
        USD = 1,
        Euro = 2,
        Pound = 3,
        Yen = 4,
        ZMB = 5
    }

    public enum RevenueApprovalStatus
    {
        NotStarted = 0,
        Reviewed = 1,
        Approved = 2,
        Finalized = 3,
        Closed = 4
    }

    public enum StatementStatus
    {
        Preliminary = 0,
        Approved = 1,
        Finalized = 2
    }

    public enum ImportFileType
    {
        Expenses = 1,
        OffAibnb = 2,
        PropertyFee = 3,
        Balance = 4,
        Steamline = 5,
        BackfillTransaction = 6,
    }

    public enum StatementReportType
    {
        Journal = 1,
        Credit = 2,
        Invoice = 3
    }

    public enum StatementReportItemType
    {
        Airbnb = 1,
        NonAirbnb = 2,
        ManagementFee = 3,
        Expense = 4,
        Tax = 5,
        Balance = 6,
    }
}