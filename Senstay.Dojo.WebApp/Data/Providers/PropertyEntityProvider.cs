using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.Entity;
using System.Web.Mvc;
using System.Data.SqlClient;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyEntityProvider : CrudProviderBase<PropertyEntity>
    {
        private readonly DojoDbContext _context;

        public PropertyEntityProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<PropertyEntityViewModel> All()
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                var entities = _context.Database.SqlQuery<PropertyEntityViewModel>("RetrievePropertyEntities", sqlParams).ToList();
                foreach (var entity in entities)
                {
                    entity.EffectiveDate = ConversionHelper.EnsureUtcDate(entity.EffectiveDate.Date);
                    if (entity.CurrentPropertyCodes != null)
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
            return _context.PropertyEntities.FirstOrDefault(p => p.PropertyEntityId == id) != null;
        }

        public void MapData(PropertyEntityViewModel model, ref PropertyEntity entity)
        {
            entity.EntityName = model.EntityName;
            entity.EffectiveDate = ConversionHelper.EnsureUtcDate(model.EffectiveDate.Date);
        }

        public List<PropertyCodePropertyEntity> PropertyToUpdate(int entityId, ICollection<SelectListItem> propertyCodes)
        {
            var oldPropertyEntities = _context.PropertyCodePropertyEntities.Where(x => x.PropertyEntityId == entityId).ToList();
            var oldProperties = oldPropertyEntities.OrderBy(x => x.PropertyCode).Select(x => x.PropertyCode).ToList();
            var newProperties = propertyCodes.OrderBy(x => x.Value).Select(x => x.Value).ToArray();
            if (string.Join(",", oldProperties) != string.Join(",", newProperties))
                return oldPropertyEntities;
            else
                return null;
        }

        public int UpdatePropertyCodeByName(string name, string propertyCode)
        {
            int updateCount = 0;
            try
            {
                if (string.IsNullOrEmpty(name)) // case of removing property code from payout entity
                {
                    var entities = _context.PropertyCodePropertyEntities.Where(x => x.PropertyCode == propertyCode);
                    if (entities != null && entities.Count() > 0)
                    {
                        _context.PropertyCodePropertyEntities.RemoveRange(entities);
                        _context.SaveChanges();
                    }
                }
                else
                {
                    var entity = _context.PropertyEntities.Where(x => x.EntityName == name).FirstOrDefault();
                    if (entity != null)
                    {
                        var entityId = entity.PropertyEntityId;
                        var count = (from pe in _context.PropertyCodePropertyEntities.Where(x => x.PropertyCode == propertyCode)
                                     join e in _context.PropertyEntities
                                     on pe.PropertyEntityId equals e.PropertyEntityId
                                     select e.PropertyEntityId)
                                    .Count();

                        if (count == 0) // add only when it does not exist
                        {
                            _context.PropertyCodePropertyEntities.Add(new PropertyCodePropertyEntity
                            {
                                PropertyCode = propertyCode,
                                PropertyEntityId = entityId
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

        public string GetEntityName(string propertyCode, DateTime date)
        {
            var name = (from e in _context.PropertyCodePropertyEntities.Where(x => x.PropertyCode == propertyCode)
                        join p in _context.PropertyEntities
                        on e.PropertyEntityId equals p.PropertyEntityId
                        select new
                        {
                            EntityName = p.EntityName,
                            EffectiveDate = p.EffectiveDate
                        })
                       .Where(x => DbFunctions.TruncateTime(x.EffectiveDate) <= date)
                       .OrderByDescending(x => x.EffectiveDate)
                       .FirstOrDefault();

            return name != null ? name.EntityName : string.Empty;
        }
    }
}
