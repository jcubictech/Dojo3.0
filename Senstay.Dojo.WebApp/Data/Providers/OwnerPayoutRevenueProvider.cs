using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.Grid;
using System.Data.Entity;

namespace Senstay.Dojo.Data.Providers
{
    public class OwnerPayoutRevenueProvider : CrudProviderBase<OwnerPayoutRevenueModel>
    {
        private readonly DojoDbContext _context;

        public OwnerPayoutRevenueProvider(DojoDbContext dbContext) : base(dbContext, true)
        {
            _context = dbContext;
        }

        #region custom methods for owner payout revenue

        public int GetKey(OwnerPayoutRevenueModel model)
        {
            var entity = _context.OwnerPayouts.Where(r => string.Compare(r.Source, model.Source, true) == 0 &&
                                                         DbFunctions.TruncateTime(r.PayoutDate.Value) == DbFunctions.TruncateTime(model.PayoutDate.Value) &&
                                                         r.PayoutAmount == model.PayoutAmount &&
                                                         r.AccountNumber == model.PayToAccount).FirstOrDefault();
            if (entity != null)
                return entity.OwnerPayoutId;
            else
                return 0;
        }

        public List<OwnerPayoutRevenueModel> Retrieve(DateTime month, string source)
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
                sqlParams[2] = new SqlParameter("@Source", SqlDbType.NVarChar);
                sqlParams[2].Value = source;

                List<OwnerPayoutRevenueFlatModel> payouts = _context.Database.SqlQuery<OwnerPayoutRevenueFlatModel>("RetrieveOwnerPayoutRevenue @StartDate, @EndDate, @Source", sqlParams).ToList();

                // group query result into owner payout and reservation/ resolution relation
                var payoutRevenue = (from p in payouts
                                     group p by new // group by owner payout
                                     {
                                         p.OwnerPayoutId,
                                         p.Source,
                                         p.PayoutDate,
                                         p.PayoutAmount,
                                         p.PayToAccount,
                                         p.IsAmountMatched,
                                         p.DiscrepancyAmount,
                                         p.InputSource,
                                         p.ReservationTotal,
                                         p.ResolutionTotal
                                     } into g
                                     orderby g.Key.PayoutDate, g.Key.PayoutAmount descending
                                     select new OwnerPayoutRevenueModel
                                     {
                                         OwnerPayoutId = g.Key.OwnerPayoutId,
                                         Source = g.Key.Source,
                                         PayoutDate = g.Key.PayoutDate,
                                         PayoutAmount = g.Key.PayoutAmount,
                                         PayToAccount = g.Key.PayToAccount,
                                         IsAmountMatched = Math.Round((double)g.Key.PayoutAmount - (g.Key.ReservationTotal == null ? 0 : g.Key.ReservationTotal.Value) - (g.Key.ResolutionTotal == null ? 0 : g.Key.ResolutionTotal.Value), 2) == 0 ? true : false, // g.Key.IsAmountMatched,
                                         DiscrepancyAmount = Math.Round((double)g.Key.PayoutAmount - (g.Key.ReservationTotal == null ? 0 : g.Key.ReservationTotal.Value) - (g.Key.ResolutionTotal == null ? 0 : g.Key.ResolutionTotal.Value), 2), // g.Key.DiscrepancyAmount,
                                         InputSource = g.Key.InputSource,
                                         ReservationTotal = g.Key.ReservationTotal,
                                         ResolutionTotal = g.Key.ResolutionTotal,
                                         Children = g.Select(x => new OwnerPayoutRevenueChildModel
                                         {
                                             RevenueType = x.RevenueType,
                                             CheckinDate = x.CheckinDate,
                                             Nights = x.Nights,
                                             ConfirmationCode = x.ConfirmationCode == null ? "" : x.ConfirmationCode,
                                             PropertyCode = x.PropertyCode,
                                             ChildId = x.ChildId,
                                             Amount = x.Amount
                                         })
                                         .Where(x => x.Amount != null)
                                         .ToList()
                                     }).ToList();

                return payoutRevenue;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        #endregion

        #region override to delegate CRUD operations to databae's Owner Payout entity

        public override OwnerPayoutRevenueModel Retrieve(int ownerPayoutId)
        {
            try
            {
                if (ownerPayoutId == 0) return new OwnerPayoutRevenueModel();

                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@OwnerPayoutId", SqlDbType.Int);
                sqlParams[0].Value = ownerPayoutId;

                return _context.Database.SqlQuery<OwnerPayoutRevenueModel>("RetrieveOwnerPayoutRevenueById @OwnerPayoutId", sqlParams).FirstOrDefault();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public override void Create(OwnerPayoutRevenueModel model)
        {
            try
            {
                OwnerPayoutProvider provider = new OwnerPayoutProvider(_context);
                var entity = new OwnerPayout();
                provider.MapData(model, entity, true);
                provider.Create(entity);
            }
            catch
            {
                throw;
            }
        }

        public override void Update(int id, OwnerPayoutRevenueModel model)
        {
            try
            {
                OwnerPayoutProvider provider = new OwnerPayoutProvider(_context);
                OwnerPayout entity = provider.Retrieve(id);
                provider.MapData(model, entity, false);
                provider.Update(id, entity);
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
                OwnerPayoutProvider provider = new OwnerPayoutProvider(_context);
                OwnerPayout entity = provider.Retrieve(id);
                entity.IsDeleted = true;
                provider.Update(id, entity);
            }
            catch
            {
                throw;
            }
        }

        #endregion

        #region not implemented interfaces

        public override OwnerPayoutRevenueModel Retrieve(string id)
        {
            throw new NotImplementedException();
        }

        public override IQueryable<OwnerPayoutRevenueModel> GetAll()
        {
            throw new NotImplementedException();
        }

        public override void Update(string id, OwnerPayoutRevenueModel model)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override void Delete(OwnerPayoutRevenueModel model)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region private methods

        #endregion
    }
}
