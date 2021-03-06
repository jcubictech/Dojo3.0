﻿using System.Collections.Generic;
using System.Linq;
using System.Data;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyFantasticMapProvider : CrudProviderBase<PropertyFantasticMap>
    {
        private readonly DojoDbContext _context;

        public PropertyFantasticMapProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<PropertyFantasticMap> All()
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

        public bool AddOrUpdate(Senstay.Dojo.Fantastic.PropertyMap map, bool commit = false)
        {
            bool changed = true;
            var entity = _context.PropertyFantasticMaps.Where(x => x.PropertyCode == map.nickname).FirstOrDefault();
            if (entity != null) // update
            {
                if (entity.ListingId != map.id) // only update if listing id has changed
                {
                    entity.ListingId = map.id;
                    this.Update(entity.PropertyFantasticMapId, entity);
                }
                else
                    changed = false;
            }
            else // create
            {
                entity = new PropertyFantasticMap { PropertyCode = map.nickname, ListingId = map.id };
                this.Create(entity);
            }

            if (changed && commit) _context.SaveChanges();

            return changed;
        }

        public bool Exist(int id)
        {
            return _context.PropertyFantasticMaps.FirstOrDefault(p => p.PropertyFantasticMapId == id) != null;
        }

        public bool Exist(string propertyCode, string listingId)
        {
            return _context.PropertyFantasticMaps.FirstOrDefault(p => p.PropertyCode == propertyCode && p.ListingId == listingId) != null;
        }

        public string GetPropertyCodeById(string listingId)
        {
            var entity = _context.PropertyFantasticMaps.FirstOrDefault(p => p.ListingId == listingId);
            if (entity != null)
                return entity.PropertyCode;
            else
                return null;
        }
    }
}
