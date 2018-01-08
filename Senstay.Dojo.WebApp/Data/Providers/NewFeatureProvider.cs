using System;
using System.Linq;
using System.Data;
using Senstay.Dojo.Models;


namespace Senstay.Dojo.Data.Providers
{
    public class NewFeatureProvider
    {
        private readonly DojoDbContext _dbContext;

        public NewFeatureProvider(DojoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public bool IsExpired()
        {
            try
            {
                var count = _dbContext.NewFeatures
                                      .Where(f => f.ExpiredDate >= DateTime.Today)
                                      .OrderByDescending(f => f.DeployDate)
                                      .Count();
                return count == 0;
            }
            catch
            {
            }

            return true;
        }

        public NewFeature Get(string deployDate)
        {
            NewFeature newFeature = null;
            if (string.IsNullOrEmpty(deployDate))
            {
                newFeature = _dbContext.NewFeatures
                                       .OrderByDescending(f => f.DeployDate)
                                       .FirstOrDefault();
            }
            else
            {
                DateTime dateToMatch;
                if (DateTime.TryParse(deployDate, out dateToMatch))
                {
                    newFeature = _dbContext.NewFeatures
                                           .Where(f => f.DeployDate == dateToMatch)
                                           .FirstOrDefault();
                }
            }
            return newFeature;
        }
    }
}
