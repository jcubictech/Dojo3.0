using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Data.Entity;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Models.HelperClass;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.Grid;

namespace Senstay.Dojo.Data.Providers
{
    public class ExpenseRevenueProvider : CrudProviderBase<ExpenseRevenueModel>
    {
        private readonly DojoDbContext _context;

        public ExpenseRevenueProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        #region custom methods for expense revenue

        public List<ExpenseRevenueModel> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.Category).ThenBy(x => x.ExpenseAmount).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool Exist(ExpenseRevenueModel expense)
        {
            return _context.Expenses.FirstOrDefault(x => ((x.ExpenseId == expense.ExpenseId && expense.ExpenseId != 0) ||
                                                          (x.ExpenseAmount == expense.ExpenseAmount &&
                                                           x.Category == expense.Category &&
                                                           DbFunctions.TruncateTime(x.ExpenseDate) == DbFunctions.TruncateTime(expense.ExpenseDate) &&
                                                           x.IsDeleted == false &&
                                                           x.ReservationId == expense.ReservationId)))
                                         != null;
        }

        public int GetKey(ExpenseRevenueModel expense)
        {
            var entity = _context.Expenses.Where(x => ((x.ExpenseId == expense.ExpenseId && expense.ExpenseId != 0) ||
                                                       (x.ExpenseAmount == expense.ExpenseAmount &&
                                                        x.Category == expense.Category &&
                                                        DbFunctions.TruncateTime(x.ExpenseDate) == DbFunctions.TruncateTime(expense.ExpenseDate) &&
                                                        x.IsDeleted == false &&
                                                        x.ReservationId == expense.ReservationId)))
                                          .OrderByDescending(x => x.Category)
                                          .ThenBy(x => x.ExpenseAmount)
                                          .FirstOrDefault();

            return entity != null ? entity.ExpenseId : 0;
        }

        public object Retrieve(DateTime month, string propertyCode)
        {
            try
            {
                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;

                var expenses = _context.Database.SqlQuery<ExpenseRevenueModel>("RetrieveExpensesRevenue @StartDate, @EndDate, @PropertyCode", sqlParams).ToList();

                List<ExpenseRevenueModel> expenseRevenue = new List<ExpenseRevenueModel>();
                var singleExpenses = expenses.Where(x => x.ExpenseId == x.ParentId).OrderBy(x => x.ExpenseId).ToList();
                expenseRevenue.AddRange(singleExpenses);

                var childExpenses = expenses.Where(x => x.ExpenseId != x.ParentId)
                                            .OrderBy(x => x.ParentId).ThenBy(x => x.ExpenseId).ToList();

                int lastParentId = -1;
                ExpenseRevenueModel parentExpense = null;
                foreach (var expense in childExpenses)
                {
                    if (lastParentId != expense.ParentId)
                    {
                        parentExpense = singleExpenses.Find(x => x.ExpenseId == expense.ParentId);
                        lastParentId = expense.ParentId;
                    }

                    if (parentExpense != null)
                    {
                        if (parentExpense.Children == null) parentExpense.Children = new List<ExpenseRevenueModel>();
                        parentExpense.Children.Add(expense);
                    }
                }

                return expenseRevenue;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<string> GetConfirmationCodeList(DateTime month, string propertyCode)
        {
            DateTime startDate = new DateTime(month.Year, month.Month, 1);
            DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
            return _context.Reservations.Where(x => x.PropertyCode == propertyCode && x.ReservationId > 0 && 
                                                    x.ConfirmationCode != null && x.ConfirmationCode != "" &&
                                                    DbFunctions.TruncateTime(x.TransactionDate.Value) >= DbFunctions.TruncateTime(startDate) &&
                                                    DbFunctions.TruncateTime(x.TransactionDate.Value) <= DbFunctions.TruncateTime(endDate))
                                        .OrderBy(x => x.ConfirmationCode)
                                        .Select(x => x.ConfirmationCode)
                                        .ToList();
        }

        public List<CombinedExpenseTreeModel> GetCombinedExpenseTree(DateTime month, string propertyCode)
        {
            DateTime startDate = new DateTime(month.Year, month.Month, 1);
            DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));

            SqlParameter[] sqlParams = new SqlParameter[3];
            sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
            sqlParams[0].Value = startDate;
            sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
            sqlParams[1].Value = endDate;
            sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
            sqlParams[2].Value = propertyCode;

            var expenses = _context.Database.SqlQuery<CombinedExpenseModel>("RetrieveCombinedExpensesRevenue @StartDate, @EndDate, @PropertyCode", sqlParams).ToList();

            CombinedExpenseTreeModel currentTreeNode = null;
            CombinedExpenseTreeModel expenseHome = null;

            var singleExpenses = expenses.Where(x => x.ExpenseId == x.ParentId).ToList();
            foreach (var e in singleExpenses)
            {
                currentTreeNode = new CombinedExpenseTreeModel { text = e.TreeNode, items = null };
                if (expenseHome == null)
                {
                    expenseHome = currentTreeNode;
                    expenseHome.items = new List<CombinedExpenseTreeModel>();
                }
                else
                {
                    expenseHome.items.Add(currentTreeNode);
                    currentTreeNode = null;
                }
            }

            int lastParentId = -1;
            CombinedExpenseTreeModel parentTreeNode = null;
            var childExpenses = expenses.Where(x => x.ExpenseId != x.ParentId)
                                        .OrderBy(x => x.ParentId).ThenBy(x => x.ExpenseId).ToList();
            foreach (var e in childExpenses)
            {
                currentTreeNode = new CombinedExpenseTreeModel { text = e.TreeNode, items = null };
                if (lastParentId != e.ParentId)
                {
                    var parentId = "\"expense-tree-id-" + e.ParentId.ToString() + "\"";
                    parentTreeNode = expenseHome.items.Where(x => x.text.IndexOf(parentId) > 0).FirstOrDefault();
                    lastParentId = e.ParentId;
                }

                if (parentTreeNode != null)
                {
                    if (parentTreeNode.items == null) parentTreeNode.items = new List<CombinedExpenseTreeModel>();
                    parentTreeNode.items.Add(currentTreeNode);
                }
            }

            return new List<CombinedExpenseTreeModel> { expenseHome };
        }

        public int UpdateCombinedExpense(int sourceId, int targetId)
        {
            try
            {
                var dataProvider = new ExpenseProvider(_context);
                var entity = _context.Expenses.Where(x => x.ExpenseId == sourceId).FirstOrDefault();
                int originalParentId = entity.ParentId;
                float originalAmount = entity.ExpenseAmount;
                entity.ParentId = targetId;
                dataProvider.Update(sourceId, entity);
                dataProvider.Commit();

                // update original expense total
                if (originalParentId != sourceId)
                {
                    var totalAmount = _context.Expenses.Where(x => x.ParentId == originalParentId && x.ExpenseId != originalParentId)
                                                       .Select(x => x.ExpenseAmount)
                                                       .Sum();
                    var sourceEntity = _context.Expenses.Where(x => x.ExpenseId == originalParentId).FirstOrDefault();
                    if (sourceEntity != null)
                    {
                        sourceEntity.ExpenseAmount = totalAmount;
                        dataProvider.Update(originalParentId, sourceEntity);
                    }
                }

                if (sourceId != targetId)
                {
                    var totalAmount = _context.Expenses.Where(x => x.ParentId == targetId && x.ExpenseId != targetId)
                                                       .Select(x => x.ExpenseAmount)
                                                       .Sum();
                    var targetEntity = _context.Expenses.Where(x => x.ExpenseId == targetId).FirstOrDefault();
                    if (targetEntity != null)
                    {
                        targetEntity.ExpenseAmount = totalAmount;
                        dataProvider.Update(targetId, targetEntity);
                    }
                }

                dataProvider.Commit();

                return sourceId;
            }
            catch
            {
                return -1;
            }
        }

        public bool SetFieldStatus(int expenseId, string fieldname, bool included)
        {
            try
            {
                var provider = new ExpenseProvider(_context);
                var entity = provider.Retrieve(expenseId);
                if (entity != null)
                {
                    if (fieldname == "IncludeOnStatement")
                    {
                        entity.IncludeOnStatement = included;
                        provider.Update(entity.ExpenseId, entity);
                        provider.Commit();
                        return true;
                    }
                    else // not supported field; just ignore for now...
                        return true;
                }
            }
            catch
            {
                throw;
            }
            return false;
        }

        #endregion

        #region Approval workflow

        public RevenueApprovalStatus? MoveWorkflowAll(DateTime month, string propertyCode, RevenueApprovalStatus state, int direction)
        {
            try
            {
                // Stored procedure for bulk update is a lot faster
                var user = ClaimProvider.GetFriendlyName(_context);
                var nextState = direction >= 0 ? NextState(state) : PrevState(state);

                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));

                SqlParameter[] sqlParams = new SqlParameter[5];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;
                sqlParams[3] = new SqlParameter("@State", SqlDbType.Int);
                sqlParams[3].Value = direction >= 0 ? state : nextState;
                sqlParams[4] = new SqlParameter("@User", SqlDbType.NVarChar);
                sqlParams[4].Value = user;
                var result = _context.Database.SqlQuery<SqlResult>("UpdateAllExpenseWorkflowStates @StartDate, @EndDate, @PropertyCode, @State, @User", sqlParams).FirstOrDefault();

                return result.Count > 0 ? nextState : null;
            }
            catch
            {
                throw;
            }
        }

        // Replaced by stored precedure version MoveWorkflowAll above due to performance issue using Entity Framework for bulk db operation
        public RevenueApprovalStatus? MoveWorkflowAll(DateTime month, string propertyCode, RevenueApprovalStatus state)
        {
            try
            {
                var provider = new ExpenseProvider(_context);

                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                var expenses = provider.RetrieveCombinedExpenses(startDate, endDate, propertyCode);
                var nextState = NextState(state);
                if (expenses != null && nextState != null)
                {
                    foreach (var expense in expenses)
                    {
                        if (expense.ApprovalStatus < state)
                        {
                            expense.ApprovalStatus = state;
                            SetWorkflowSignature(expense, state);
                            provider.Update(expense.ExpenseId, expense);
                        }
                    }
                    provider.Commit();
                    return nextState;
                }
            }
            catch
            {
                throw;
            }
            return null;
        }

        // Replaced by stored precedure version MoveWorkflowAll above due to performance issue using Entity Framework for bulk db operation
        public RevenueApprovalStatus? BacktrackWorkflowAll(DateTime month, string propertyCode, RevenueApprovalStatus state)
        {
            try
            {
                var provider = new ExpenseProvider(_context);

                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                var expenses = provider.RetrieveCombinedExpenses(startDate, endDate, propertyCode);
                var prevState = PrevState(state);
                if (expenses != null && prevState != null)
                {
                    foreach (var expense in expenses)
                    {
                        if (expense.ApprovalStatus >= prevState)
                        {
                            expense.ApprovalStatus = prevState.Value;
                            RetrackWorkflowSignature(expense, state);
                            provider.Update(expense.ExpenseId, expense);
                        }
                    }
                    provider.Commit();
                    return prevState;
                }
            }
            catch
            {
                throw;
            }
            return null;
        }

        public RevenueApprovalStatus? MoveWorkflow(int expenseId, RevenueApprovalStatus state)
        {
            try
            {
                var dataProvider = new ExpenseProvider(_context);
                var entity = dataProvider.Retrieve(expenseId);
                var nextState = NextState(state);
                if (entity != null && nextState != null)
                {
                    entity.ApprovalStatus = state;
                    SetWorkflowSignature(entity, state);
                    dataProvider.Update(expenseId, entity);
                    dataProvider.Commit();
                    return nextState;
                }
            }
            catch
            {
                throw;
            }
            return null;
        }

        public RevenueApprovalStatus? BacktrackWorkflow(int expenseId, RevenueApprovalStatus state)
        {
            try
            {
                var dataProvider = new ExpenseProvider(_context);
                var entity = dataProvider.Retrieve(expenseId);
                var prevState = PrevState(state);
                if (entity != null && prevState != null)
                {
                    entity.ApprovalStatus = prevState.Value;
                    RetrackWorkflowSignature(entity, state);
                    dataProvider.Update(expenseId, entity);
                    dataProvider.Commit();
                    return prevState;
                }
            }
            catch
            {
                throw;
            }
            return null;
        }

        #endregion

        #region override to delegate CRUD operations to databae's Expense entity

        public override ExpenseRevenueModel Retrieve(int id)
        {
            try
            {
                if (id == 0) return new ExpenseRevenueModel();

                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@ExpenseId", SqlDbType.Int);
                sqlParams[0].Value = id;

                return _context.Database.SqlQuery<ExpenseRevenueModel>("RetrieveExpenseRevenueById @ExpenseId", sqlParams).FirstOrDefault();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public override void Create(ExpenseRevenueModel model)
        {
            try
            {
                var entity = new Expense();
                MapData(model, entity);
                var dataProvider = new ExpenseProvider(_context);
                dataProvider.Create(entity);
            }
            catch
            {
                throw;
            }
        }

        public override void Update(int id, ExpenseRevenueModel model)
        {
            try
            {
                var dataProvider = new ExpenseProvider(_context);
                var entity = dataProvider.Retrieve(model.ExpenseId);
                MapData(model, entity);
                dataProvider.Update(id, entity);
            }
            catch
            {
                throw;
            }
        }

        public override void Delete(int id)
        {
            try
            {
                // entity deletion does not physically delete the record; it marks [IsDeletd] = true
                var dataProvider = new ExpenseProvider(_context);
                var entity = dataProvider.Retrieve(id);
                entity.IsDeleted = true;
                dataProvider.Update(id, entity);
            }
            catch
            {
                throw;
            }
        }

        private void MapData(ExpenseRevenueModel from, Expense to)
        {
            from.ReservationId = TryGetReservationId(from);
            to.ExpenseId = from.ExpenseId;
            to.ParentId = from.ParentId;
            to.ExpenseDate = ConversionHelper.EnsureUtcDate(from.ExpenseDate);
            to.ConfirmationCode = from.ConfirmationCode;
            to.Category = from.Category;
            to.ExpenseAmount = from.ExpenseAmount;
            to.PropertyCode = from.PropertyCode;
            to.ReservationId = from.ReservationId;
            to.IncludeOnStatement = from.IncludeOnStatement;
            to.ApprovedNote = from.ApprovedNote;
        }
        #endregion

        #region not implemented interfaces

        public override ExpenseRevenueModel Retrieve(string id)
        {
            throw new NotImplementedException();
        }

        public override IQueryable<ExpenseRevenueModel> GetAll()
        {
            throw new NotImplementedException();
        }

        public override void Update(string id, ExpenseRevenueModel model)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override void Delete(ExpenseRevenueModel model)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region private methods
    
        private RevenueApprovalStatus? NextState(RevenueApprovalStatus state)
        {
            var nextState = ((int)state + 1);
            if (nextState > (int)RevenueApprovalStatus.Closed)
                return null;
            else
                return (RevenueApprovalStatus)nextState;
        }

        private RevenueApprovalStatus? PrevState(RevenueApprovalStatus state)
        {
            var prevState = ((int)state - 1);
            if (prevState < (int)RevenueApprovalStatus.NotStarted)
                return null;
            else
                return (RevenueApprovalStatus)prevState;
        }

        private void SetWorkflowSignature( Expense entity, RevenueApprovalStatus state)
        {
            var userName = ClaimProvider.GetFriendlyName(_context);
            switch (state)
            {
                case RevenueApprovalStatus.Reviewed:
                    entity.ReviewedBy = userName;
                    entity.ReviewedDate = DateTime.Now.ToUniversalTime();
                    break;
                case RevenueApprovalStatus.Approved:
                    entity.ApprovedBy = userName;
                    entity.ApprovedDate = DateTime.Now.ToUniversalTime();
                    break;
            }
        }

        private void RetrackWorkflowSignature(Expense entity, RevenueApprovalStatus state)
        {
            var userName = ClaimProvider.GetFriendlyName(_context);
            switch (state)
            {
                case RevenueApprovalStatus.Reviewed:
                    entity.ReviewedBy = null;
                    entity.ReviewedDate = null;
                    break;
                case RevenueApprovalStatus.Approved:
                    entity.ApprovedBy = null;
                    entity.ApprovedDate = null;
                    break;
            }
        }

        private int TryGetReservationId(ExpenseRevenueModel model)
        {
            var one = _context.Reservations.Where(x => x.ConfirmationCode == model.ConfirmationCode &&
                                                       x.PropertyCode == model.PropertyCode)
                                        .FirstOrDefault();
            if (one != null)
                return one.ReservationId;
            else
                return 0;
        }
        #endregion
    }

    public class CombinedExpenseModel
    {
        public int ParentId { get; set; }
        public int ExpenseId { get; set; }
        public string TreeNode { get; set; }
    }

    public class CombinedExpenseTreeModel
    {
        public string text { get; set; }
        //public bool expanded { get; set; }
        //public string spriteCssClass { get; set; }

        public List<CombinedExpenseTreeModel> items { get; set; }
    }
}
