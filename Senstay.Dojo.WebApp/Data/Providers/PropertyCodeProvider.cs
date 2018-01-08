using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyCodeProvider
    {
        private readonly DojoDbContext _context;

        public PropertyCodeProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public List<PropertyCodeViewModel> GetPropertyCodeInfo()
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                var result = _context.Database.SqlQuery<PropertyCodeViewModel>("GetPropertyCodeInfo", sqlParams).ToList();
                return result;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public int Update(PropertyCodeViewModel model)
        {
            try
            {
                // 3 relation tables to update: PropertyPayoutMethods, PropertyCodePropertyEntities, and PropertyAccountPayoutMethods

                var payoutMethodProvider = new PayoutMethodProvider(_context);
                int updateCount = payoutMethodProvider.UpdatePropertyCodeByName(model.PayoutMethod, model.PropertyCode);

                var entityProvider = new PropertyEntityProvider(_context);
                updateCount += entityProvider.UpdatePropertyCodeByName(model.PayoutEntity, model.PropertyCode);

                var accountProvider = new PropertyAccountProvider(_context);
                updateCount += accountProvider.UpdatePayoutMethodByOwner(model.PropertyOwner, model.PayoutMethod);

                return updateCount;
            }
            catch
            {
                throw;
            }
        }
    }
}
