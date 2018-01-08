using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyAccountProvider : CrudProviderBase<PropertyAccount>
    {
        private readonly DojoDbContext _context;

        public PropertyAccountProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<PropertyAccountViewModel> All()
        {
            try
            {
                var payoutMethods = new PayoutMethodProvider(_context).GetAll().ToList();
                SqlParameter[] sqlParams = new SqlParameter[1];
                var accounts = _context.Database.SqlQuery<PropertyAccountViewModel>("RetrievePropertyAccounts", sqlParams).ToList();
                foreach (var account in accounts)
                {
                    string[] payoutMethodIds = account.CurrentPayoutMethodIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string idString in payoutMethodIds)
                    {
                        var id = Int32.Parse(idString.Trim());
                        account.SelectedPayoutMethods.Add(new SelectListItem
                        {
                            Text = payoutMethods.Where(m => m.PayoutMethodId == id).Select(m => m.PayoutMethodName).FirstOrDefault(),
                            Value = idString.Trim(),
                        });
                    }
                }
                return accounts;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool Exist(int id)
        {
            return _context.PropertyAccounts.FirstOrDefault(p => p.PropertyAccountId == id) != null;
        }
        
        public void MapData(PropertyAccountViewModel model, ref PropertyAccount entity)
        {
            entity.PropertyAccountId = model.PropertyAccountId;
            entity.LoginAccount = model.LoginAccount;
            entity.OwnerName = model.OwnerName;
            entity.OwnerEmail = model.OwnerEmail;
        }

        public bool IsPayoutMethodLinkChanged(PropertyAccountViewModel model)
        {
            //var current = (from p in _context.PropertyAccounts.Where(p => p.PropertyAccountId == model.PropertyAccountId)
            //               join l in _context.PropertyAccountPayoutMethods
            //               on p.PropertyAccountId equals l.PropertyAccountId
            //               join m in _context.PayoutMethods
            //               on l.PayoutMethodId equals m.PayoutMethodId                          
            //               select m.PayoutMethodName).ToList();
            //var oldListString = string.Join(", ", current);

            var newList = model.SelectedPayoutMethods.OrderBy(x => x.Value).OrderBy(x => x.Value).Select(x => x.Value).ToList();
            var newListString = string.Join(", ", newList);
            return string.Compare(newListString, model.CurrentPayoutMethodIds, true) != 0;
        }

        public int UpdatePayoutMethodByOwner(string owner, string payoutMethodName)
        {
            int updateCount = 0;
            try
            {
                if (string.IsNullOrEmpty(owner)) // case of removing payout method from property account
                {
                    var payoutMethodModel = _context.PayoutMethods.Where(x => x.PayoutMethodName == payoutMethodName).FirstOrDefault();
                    if (payoutMethodModel != null)
                    {
                        var entities = _context.PropertyAccountPayoutMethods.Where(x => x.PayoutMethodId == payoutMethodModel.PayoutMethodId);
                        if (entities != null && entities.Count() > 0)
                        {
                            _context.PropertyAccountPayoutMethods.RemoveRange(entities);
                            _context.SaveChanges();
                        }
                    }
                }
                else
                {
                    var entity = _context.PropertyAccounts.Where(x => x.OwnerName == owner).FirstOrDefault();
                    if (entity != null)
                    {
                        var entityId = entity.PropertyAccountId;
                        var payoutMethodEntity = (from pm in _context.PropertyAccountPayoutMethods.Where(x => x.PropertyAccountId == entityId)
                                                  join m in _context.PayoutMethods
                                                  on pm.PayoutMethodId equals m.PayoutMethodId
                                                  select m)
                                                 .FirstOrDefault();

                        if (payoutMethodEntity == null) // add only when it does not exist
                        {
                            var model = _context.PayoutMethods.Where(x => x.PayoutMethodName == payoutMethodName).FirstOrDefault();
                            if (model != null)
                            {
                                _context.PropertyAccountPayoutMethods.Add(new PropertyAccountPayoutMethod
                                {
                                    PropertyAccountId = payoutMethodEntity.PayoutMethodId,
                                    PayoutMethodId = entityId
                                });
                                _context.SaveChanges();

                                updateCount = 1;
                            }
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
