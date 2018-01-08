using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Web.Mvc;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class LookupProvider : CrudProviderBase<Lookup>
    {
        private readonly DojoDbContext _context;
        private List<Lookup> _all; // cache per provider call cycle

        public LookupProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
            _all = _context.Lookups.Distinct().ToList();
        }

        public List<Lookup> All(LookupType type)
        {
            return _all.Where(l => l.Type == type.ToString()).OrderBy(l => l.Name).ToList();
        }

        public List<Lookup> All(string type)
        {
            return _all.Where(l => l.Type == type).OrderBy(l => l.Name).ToList();
        }

        public int GetKey(Lookup model)
        {
            var entity = _context.Lookups.Where(x => string.Compare(x.Type, model.Type, true) == 0 &&
                                                     string.Compare(x.Name, model.Name, true) == 0).FirstOrDefault();
            if (entity != null)
                return entity.Id;
            else
                return 0;
        }

        public List<string> GetLookupText(LookupType type)
        {
            if (type == LookupType.Boolean)
            {
                return new List<string>()
                {
                    "Yes",
                    "No"
                };
            }
            else
            {
                return _all.Where(l => l.Type == type.ToString())
                           .Select(l => l.Name)
                           .ToList();
            }
        }

        public List<SelectListItem> GetLookupList(LookupType type)
        {
            if (type == LookupType.Boolean)
            {
                return new List<SelectListItem>()
                {
                    new SelectListItem { Text = "Yes", Value = "True" },
                    new SelectListItem { Text = "No", Value = "False" }
                };
            }
            else
            {
                return _all.Where(l => l.Type == type.ToString())
                           .Select(l => new SelectListItem
                           {
                               Text = l.Name,
                               Value = l.Name
                           })
                           //.OrderBy(l => l.Text)
                           .ToList();
            }
        }

        public List<Lookup> AllVerticals()
        {
            return All(LookupType.Vertical);
        }

        public List<Lookup> AllCities()
        {
            return All(LookupType.City);
        }

        public List<Lookup> AllMarkets()
        {
            return All(LookupType.Market);
        }

        public List<Lookup> AllAreas()
        {
            return All(LookupType.Area);
        }

        public List<Lookup> AllChannels()
        {
            return All(LookupType.Channel);
        }

        public List<Lookup> AllNeighborhoods()
        {
            return All(LookupType.Neighborhood);
        }

        public bool Exist(LookupType type, string value)
        {
            return _all.Where(l => l.Type == type.ToString() && l.Name == value).Count() > 0;
        }

        public bool InUse(LookupType type, string value)
        {
            value = value.ToLower();
            bool inUse = false;
            switch(type)
            {
                case LookupType.Vertical:
                    inUse = _context.CPLs.Where(c => c.Vertical.ToLower() == value).Count() > 0 ||
                            _context.AirbnbAccounts.Where(c => c.Vertical.ToLower() == value).Count() > 0;
                    break;
                case LookupType.Channel:
                    inUse = _context.InquiriesValidations.Where(c => c.Channel.ToLower() == value).Count() > 0;
                    break;
                case LookupType.Market:
                    inUse = _context.CPLs.Where(c => c.Market.ToLower() == value).Count() > 0;
                    break;
                case LookupType.Area:
                    inUse = _context.CPLs.Where(c => c.Area.ToLower() == value).Count() > 0;
                    break;
                case LookupType.Neighborhood:
                    inUse = _context.CPLs.Where(c => c.Neighborhood.ToLower() == value).Count() > 0;
                    break;
                case LookupType.City:
                    inUse = _context.CPLs.Where(c => c.City.ToLower() == value).Count() > 0;
                    break;
                default:
                    break;
            }
            return inUse;
        }
    }
}
