using System;
using System.Linq;
using System.Data;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Airbnb.Import
{
    public class OwnerPayoutService
    {
        private readonly DojoDbContext _context;

        public OwnerPayoutService(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public DateTime? GetMostRecentPayoutDate(string accountFile)
        {
            var source = accountFile.Substring(0, accountFile.ToLower().LastIndexOf("-airbnb"));
            var lastDate = _context.OwnerPayouts.Where(x => x.Source == source)
                                                .OrderByDescending(x => x.PayoutDate)
                                                .Select(x => x.PayoutDate).FirstOrDefault();

            return lastDate != null ? lastDate.Value.Date : lastDate;
        }
    }
}
