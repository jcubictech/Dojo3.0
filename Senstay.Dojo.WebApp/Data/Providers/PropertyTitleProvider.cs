using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyTitleProvider : CrudProviderBase<PropertyTitleHistory>
    {
        private readonly DojoDbContext _context;

        public PropertyTitleProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<PropertyTitleHistory> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.PropertyCode).OrderBy(x => x.PropertyCode).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
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
