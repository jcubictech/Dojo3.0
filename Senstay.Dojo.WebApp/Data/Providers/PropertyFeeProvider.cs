using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyFeeProvider : CrudProviderBase<PropertyFee>
    {
        private readonly DojoDbContext _context;

        public PropertyFeeProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<PropertyFeeViewModel> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.PropertyCode)
                               .Select(x => new PropertyFeeViewModel
                               {
                                    PropertyFeeId = x.PropertyCostId,
                                    PropertyCode = x.PropertyCode,
                                    EffectiveDate = x.EntryDate,
                                    CityTax = x.CityTax == null ? 0 : x.CityTax.Value,
                                    ManagementFee = x.ManagementFee == null ? 0 : x.ManagementFee.Value,
                                    DamageWaiver = x.DamageWaiver == null ? 0 : x.DamageWaiver.Value,
                                    Cleanings = x.Cleanings == null ? 0 : x.Cleanings.Value,
                                    Consumables = x.Consumables == null ? 0 : x.Consumables.Value,
                                    Landscaping = x.Landscaping == null ? 0 : x.Landscaping.Value,
                                    Laundry = x.Laundry == null ? 0 : x.Laundry.Value,
                                    PoolService = x.PoolService == null ? 0 : x.PoolService.Value,
                                    TrashService = x.TrashService == null ? 0 : x.TrashService.Value,
                                    PestService = x.PestService == null ? 0 : x.PestService.Value,
                               })
                               .ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public PropertyFee Retrieve(string propertyCode, DateTime month)
        {
            return _context.PropertyFees.Where(x => x.PropertyCode == propertyCode && x.EntryDate < month)
                                        .OrderByDescending(x => x.EntryDate)
                                        .FirstOrDefault();
        }

        public bool Exist(int id)
        {
            return _context.PropertyFees.FirstOrDefault(p => p.PropertyCostId == id) != null;
        }

        public void MapData(PropertyFeeViewModel model, ref PropertyFee entity)
        {
            entity.PropertyCostId = model.PropertyFeeId;
            entity.PropertyCode = model.PropertyCode;
            entity.EntryDate = ConversionHelper.EnsureUtcDate(model.EffectiveDate.Date);
            entity.CityTax = model.CityTax;
            entity.ManagementFee = model.ManagementFee;
            entity.DamageWaiver = model.DamageWaiver;
            entity.Cleanings = model.Cleanings;
            entity.Consumables = model.Consumables;
            entity.Landscaping = model.Landscaping;
            entity.Laundry = model.Laundry;
            entity.PoolService = model.PoolService;
            entity.TrashService = model.TrashService;
            entity.PestService = model.PestService;
            entity.InputSource = AppConstants.MANUAL_INPUT_SOURCE;
        }
    }
}
