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
    public class ResolutionRevenueProvider : CrudProviderBase<ResolutionRevenueModel>
    {
        private readonly DojoDbContext _context;

        public ResolutionRevenueProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        #region custom methods for resolution revenue

        public List<ResolutionRevenueModel> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.ResolutionDate).ThenBy(x => x.ResolutionType).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool Exist(ResolutionRevenueModel model)
        {
            return _context.Resolutions.FirstOrDefault(x => ((x.ResolutionId == model.ResolutionId && model.ResolutionId != 0) ||
                                                             (x.ReviewedDate == model.ResolutionDate &&
                                                              x.ResolutionType == model.ResolutionType &&
                                                              x.ResolutionAmount == model.ResolutionAmount &&
                                                              x.ResolutionDescription == model.ResolutionDescription)))
                                        != null;
        }

        public int GetKey(ResolutionRevenueModel model)
        {
            var entity = _context.Resolutions.Where(x => x.ResolutionDate == model.ResolutionDate &&
                                                         x.ResolutionType == model.ResolutionType &&
                                                         x.ResolutionAmount == model.ResolutionAmount &&
                                                         x.ConfirmationCode == model.ConfirmationCode &&
                                                         x.ResolutionDescription == model.ResolutionDescription)
                                             .OrderByDescending(x => x.ResolutionId)
                                             .FirstOrDefault();

            return entity != null ? entity.ResolutionId : 0;
        }

        public List<ResolutionRevenueModel> Retrieve(DateTime month, string propertyCode)
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

                var resolutions = _context.Database.SqlQuery<ResolutionRevenueModel>("RetrieveResolutionRevenue @StartDate, @EndDate, @PropertyCode", sqlParams).ToList();

                // mark the resolutions that are 'orphan' - meaning that the property code and confirmation does not exist in reservation
                SqlParameter[] sqlParams2 = new SqlParameter[2];
                sqlParams2[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams2[0].Value = startDate;
                sqlParams2[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams2[1].Value = endDate;
                var orphanRevenue = _context.Database.SqlQuery<ResolutionRevenueModel>("GetOrphanResolutions @StartDate, @EndDate", sqlParams2).ToList();
                if (orphanRevenue != null && orphanRevenue.Count > 0)
                {
                    foreach(var orphan in orphanRevenue)
                    {
                        var resolution = resolutions.Where(x => x.PropertyCode == orphan.PropertyCode && x.ConfirmationCode == orphan.ConfirmationCode).FirstOrDefault();
                        if (resolution != null) resolution.CanStatementSeeIt = false;
                    }
                }

                return resolutions;
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

        public RevenueApprovalStatus? MoveWorkflowAll(DateTime month, RevenueApprovalStatus state, int direction)
        {
            try
            {
                // Stored procedure for bulk update is a lot faster
                var user = ClaimProvider.GetFriendlyName(_context);    
                var nextState = direction >= 0 ? NextState(state) : PrevState(state);

                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));

                SqlParameter[] sqlParams = new SqlParameter[4];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@State", SqlDbType.Int);
                sqlParams[2].Value = direction >= 0 ? state : nextState;
                sqlParams[3] = new SqlParameter("@User", SqlDbType.NVarChar);
                sqlParams[3].Value = user;
                var result = _context.Database.SqlQuery<SqlResult>("UpdateAllResolutionWorkflowStates @StartDate, @EndDate, @State, @User", sqlParams).FirstOrDefault();

                return result.Count > 0 ? nextState : null;
            }
            catch
            {
                throw;
            }
        }

        // Replaced by stored precedure version MoveWorkflowAll above due to performance issue using Entity Framework for bulk db operation
        public RevenueApprovalStatus? MoveWorkflowAll(DateTime month, RevenueApprovalStatus state)
        {
            try
            {
                // Too slow using Entity Framework for bulk upload
                var provider = new ResolutionRevenueProvider(_context);
                List<ResolutionRevenueModel> resolutions = provider.Retrieve(month, string.Empty);
                var nextState = NextState(state);
                if (resolutions != null && nextState != null)
                {
                    var dataProvider = new ResolutionProvider(_context);
                    foreach (var viewModel in resolutions)
                    {
                        //MoveWorkflow(viewModel.ResolutionId, state);
                        var entity = dataProvider.Retrieve(viewModel.ResolutionId);
                        if (entity != null)
                        {
                            entity.ApprovalStatus = state;
                            SetWorkflowSignature(entity, state);
                            dataProvider.Update(entity.ResolutionId, entity);
                        }
                    }
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

        public RevenueApprovalStatus? MoveWorkflow(int resolutionId, RevenueApprovalStatus state)
        {
            try
            {
                var dataProvider = new ResolutionProvider(_context);
                var entity = dataProvider.Retrieve(resolutionId);
                var nextState = NextState(state);
                if (entity != null && nextState != null)
                {
                    entity.ApprovalStatus = state;
                    SetWorkflowSignature(entity, state);
                    dataProvider.Update(resolutionId, entity);
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

        // Replaced by stored precedure version MoveWorkflowAll above due to performance issue using Entity Framework for bulk db operation
        public RevenueApprovalStatus? BacktrackWorkflowAll(DateTime month, RevenueApprovalStatus state)
        {
            try
            {
                // Too slow to use Entity Framework for bulk upload
                var provider = new ResolutionRevenueProvider(_context);
                var resolutions = provider.Retrieve(month, string.Empty);
                var prevState = PrevState(state);
                if (resolutions != null && prevState != null)
                {
                    var dataProvider = new ResolutionProvider(_context);
                    foreach (var viewModel in resolutions)
                    {
                        //BacktrackWorkflow(viewModel.ResolutionId, state);
                        var entity = dataProvider.Retrieve(viewModel.ResolutionId);
                        if (entity != null)
                        {
                            entity.ApprovalStatus = prevState.Value;
                            RetrackWorkflowSignature(entity, state);
                            dataProvider.Update(entity.ResolutionId, entity);
                        }
                    }
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

        public RevenueApprovalStatus? BacktrackWorkflow(int resolutionId, RevenueApprovalStatus state)
        {
            try
            {
                var dataProvider = new ResolutionProvider(_context);
                var entity = dataProvider.Retrieve(resolutionId);
                var prevState = PrevState(state);
                if (entity != null && prevState != null)
                {
                    entity.ApprovalStatus = prevState.Value;
                    RetrackWorkflowSignature(entity, state);
                    dataProvider.Update(resolutionId, entity);
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

        public bool SetFieldStatus(int resolutionId, string fieldname, bool included)
        {
            try
            {
                var provider = new ResolutionProvider(_context);
                var entity = provider.Retrieve(resolutionId);
                if (entity != null)
                {
                    if (fieldname == "IncludeOnStatement")
                    {
                        entity.IncludeOnStatement = included;
                        provider.Update(entity.ResolutionId, entity);
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

        #region override to delegate CRUD operations to databae's Expense entity

        public override ResolutionRevenueModel Retrieve(int id)
        {
            try
            {
                if (id == 0) return new ResolutionRevenueModel();

                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@ResolutionId", SqlDbType.Int);
                sqlParams[0].Value = id;

                return _context.Database.SqlQuery<ResolutionRevenueModel>("RetrieveResolutionRevenueById @ResolutionId", sqlParams).FirstOrDefault();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public override void Create(ResolutionRevenueModel model)
        {
            try
            {
                var dataProvider = new ResolutionProvider(_context);
                var entity = new Resolution();
                MapData(model, entity, true);
                dataProvider.Create(entity);
            }
            catch
            {
                throw;
            }
        }

        public override void Update(int id, ResolutionRevenueModel model)
        {
            try
            {
                var dataProvider = new ResolutionProvider(_context);
                var entity = dataProvider.Retrieve(model.ResolutionId);
                MapData(model, entity, false);
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
                var dataProvider = new ResolutionProvider(_context);
                var entity = dataProvider.Retrieve(id);
                entity.IsDeleted = true;
                dataProvider.Update(id, entity);
            }
            catch
            {
                throw;
            }
        }

        private void MapData(ResolutionRevenueModel from, Resolution to, bool isNew = false)
        {
            to.ConfirmationCode = from.ConfirmationCode;
            to.PropertyCode = from.PropertyCode;
            to.Impact = from.Impact;
            to.Cause = from.Cause;
            to.ResolutionType = from.ResolutionType;
            to.ResolutionDate = ConversionHelper.EnsureUtcDate(from.ResolutionDate);
            to.ResolutionAmount = from.ResolutionAmount;
            to.ResolutionDescription = from.ResolutionDescription;
            to.IncludeOnStatement = from.IncludeOnStatement;

            if (isNew)
            {
                to.OwnerPayoutId = from.OwnerPayoutId;
                to.IsDeleted = false;
                to.InputSource = AppConstants.MANUAL_INPUT_SOURCE;
                to.ApprovalStatus = RevenueApprovalStatus.NotStarted;
                to.ApprovedNote = string.Empty;
            }
            else
            {
                to.ApprovedNote = from.ApprovedNote;
            }
        }

        #endregion

        #region not implemented interfaces

        public override ResolutionRevenueModel Retrieve(string id)
        {
            throw new NotImplementedException();
        }

        public override IQueryable<ResolutionRevenueModel> GetAll()
        {
            throw new NotImplementedException();
        }

        public override void Update(string id, ResolutionRevenueModel model)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override void Delete(ResolutionRevenueModel model)
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

        private void SetWorkflowSignature(Resolution entity, RevenueApprovalStatus state)
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

        private void RetrackWorkflowSignature(Resolution entity, RevenueApprovalStatus state)
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

        private int TryGetReservationId(ResolutionRevenueModel model)
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
}
