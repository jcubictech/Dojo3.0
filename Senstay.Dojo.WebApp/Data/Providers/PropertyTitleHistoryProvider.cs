using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyTitleHistoryProvider : CrudProviderBase<PropertyTitleHistory>
    {
        private readonly DojoDbContext _context;

        public PropertyTitleHistoryProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<PropertyTitleHistory> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.PropertyCode).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool Exist(int id)
        {
            return _context.PropertyTitleHistories.FirstOrDefault(p => p.PropertyTitleHistoryId == id) != null;
        }

        public bool Exist(string propertyCode, string title)
        {
            return _context.PropertyTitleHistories.FirstOrDefault(p => p.PropertyCode == propertyCode && p.PropertyTitle.ToLower() == title.ToLower()) != null;
        }

        public string GetPropertyCodeByTitle(string title)
        {
            var entity = _context.PropertyTitleHistories.FirstOrDefault(p => p.PropertyTitle.ToLower() == title.ToLower());
            if (entity != null)
                return entity.PropertyCode;
            else
                return null;
        }
    }

    public class PropertyTitleHistoryRow
    {
        public int PropertyTitleHistoryId { get; set; }

        public string PropertyTitle { get; set; }

        public string PropertyCode { get; set; }

        public DateTime EffectiveDate { get; set; }
    }
}
