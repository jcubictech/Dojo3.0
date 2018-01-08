using System;
using System.Linq;
using Senstay.Dojo.Models;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace Senstay.Dojo.Data.Providers
{
    public interface ICrud<T> where T : class
    {
        IQueryable<T> GetAll();
        T Retrieve(int id);
        T Retrieve(string id);
        void Create(T entity);
        void Update(int id, T entity);
        void Update(string id, T entity);
        void Delete(int id);
        void Delete(string id);
        void Delete(T entity);
        void Commit();
    }

    /// <summary>
    /// The EF-dependent, generic CRUD provider for data access
    /// </summary>
    /// <typeparam name="T">Type of entity for this Repository.</typeparam>
    public class CrudProviderBase<T> : ICrud<T> where T : class
    {
        public CrudProviderBase(DojoDbContext dbContext, bool isDelegating = false)
        {
            if (dbContext == null) throw new ArgumentNullException("Null DbContext");
            DbContext = dbContext;
            if (!isDelegating)
                DbSet = DbContext.Set<T>();
            else
                DbSet = null;
        }

        protected DojoDbContext DbContext { get; set; }

        protected DbSet<T> DbSet { get; set; }

        public virtual IQueryable<T> GetAll()
        {
            return DbSet;
        }

        public virtual void Create(T entity)
        {
            DbEntityEntry dbEntityEntry = DbContext.Entry(entity);
            if (dbEntityEntry.State != EntityState.Detached)
            {
                dbEntityEntry.State = EntityState.Added;
            }
            else
            {
                DbSet.Add(entity);
            }
        }

        public virtual T Retrieve(int id)
        {
            return DbSet != null ? DbSet.Find(id) : null;
        }

        public virtual T Retrieve(string id)
        {
            return DbSet != null ? DbSet.Find(id) : null;
        }

        public virtual void Update(int id, T entity)
        {
            T oldEntity = Retrieve(id);
            if (oldEntity != null)
            {
                DbContext.Entry(oldEntity).CurrentValues.SetValues(entity);
            }
        }

        public virtual void Update(string id, T entity)
        {
            T oldEntity = Retrieve(id);
            if (oldEntity != null)
            {
                DbContext.Entry(oldEntity).CurrentValues.SetValues(entity);
            }
        }

        public virtual void Delete(int id)
        {
            var entity = Retrieve(id);
            if (entity == null) return; // not found; assume already deleted.
            Delete(entity);
        }

        public virtual void Delete(string id)
        {
            var entity = Retrieve(id);
            if (entity == null) return; // not found; assume already deleted.
            Delete(entity);
        }

        public virtual void Delete(T entity)
        {
            DbEntityEntry dbEntityEntry = DbContext.Entry(entity);
            if (dbEntityEntry.State != EntityState.Deleted)
            {
                dbEntityEntry.State = EntityState.Deleted;
            }
            else
            {
                DbSet.Attach(entity);
                DbSet.Remove(entity);
            }
        }

        public virtual void Commit()
        {
            DbContext.SaveChanges();
        }
    }
}