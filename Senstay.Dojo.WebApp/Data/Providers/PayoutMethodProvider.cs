using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Web.Mvc;
using System.Data.SqlClient;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Data.Providers
{
    public class PayoutMethodProvider : CrudProviderBase<PayoutMethod>
    {
        private readonly DojoDbContext _context;

        public PayoutMethodProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<PayoutMethodViewModel> All()
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                var entities = _context.Database.SqlQuery<PayoutMethodViewModel>("RetrievePayoutMethods", sqlParams).ToList();
                foreach (var entity in entities)
                {
                    entity.EffectiveDate = ConversionHelper.EnsureUtcDate(entity.EffectiveDate.Date);
                    if (!string.IsNullOrEmpty(entity.CurrentPropertyCodes))
                    {
                        string[] currentPropertyCodes = entity.CurrentPropertyCodes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string propertyCode in currentPropertyCodes)
                        {
                            entity.SelectedPropertyCodes.Add(new SelectListItem
                            {
                                Text = propertyCode.Trim(),
                                Value = propertyCode.Trim(),
                            });
                        }
                    }
                }
                return entities;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool Exist(int id)
        {
            return _context.PayoutMethods.FirstOrDefault(p => p.PayoutMethodId == id) != null;
        }

        public List<SelectListItem> PayoutMethodList()
        {
            return _context.PayoutMethods.OrderBy(m => m.PayoutMethodName)
                                           .Select(m => new SelectListItem
                                            {
                                                Text = m.PayoutMethodName,
                                                Value = m.PayoutMethodId.ToString()
                                            })
                                           .ToList();
        }

        public List<SelectListItem> PropertyList(string payoutMethod, DateTime month)
        {
            var properties = (from m in _context.PayoutMethods.Where(m => m.PayoutMethodName == payoutMethod && m.EffectiveDate <= month)
                              join p in _context.PropertyPayoutMethods
                              on m.PayoutMethodId equals p.PayoutMethodId
                              join c in _context.CPLs
                              on p.PropertyCode equals c.PropertyCode
                              select new SelectListItem
                                  {
                                      Text = p.PropertyCode + " | " + c.Address,
                                      Value = p.PropertyCode
                                  })
                              .ToList();
            return properties;
        }

        public List<PropertyBalanceModel> PropertyBalances(string payoutMethod, DateTime effectiveDate)
        {
            int month = effectiveDate.Month;
            int year = effectiveDate.Year;
            var properties = (from m in _context.PayoutMethods.Where(m => m.PayoutMethodName == payoutMethod && m.EffectiveDate <= effectiveDate)
                              join p in _context.PropertyPayoutMethods
                              on m.PayoutMethodId equals p.PayoutMethodId
                              join s in _context.OwnerStatements.Where(s => s.Month == month && s.Year == year)
                              on p.PropertyCode equals s.PropertyCode
                              select new PropertyBalanceModel
                              {
                                  PropertyCode = s.PropertyCode,
                                  PropertyBalance = s.Balance
                              })
                              .ToList();
            return properties;
        }

        public void MapData(PayoutMethodViewModel model, ref PayoutMethod entity)
        {
            PayoutMethodType methodType = PayoutMethodType.Checking; // default to checking if parse fail
            Enum.TryParse(model.PayoutMethodType, out methodType);
            entity.PayoutMethodId = model.PayoutMethodId;
            entity.PayoutMethodName = model.PayoutMethodName;
            entity.EffectiveDate = ConversionHelper.EnsureUtcDate(model.EffectiveDate);
            entity.PayoutMethodType = methodType;
            entity.PayoutAccount = model.PayoutAccount;
        }

        public List<PropertyPayoutMethod> PropertyToUpdate(int entityId, ICollection<SelectListItem> propertyCodes)
        {
            var oldPropertyPayoutMethods = _context.PropertyPayoutMethods.Where(x => x.PayoutMethodId == entityId).ToList();
            var oldProperties = oldPropertyPayoutMethods.OrderBy(x => x.PropertyCode).Select(x => x.PropertyCode).ToList();
            var newProperties = propertyCodes.OrderBy(x => x.Value).Select(x => x.Value).ToArray();
            if (string.Join(",", oldProperties) != string.Join(",", newProperties))
                return oldPropertyPayoutMethods;
            else
                return null;
        }

        public int UpdatePropertyCodeByName(string name, string propertyCode)
        {
            int updateCount = 0;
            try
            {
                if (string.IsNullOrEmpty(name)) // case of removing roperty code from payout method 
                {
                    var entities = _context.PropertyPayoutMethods.Where(x => x.PropertyCode == propertyCode);
                    if (entities != null && entities.Count() > 0)
                    {
                        _context.PropertyPayoutMethods.RemoveRange(entities);
                        _context.SaveChanges();
                    }
                }
                else
                {
                    var entity = _context.PayoutMethods.Where(x => x.PayoutMethodName == name).FirstOrDefault();
                    if (entity != null)
                    {
                        var entityId = entity.PayoutMethodId;
                        var count = (from pm in _context.PropertyPayoutMethods.Where(x => x.PropertyCode == propertyCode)
                                     join m in _context.PayoutMethods
                                     on pm.PayoutMethodId equals m.PayoutMethodId
                                     select m.PayoutMethodId)
                                    .Count();

                        if (count == 0) // add only when it does not exist
                        {
                            _context.PropertyPayoutMethods.Add(new PropertyPayoutMethod
                            {
                                PropertyCode = propertyCode,
                                PayoutMethodId = entityId
                            });
                            _context.SaveChanges();

                            updateCount = 1;
                        }
                    }
                }
                return updateCount;
            }
            catch
            {
                throw;
            }
        }
    }
}
