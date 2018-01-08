using System.Linq;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Airbnb.Import
{
    public class ReservationService
    {
        private readonly DojoDbContext _context;

        public ReservationService(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public string GetMostRecentFutureDate()
        {
            var inputSource1 = _context.FutureReservations.OrderByDescending(x => x.InputSource)
                                                          .Select(x => x.InputSource).FirstOrDefault();
            var date1 = inputSource1 != null ? inputSource1.Substring(0, inputSource1.IndexOf(" ")) : string.Empty;

            var inputSource2 = _context.FutureResolutions.OrderByDescending(x => x.InputSource)
                                                         .Select(x => x.InputSource).FirstOrDefault();
            var date2 = inputSource2 != null ? inputSource2.Substring(0, inputSource2.IndexOf(" ")) : string.Empty;

            return string.Compare(date1, date2) > 0 ? date1 : date2;
        }

        public bool FutureTransactionProcessed(string accountFile, string sortableDate)
        {
            return _context.FutureReservations.Count(x => x.InputSource.IndexOf(sortableDate) >= 0) > 0 ||
                   _context.FutureResolutions.Count(x => x.InputSource.IndexOf(sortableDate) >= 0) > 0;
        }
        public string GetMostRecentGrossDate()
        {
            var inputSource1 = _context.GrossEarnings.OrderByDescending(x => x.InputSource)
                                                          .Select(x => x.InputSource).FirstOrDefault();
            var date1 = inputSource1 != null ? inputSource1.Substring(0, inputSource1.IndexOf(" ")) : string.Empty;

            return date1;
        }
    }
}
