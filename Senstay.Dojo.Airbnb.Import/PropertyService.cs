using System.Linq;
using System.Data;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Airbnb.Import
{
    public class PropertyService
    {
        private readonly DojoDbContext _context;

        public PropertyService(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public string GetPropertyCodeByListing(string account, string listing)
        {
            string propertyCode = string.Empty;
            CPL property = _context.CPLs.Where(p => p.AirBnBHomeName.Contains(listing)).FirstOrDefault();
            if (property != null)
                propertyCode = property.PropertyCode;
            else // try PropertyTitleHistory table
            {
                var entity = _context.PropertyTitleHistories.FirstOrDefault(p => p.PropertyTitle.ToLower() == listing.ToLower());
                if (entity != null) propertyCode = entity.PropertyCode;
            }
            return propertyCode;
        }
    }
}
