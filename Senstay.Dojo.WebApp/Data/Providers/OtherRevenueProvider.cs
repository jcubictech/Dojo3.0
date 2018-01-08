using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Data.Entity;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.Grid;

namespace Senstay.Dojo.Data.Providers
{
    public class OtherRevenueProvider : CrudProviderBase<OtherRevenueModel>
    {
        private readonly DojoDbContext _context;

        public OtherRevenueProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        #region custom methods for owner payout revenue

        public List<OtherRevenueModel> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.PropertyCode).ThenBy(x => x.OtherRevenueDate).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool Exist(OtherRevenueModel expense)
        {
            return _context.OtherRevenues.FirstOrDefault(x => ((x.OtherRevenueId == expense.OtherRevenueId && expense.OtherRevenueId != 0) ||
                                                               (x.OtherRevenueDate.Value.Date == expense.OtherRevenueDate.Value.Date &&
                                                                x.OtherRevenueAmount == expense.OtherRevenueAmount &&
                                                                x.PropertyCode == expense.PropertyCode)))
                                         != null;
        }

        public int GetKey(OtherRevenueModel expense)
        {
            var entity = _context.OtherRevenues.Where(x => ((x.OtherRevenueId == expense.OtherRevenueId && expense.OtherRevenueId != 0) ||
                                                            (DbFunctions.TruncateTime(x.OtherRevenueDate.Value) == DbFunctions.TruncateTime(expense.OtherRevenueDate.Value) &&
                                                             x.OtherRevenueAmount == expense.OtherRevenueAmount &&
                                                             x.OtherRevenueDescription == expense.OtherRevenueDescription &&
                                                             x.PropertyCode == expense.PropertyCode)))
                                               .OrderByDescending(x => x.OtherRevenueId)
                                               .FirstOrDefault();

            return entity != null ? entity.OtherRevenueId : 0;
        }

        public List<OtherRevenueModel> Retrieve(DateTime month, string propertyCode)
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

                var data = _context.Database.SqlQuery<OtherRevenueModel>("RetrieveOtherRevenues @StartDate, @EndDate, @PropertyCode", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public RevenueApprovalStatus? MoveWorkflow(int otherRevenueId, RevenueApprovalStatus state)
        {
            try
            {
                var dataProvider = new OtherRevenueTableProvider(_context);
                var entity = dataProvider.Retrieve(otherRevenueId);
                var nextState = NextState(state);
                if (entity != null && nextState != null)
                {
                    entity.ApprovalStatus = state;
                    SetWorkflowSignature(ref entity, state);
                    dataProvider.Update(otherRevenueId, entity);
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

        public RevenueApprovalStatus? BacktrackWorkflow(int reservationId, RevenueApprovalStatus state)
        {
            try
            {
                var dataProvider = new OtherRevenueTableProvider(_context);
                var entity = dataProvider.Retrieve(reservationId);
                var prevState = PrevState(state);
                if (entity != null && prevState != null)
                {
                    entity.ApprovalStatus = prevState.Value;
                    RetrackWorkflowSignature(ref entity, state);
                    dataProvider.Update(reservationId, entity);
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

        public bool SetFieldStatus(int otherRevenueId, string fieldname, bool included)
        {
            try
            {
                var provider = new OtherRevenueTableProvider(_context);
                var entity = provider.Retrieve(otherRevenueId);
                if (entity != null)
                {
                    if (fieldname == "IncludeOnStatement")
                    {
                        entity.IncludeOnStatement = included;
                        provider.Update(entity.OtherRevenueId, entity);
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

        #region override to delegate CRUD operations to databae's Other Expense entity

        public override OtherRevenueModel Retrieve(int id)
        {
            try
            {
                if (id == 0) return new OtherRevenueModel();

                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@OtherRevenueId", SqlDbType.Int);
                sqlParams[0].Value = id;

                return _context.Database.SqlQuery<OtherRevenueModel>("RetrieveOtherRevenueById @OtherRevenueId", sqlParams).FirstOrDefault();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public override void Create(OtherRevenueModel model)
        {
            try
            {
                OtherRevenue entity = new OtherRevenue();
                MapData(model, ref entity);
                var dataProvider = new OtherRevenueTableProvider(_context);
                dataProvider.Create(entity);
            }
            catch
            {
                throw;
            }
        }

        public override void Update(int id, OtherRevenueModel model)
        {
            try
            {
                var dataProvider = new OtherRevenueTableProvider(_context);
                var entity = dataProvider.Retrieve(model.OtherRevenueId);
                MapData(model, ref entity);
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
                var dataProvider = new OtherRevenueTableProvider(_context);
                var entity = dataProvider.Retrieve(id);
                entity.IsDeleted = true;
                dataProvider.Update(id, entity);
            }
            catch
            {
                throw;
            }
        }

        private void MapData(OtherRevenueModel from, ref OtherRevenue to)
        {
            to.OtherRevenueAmount = from.OtherRevenueAmount;
            to.OtherRevenueDate = ConversionHelper.EnsureUtcDate(from.OtherRevenueDate);
            to.OtherRevenueDescription = from.OtherRevenueDescription;
            to.OtherRevenueId = from.OtherRevenueId;
            to.PropertyCode = from.PropertyCode;
            to.IncludeOnStatement = from.IncludeOnStatement;
            to.ApprovedNote = from.ApprovedNote;
        }
        #endregion

        #region not implemented interfaces

        public override OtherRevenueModel Retrieve(string id)
        {
            throw new NotImplementedException();
        }

        public override IQueryable<OtherRevenueModel> GetAll()
        {
            throw new NotImplementedException();
        }

        public override void Update(string id, OtherRevenueModel model)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override void Delete(OtherRevenueModel model)
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

        private void SetWorkflowSignature(ref OtherRevenue entity, RevenueApprovalStatus state)
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

        private void RetrackWorkflowSignature(ref OtherRevenue entity, RevenueApprovalStatus state)
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

        #endregion
    }

}
