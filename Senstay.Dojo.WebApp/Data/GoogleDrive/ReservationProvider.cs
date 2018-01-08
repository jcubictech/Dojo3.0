using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.Grid;

namespace Senstay.Dojo.Data.Providers
{
    public class ReservationProvider : CrudProviderBase<Reservation>
    {
        private readonly DojoDbContext _context;

        public ReservationProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<Reservation> All()
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[0];
                List<Reservation> data = _context.Database.SqlQuery<Reservation>("RetrieveReservations", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<Reservation> Retrieve(DateTime beginDate, DateTime endDate, string propertyCode = "")
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = beginDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;

                List<Reservation> data = _context.Database.SqlQuery<Reservation>("RetrieveReservations @StartDate, @EndDate, @PropertyCode", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        #region private methods

        #endregion
    }
}
