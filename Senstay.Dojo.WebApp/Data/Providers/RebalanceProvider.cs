using System;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Data.Providers
{
    public class RebalanceProvider
    {
        private readonly DojoDbContext _dbContext;
        private const string DebitGroupCategory = "Debit To Property";
        private const string CreditGroupCategory = "Credit From Property";
        private const string DebitCategoryPrefix = "Rebalance: Debit To ";
        private const string CreditCategoryPrefix = "Rebalance: Credit From ";

        public RebalanceProvider(DojoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void RebalanceSummaryWithNoGroup(SummaryRebalanceTransactionModel model)
        {
            try
            {
                // for each transaction, 
                // 1. a debit expense object is created for FromProperty to debit the amount to ToProperty.
                // 2. a credit expense object is created for ToProperty to credit the amount from FromProperty.
                // 3. the FromProperty's owner statement is regenerated to account for the debited amount.
                // 4. the ToProperty's owner statement is regenerated to account for the credited amount.
                // 5. Make debit and credit expense item an expense group.

                int month = model.PayoutMonth;
                int year = model.PayoutYear;
                DateTime transactionDate = ConversionHelper.EnsureUtcDate(new DateTime(year, month, DateTime.DaysInMonth(year, month)));

                foreach (RebalanceTransactionModel transaction in model.Transactions)
                {
                    var debit = -transaction.TransactionAmount;
                    var credit = transaction.TransactionAmount;

                    CreateExpenseForTransaction(transaction.FromProperty, debit, transactionDate, MakeCategory(DebitCategoryPrefix, transaction.ToAddress));
                    CreateExpenseForTransaction(transaction.ToProperty, credit, transactionDate, MakeCategory(CreditCategoryPrefix, transaction.FromAddress));

                    UpdateOwnerStatement(transaction.FromProperty, month, year, debit); // debit
                    UpdateOwnerStatement(transaction.ToProperty, month, year, credit); // credit
                }

                if (model.Transactions.Count > 0)
                {
                    CommitChanges();
                    CreateExpenseGroupsForTransactions(model, transactionDate);
                }
            }
            catch
            {
                throw;
            }
        }

        private bool CreateExpenseForTransaction(string propertyCode, double amount, DateTime transactionDate, string category)
        {
            var provider = new ExpenseProvider(_dbContext);
            Expense expense = provider.GetRebalanceExpenseByKey(propertyCode, category, transactionDate);

            if (expense == null)
            {
                amount = -Math.Round(amount, 2);

                expense = new Expense
                {
                    ExpenseDate = transactionDate,
                    Category = category,
                    ExpenseAmount = (float)amount,
                    PropertyCode = propertyCode,
                    ParentId = 0,
                    ApprovalStatus = RevenueApprovalStatus.Approved,
                    ApprovedNote = "Rebalance",
                    ConfirmationCode = string.Empty,
                };

                provider.Create(expense);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CreateExpenseGroupsForTransactions(SummaryRebalanceTransactionModel model, DateTime transactionDate)
        {
            var provider = new ExpenseProvider(_dbContext);
            foreach (RebalanceTransactionModel transaction in model.Transactions)
            {
                // From property
                var debitCategory = MakeCategory(DebitCategoryPrefix, transaction.ToAddress);
                Expense debitExpense = provider.GetRebalanceExpenseByKey(transaction.FromProperty, debitCategory, transactionDate);
                if (debitExpense != null)
                {
                    debitExpense.ParentId = debitExpense.ExpenseId;
                    provider.Update(debitExpense.ExpenseId, debitExpense);
                }

                // To Property
                var creditCategory = MakeCategory(CreditCategoryPrefix, transaction.FromAddress);
                var creditExpense = provider.GetRebalanceExpenseByKey(transaction.ToProperty, creditCategory, transactionDate);
                if (creditExpense != null)
                {
                    creditExpense.ParentId = creditExpense.ExpenseId;
                    provider.Update(creditExpense.ExpenseId, creditExpense);
                }
            }
            CommitChanges();
        }

        private void UpdateOwnerStatement(string propertyCode, int month, int year, double amount)
        {
            var provider = new OwnerStatementProvider(_dbContext);
            var statement = provider.GetOwnerStatement( propertyCode, month, year);
            if (statement != null)
            {
                statement.UnitExpenseItems += amount;
                statement.Balance += amount;
                provider.Update(statement.OwnerStatementId, statement);
            }
        }

        private void CommitChanges()
        {
            _dbContext.SaveChanges();
        }

        private string MakeCategory(string prefix, string address)
        {
            return prefix + address;
        }

        #region Create rebalance expenses with grouping - No Use

        public void RebalanceSummaryWithGroups(SummaryRebalanceTransactionModel model)
        {
            const string DebitCategoryPrefix = "Rebalance: Debit To ";
            const string CreditCategoryPrefix = "Rebalance: Credit From ";
            try
            {
                // for each transaction, 
                // 1. Create the expense group for debit and credit group first.
                // 2. a expense object is created for FromProperty to debit the amount to ToProperty.
                // 3. a expense object is created for ToProperty to credit the amount from FromProperty.
                // 4. the FromProperty's owner statement is regenerated to account for the debited amount.
                // 5. the ToProperty's owner statement is regenerated to account for the credited amount.

                int month = model.PayoutMonth;
                int year = model.PayoutYear;
                DateTime transactionDate = ConversionHelper.EnsureUtcDate(new DateTime(year, month, DateTime.DaysInMonth(year, month)));

                CreateExpenseGroupAsNeeded(model, transactionDate);

                foreach (RebalanceTransactionModel transaction in model.Transactions)
                {
                    var debit = -transaction.TransactionAmount;
                    var credit = transaction.TransactionAmount;

                    CreateExpenseForTransaction(transaction.FromProperty, debit, transactionDate, DebitGroupCategory, DebitCategoryPrefix + transaction.ToProperty);
                    CreateExpenseForTransaction(transaction.ToProperty, credit, transactionDate, CreditGroupCategory, CreditCategoryPrefix + transaction.FromProperty);

                    UpdateOwnerStatement(transaction.FromProperty, month, year, debit); // debit
                    UpdateOwnerStatement(transaction.ToProperty, month, year, credit); // credit
                }

                if (model.Transactions.Count > 0) CommitChanges();
            }
            catch
            {
                throw;
            }
        }

        private void CreateExpenseGroupAsNeeded(SummaryRebalanceTransactionModel model, DateTime transactionDate)
        {
            const string RebalanceApprovalNote = "Rebalance";

            var provider = new ExpenseProvider(_dbContext);
            foreach (RebalanceTransactionModel t in model.Transactions)
            {
                int id = provider.GetGroupByKey(t.FromProperty, DebitGroupCategory, transactionDate);
                if (id == 0)
                {
                    var expense = new Expense
                    {
                        ExpenseDate = transactionDate,
                        Category = DebitGroupCategory,
                        ExpenseAmount = 0,
                        PropertyCode = t.FromProperty,
                        ApprovalStatus = RevenueApprovalStatus.Approved,
                        ApprovedNote = RebalanceApprovalNote,
                        ConfirmationCode = string.Empty,
                    };
                    provider.CreateExpenseGroup(expense);
                }

                id = provider.GetGroupByKey(t.ToProperty, CreditGroupCategory, transactionDate);
                if (id == 0)
                {
                    var expense = new Expense
                    {
                        ExpenseDate = transactionDate,
                        Category = CreditGroupCategory,
                        ExpenseAmount = 0,
                        PropertyCode = t.ToProperty,
                        ApprovalStatus = RevenueApprovalStatus.Approved,
                        ApprovedNote = RebalanceApprovalNote,
                        ConfirmationCode = string.Empty,
                    };
                    provider.CreateExpenseGroup(expense);
                }
            }
        }

        private bool CreateExpenseForTransaction(string propertyCode, double amount, DateTime transactionDate, string groupCategory, string category)
        {
            var provider = new ExpenseProvider(_dbContext);
            int groupId = provider.GetGroupByKey(propertyCode, groupCategory, transactionDate);

            if (groupId > 0)
            {
                amount = -Math.Round(amount, 2);

                var expense = new Expense
                {
                    ExpenseDate = transactionDate,
                    Category = category,
                    ExpenseAmount = (float)amount,
                    PropertyCode = propertyCode,
                    ParentId = groupId,
                    ApprovalStatus = RevenueApprovalStatus.Approved,
                    ApprovedNote = string.Empty,
                    ConfirmationCode = string.Empty,
                };

                provider.Create(expense);

                var entity = provider.Retrieve(groupId);
                if (entity != null)
                {
                    entity.ExpenseAmount += (float)amount;
                    provider.Update(entity.ExpenseId, entity);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}
