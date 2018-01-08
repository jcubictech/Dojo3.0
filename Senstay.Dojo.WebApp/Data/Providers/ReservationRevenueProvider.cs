using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.Grid;
using System.Security.Claims;
using System.Data.Entity;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Models.HelperClass;
using System.Web.Mvc;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Data.Providers
{
    public class ReservationRevenueProvider : CrudProviderBase<ReservationRevenueModel>
    {
        private readonly DojoDbContext _context;

        public ReservationRevenueProvider(DojoDbContext dbContext) : base(dbContext, true)
        {
            _context = dbContext;
        }

        #region custom methods for revenue reservation

        public int GetIdByConfirmationCode(string confirmationCode)
        {
            var entity = _context.Reservations.Where(r => r.ConfirmationCode == confirmationCode).FirstOrDefault();
            if (entity != null)
                return entity.ReservationId;
            else
                return 0;
        }

        public List<SelectListItem> GetConfirmationCode(string account)
        {
            try
            {
                // Stored procedure for bulk update is a lot faster
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@Account", SqlDbType.NVarChar);
                sqlParams[0].Value = account;
                var result = _context.Database.SqlQuery<SelectListItem>("GetConfirmationCodeForAccount @Account", sqlParams).ToList();

                return result;
            }
            catch
            {
                throw;
            }
        }

        public string GetPropertyCodeById(int id)
        {
            var model = _context.Reservations.Where(r => r.ReservationId == id).FirstOrDefault();
            if (model != null)
                return model.PropertyCode;
            else
                return string.Empty;
        }

        public int GetKey(ReservationRevenueModel model)
        {
            var entity = _context.Reservations.Where(r => string.Compare(r.GuestName, model.GuestName, true) == 0 &&
                                                         DbFunctions.TruncateTime(r.CheckinDate.Value) == DbFunctions.TruncateTime(model.CheckinDate.Value) &&
                                                         DbFunctions.TruncateTime(r.TransactionDate.Value) == DbFunctions.TruncateTime(model.PayoutDate.Value) &&
                                                         r.Nights == model.Nights &&
                                                         r.TotalRevenue == model.TotalRevenue &&
                                                         r.Channel == model.Channel &&
                                                         r.PropertyCode == model.PropertyCode).FirstOrDefault();
            if (entity != null)
                return entity.ReservationId;
            else
                return 0;
        }

        public string GetReservationSource(string propertyCode, string currentUser)
        {
            string account = _context.CPLs.Where(x => x.PropertyCode == propertyCode).Select(x => x.Account).FirstOrDefault();
            if (account == null) account = currentUser;
            return account;
        }

        public List<DuplicateReservationModel> GetDuplicateReservations(DateTime month)
        {
            try
            {
                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                var duplicates = _context.Database.SqlQuery<DuplicateReservationModel>("GetDuplicateReservations @StartDate, @EndDate", sqlParams).ToList();

                for (int i = 0; i < duplicates.Count - 1; i++)
                {
                    int next = i + 1;
                    if (duplicates[i].TransactionDate != duplicates[next].TransactionDate &&
                        duplicates[i].PayoutAccount == duplicates[next].PayoutAccount &&
                        duplicates[i].PropertyCode == duplicates[next].PropertyCode &&
                        duplicates[i].ConfirmationCode == duplicates[next].ConfirmationCode &&
                        duplicates[i].GuestName == duplicates[next].GuestName &&
                        duplicates[i].CheckinDate == duplicates[next].CheckinDate &&
                        duplicates[i].Nights == duplicates[next].Nights)
                    {
                        duplicates[i].IsSameTransactionDate = true;
                        duplicates[next].IsSameTransactionDate = true;
                    }
                }

                return duplicates;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<MissingPropertyCodesModel> GetMissingPropertyCodes(DateTime month)
        {
            try
            {
                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                var missing = _context.Database.SqlQuery<MissingPropertyCodesModel>("GetMissingPropertyCodes @StartDate, @EndDate", sqlParams).ToList();

                return missing;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<ReservationRevenueModel> Retrieve(DateTime startDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                return _context.Database.SqlQuery<ReservationRevenueModel>("RetrieveReservationRevenue @StartDate, @EndDate", sqlParams).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<ReservationRevenueModel> Retrieve(DateTime month, string propertyCode)
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

                List<ReservationRevenueModel> data = _context.Database.SqlQuery<ReservationRevenueModel>("RetrieveReservationRevenue @StartDate, @EndDate, @PropertyCode", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

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
                var result = _context.Database.SqlQuery<SqlResult>("UpdateAllReservationWorkflowStates @StartDate, @EndDate, @PropertyCode, @State, @User", sqlParams).FirstOrDefault();

                return result.Count > 0 ? nextState : null;
            }
            catch
            {
                throw;
            }
        }

        public RevenueApprovalStatus? MoveWorkflowAll(DateTime month, string propertyCode, RevenueApprovalStatus state)
        {
            try
            {
                var reservationProvider = new ReservationProvider(_context);

                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                var reservations = reservationProvider.Retrieve(startDate, endDate, propertyCode);
                var nextState = NextState(state);
                if (reservations != null && nextState != null)
                {
                    foreach (var reservation in reservations)
                    {
                        if (reservation.ApprovalStatus < state)
                        {
                            reservation.ApprovalStatus = state;
                            SetWorkflowSignature(reservation, state);
                            reservationProvider.Update(reservation.ReservationId, reservation);
                        }
                    }
                    reservationProvider.Commit();
                    return nextState;
                }
            }
            catch
            {
                throw;
            }
            return null;
        }

        public RevenueApprovalStatus? MoveWorkflow(int reservationId, RevenueApprovalStatus state)
        {
            try
            {
                var reservationProvider = new ReservationProvider(_context);
                var reservation = reservationProvider.Retrieve(reservationId);
                var nextState = NextState(state);
                if (reservation != null && nextState != null)
                {
                    reservation.ApprovalStatus = state;
                    SetWorkflowSignature(reservation, state);
                    reservationProvider.Update(reservationId, reservation);
                    reservationProvider.Commit();
                    return nextState;
                }
            }
            catch
            {
                throw;
            }
            return null;
        }

        public RevenueApprovalStatus? BacktrackWorkflowAll(DateTime month, string propertyCode, RevenueApprovalStatus state)
        {
            try
            {
                var reservationProvider = new ReservationProvider(_context);

                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                var reservations = reservationProvider.Retrieve(startDate, endDate, propertyCode);
                var prevState = PrevState(state);
                if (reservations != null && prevState != null)
                {
                    foreach (var reservation in reservations)
                    {
                        if (reservation.ApprovalStatus >= prevState)
                        {
                            reservation.ApprovalStatus = prevState.Value;
                            RetrackWorkflowSignature(reservation, state);
                            reservationProvider.Update(reservation.ReservationId, reservation);
                        }
                    }
                    reservationProvider.Commit();
                    return prevState;
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
                var reservationProvider = new ReservationProvider(_context);
                var reservation = reservationProvider.Retrieve(reservationId);
                var prevState = PrevState(state);
                if (reservation != null && prevState != null)
                {
                    reservation.ApprovalStatus = prevState.Value;
                    RetrackWorkflowSignature(reservation, state);
                    reservationProvider.Update(reservationId, reservation);
                    reservationProvider.Commit();
                    return prevState;
                }
            }
            catch
            {
                throw;
            }
            return null;
        }

        public string GetPropertyCodeByConfirmationCode(string confirmationCode)
        {
            string propertyCode = string.Empty;
            var property = _context.Reservations.Where(x => x.ConfirmationCode == confirmationCode).FirstOrDefault();
            if (property != null && property.PropertyCode != AppConstants.DEFAULT_PROPERTY_CODE)
                propertyCode = property.PropertyCode;

            return propertyCode;
        }

        public bool SetFieldStatus(int reservationId, string fieldname, double taxrate, bool included)
        {
            try
            {
                var reservationProvider = new ReservationProvider(_context);
                var reservation = reservationProvider.Retrieve(reservationId);
                if (reservation != null)
                {
                    if (fieldname == "IncludeOnStatement")
                        reservation.IncludeOnStatement = included;
                    else if (fieldname == "IsTaxed")
                    {
                        reservation.TaxRate = (float)taxrate;
                        reservation.IsTaxed = included;
                        UpdateTotalRevenue(reservation);
                    }
                    else if (fieldname == "IsFutureBooking")
                        reservation.IsFutureBooking = included;
                    else // not supported field; just ignore for now...
                        return true;

                    reservationProvider.Update(reservation.ReservationId, reservation);
                    reservationProvider.Commit();
                    return true;
                }
            }
            catch
            {
                throw;
            }
            return false;
        }

        public int? SplitReservation(ResevationSplitModel model)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@ReservationId", SqlDbType.Int);
                sqlParams[0].Value = model.ReservationId;
                sqlParams[1] = new SqlParameter("@PropertyCodes", SqlDbType.NVarChar);
                sqlParams[1].Value = String.Join(";", model.TargetProperties);

                var sql = _context.Database.SqlQuery<SqlResultModel>("SplitReservation @ReservationId, @PropertyCodes", sqlParams).FirstOrDefault();
                return sql == null ? null : sql.Result;
            }
            catch
            {
                throw;
            }
        }
        #endregion

        #region override to delegate CRUD operations to databae's Reservation entity

        public override ReservationRevenueModel Retrieve(int reservationId)
        {
            try
            {
                if (reservationId == 0) return new ReservationRevenueModel();

                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@ReservationId", SqlDbType.Int);
                sqlParams[0].Value = reservationId;

                return _context.Database.SqlQuery<ReservationRevenueModel>("RetrieveReservationRevenueById @ReservationId", sqlParams).FirstOrDefault();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public override void Create(ReservationRevenueModel model)
        {
            try
            {
                ReservationProvider dataProvider = new ReservationProvider(_context);
                var entity = new Reservation();
                MapData(model, entity, true);
                dataProvider.Create(entity);
            }
            catch
            {
                throw;
            }
        }

        public override void Update(int id, ReservationRevenueModel model)
        {
            try
            {
                ReservationProvider reservationProvider = new ReservationProvider(_context);
                Reservation entity = reservationProvider.Retrieve(model.ReservationId);
                MapData(model, entity, false);
                reservationProvider.Update(id, entity);
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
                // revenue reservation deletion does not physically delete the record; it marks [IsDeletd] = true
                ReservationProvider reservationProvider = new ReservationProvider(_context);
                Reservation reservation = reservationProvider.Retrieve(id);
                reservation.IsDeleted = true;
                reservationProvider.Update(id, reservation);
            }
            catch
            {
                throw;
            }
        }

        private void MapData(ReservationRevenueModel from, Reservation to, bool isNew = false)
        {
            // map form fields
            to.ConfirmationCode = from.ConfirmationCode;
            to.PropertyCode = from.PropertyCode;
            to.Channel = from.Channel;
            to.TransactionDate = ConversionHelper.EnsureUtcDate(from.PayoutDate);
            to.GuestName = from.GuestName;
            to.CheckinDate = ConversionHelper.EnsureUtcDate(from.CheckinDate);
            to.Nights = from.Nights;
            to.TotalRevenue = from.TotalRevenue;
            to.IncludeOnStatement = from.IncludeOnStatement;
            to.CheckoutDate = to.CheckinDate.Value.AddDays(from.Nights);

            if (isNew)
            {
                var revenueProvider = new ReservationRevenueProvider(_context);
                to.Source = revenueProvider.GetReservationSource(to.PropertyCode, ClaimsPrincipal.Current.Identity.Name);
                to.ApprovalStatus = RevenueApprovalStatus.NotStarted;

                to.OwnerPayoutId = from.OwnerPayoutId;
                to.IsDeleted = false;
                to.InputSource = AppConstants.MANUAL_INPUT_SOURCE;
            }
            else
            {
                // all other fields should be in to object
            }
        }

        #endregion

        #region not implemented interfaces

        public override ReservationRevenueModel Retrieve(string id)
        {
            throw new NotImplementedException();
        }

        public override IQueryable<ReservationRevenueModel> GetAll()
        {
            throw new NotImplementedException();
        }

        public override void Update(string id, ReservationRevenueModel model)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override void Delete(ReservationRevenueModel model)
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

        private void SetWorkflowSignature(Reservation reservation, RevenueApprovalStatus state)
        {
            var userName = ClaimProvider.GetFriendlyName(_context);
            switch (state)
            {
                case RevenueApprovalStatus.Reviewed:
                    reservation.ReviewedBy = userName;
                    reservation.ReviewedDate = DateTime.Now.ToUniversalTime();
                    break;
                case RevenueApprovalStatus.Approved:
                    reservation.ApprovedBy = userName;
                    reservation.ApprovedDate = DateTime.Now.ToUniversalTime();
                    break;
            }
        }

        private void RetrackWorkflowSignature(Reservation reservation, RevenueApprovalStatus state)
        {
            var userName = ClaimProvider.GetFriendlyName(_context);
            switch (state)
            {
                case RevenueApprovalStatus.Reviewed:
                    reservation.ReviewedBy = null;
                    reservation.ReviewedDate = null;
                    break;
                case RevenueApprovalStatus.Approved:
                    reservation.ApprovedBy = null;
                    reservation.ApprovedDate = null;
                    break;
            }
        }

        private void UpdateTotalRevenue(Reservation reservation)
        {
            if (reservation.TaxRate != null && reservation.TaxRate.Value > 0)
            {
                if (reservation.IsTaxed) // change to taxed revenue
                {
                    if (reservation.TotalRevenue - reservation.DamageWaiver.Value > 0)
                    {
                        reservation.TotalRevenue = (reservation.TotalRevenue - reservation.DamageWaiver.Value) / (1.14f + reservation.TaxRate.Value);
                    }
                    else
                    {
                        reservation.AdminFee = reservation.TotalRevenue; // placeholder for resersing taxed revenue
                        reservation.TotalRevenue = 0;
                    }
                }
                else // reverse taxed revenue
                {
                    if (reservation.TotalRevenue == 0 && reservation.DamageWaiver != null && reservation.DamageWaiver.Value > 0)
                    {
                        reservation.TotalRevenue = reservation.AdminFee.Value;
                    }
                    else
                        reservation.TotalRevenue = reservation.TotalRevenue * (1.14f + reservation.TaxRate.Value) + reservation.DamageWaiver.Value;
                }
            }

        }
        #endregion
    }
}
