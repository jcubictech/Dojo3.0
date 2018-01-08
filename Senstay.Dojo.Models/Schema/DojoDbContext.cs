using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNet.Identity.EntityFramework;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Models
{
    public partial class DojoDbContext : IdentityDbContext<ApplicationUser>
    {
        public DojoDbContext()
            : base(AppConstants.DOJO_CONNECTION_NAME, throwIfV1Schema: false)
        {
            // turn on loading related tables for linq query
            this.Configuration.LazyLoadingEnabled = true;
        }

        public static DojoDbContext Create()
        {
            return new DojoDbContext();
        }

        public override int SaveChanges()
        {
            //auto call the modified date in defined entities
            UpdateChangedDateTimestamps();
            
            // Aduit loggin bits
            var entities = new List<System.Data.Entity.Infrastructure.DbEntityEntry>();
            bool isAdd = false;

            // get a collection of changed entries
            foreach (var ent in this.ChangeTracker.Entries().Where(p => AuditLogging.changeStates.Contains(p.State)))
            {
                isAdd = ent.State == EntityState.Added;
                entities.Add(ent);
            }

            // pre-commit the Added items so we can get the Identity
            if (isAdd)
            {
                base.SaveChanges();
            }

            // create a Audit Log item
            AuditLogging.WriteAuditLog(entities, this.AuditLog);

            return base.SaveChanges();
        }

        private void UpdateChangedDateTimestamps()
        {
            string currentUserName = ClaimsPrincipal.Current.Identity.Name;
            var changedAtEntities = ChangeTracker.Entries()
                                                 .Where(i => i.State != EntityState.Unchanged &&
                                                            (i.Entity is CPL ||
                                                             i.Entity is AirbnbAccount ||
                                                             i.Entity is InquiriesValidation ||
                                                             i.Entity is OwnerPayout ||
                                                             i.Entity is Reservation ||
                                                             i.Entity is Resolution ||
                                                             i.Entity is FutureReservation ||
                                                             i.Entity is FutureResolution ||
                                                             i.Entity is OtherRevenue ||
                                                             i.Entity is GrossEarning ||
                                                             i.Entity is Expense));

            foreach (var entity in changedAtEntities)
            {
                if (entity.State == EntityState.Added)
                {
                    if (entity.Entity is InquiriesValidation)
                    {
                        ((InquiriesValidation)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((InquiriesValidation)entity.Entity).CreatedBy = currentUserName;
                    }
                    if (entity.Entity is AirbnbAccount)
                    {
                        ((AirbnbAccount)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((AirbnbAccount)entity.Entity).CreatedBy = currentUserName;
                    }
                    if (entity.Entity is CPL)
                    {
                        ((CPL)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((CPL)entity.Entity).CreatedBy = currentUserName;
                    }
                    if (entity.Entity is OwnerPayout)
                    {
                        ((OwnerPayout)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((OwnerPayout)entity.Entity).CreatedBy = currentUserName;
                    }
                    if (entity.Entity is Reservation)
                    {
                        ((Reservation)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((Reservation)entity.Entity).CreatedBy = currentUserName;
                    }
                    if (entity.Entity is Resolution)
                    {
                        ((Resolution)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((Resolution)entity.Entity).CreatedBy = currentUserName;
                    }
                    if (entity.Entity is FutureReservation)
                    {
                        ((FutureReservation)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((FutureReservation)entity.Entity).CreatedBy = currentUserName;
                    }
                    if (entity.Entity is FutureResolution)
                    {
                        ((FutureResolution)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((FutureResolution)entity.Entity).CreatedBy = currentUserName;
                    }
                    if (entity.Entity is Expense)
                    {
                        ((Expense)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((Expense)entity.Entity).CreatedBy = currentUserName;
                    }
                    if (entity.Entity is GrossEarning)
                    {
                        ((GrossEarning)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((GrossEarning)entity.Entity).CreatedBy = currentUserName;
                    }
                    if (entity.Entity is OtherRevenue)
                    {
                        ((OtherRevenue)entity.Entity).CreatedDate = DateTime.UtcNow;
                        ((OtherRevenue)entity.Entity).CreatedBy = currentUserName;
                    }
                }

                if (entity.Entity is InquiriesValidation)
                {
                    ((InquiriesValidation)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((InquiriesValidation)entity.Entity).ModifiedBy = currentUserName;
                }

                if (entity.Entity is AirbnbAccount)
                {
                    ((AirbnbAccount)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((AirbnbAccount)entity.Entity).ModifiedBy = currentUserName;
                }
                if (entity.Entity is CPL)
                {
                    ((CPL)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((CPL)entity.Entity).ModifiedBy = currentUserName;
                }
                if (entity.Entity is OwnerPayout)
                {
                    ((OwnerPayout)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((OwnerPayout)entity.Entity).ModifiedBy = currentUserName;
                }
                if (entity.Entity is Reservation)
                {
                    ((Reservation)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((Reservation)entity.Entity).ModifiedBy = currentUserName;
                }
                if (entity.Entity is Resolution)
                {
                    ((Resolution)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((Resolution)entity.Entity).ModifiedBy = currentUserName;
                }
                if (entity.Entity is FutureReservation)
                {
                    ((FutureReservation)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((FutureReservation)entity.Entity).ModifiedBy = currentUserName;
                }
                if (entity.Entity is FutureResolution)
                {
                    ((FutureResolution)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((FutureResolution)entity.Entity).ModifiedBy = currentUserName;
                }
                if (entity.Entity is Expense)
                {
                    ((Expense)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((Expense)entity.Entity).ModifiedBy = currentUserName;
                }
                if (entity.Entity is GrossEarning)
                {
                    ((GrossEarning)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((GrossEarning)entity.Entity).ModifiedBy = currentUserName;
                }
                if (entity.Entity is OtherRevenue)
                {
                    ((OtherRevenue)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((OtherRevenue)entity.Entity).ModifiedBy = currentUserName;
                }
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // cascade deletion is enabled by EF 6 as default...
            //modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            modelBuilder.Entity<NewFeature>().Property(x => x.NewFeatureId).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            base.OnModelCreating(modelBuilder); // will initialize UserManager

            //modelBuilder.Configurations.Add(new AirbnbAccountMap());
            //modelBuilder.Configurations.Add(new BookingMap());
            //modelBuilder.Configurations.Add(new CPLMap());
            //modelBuilder.Configurations.Add(new InquiriesValidationMap());

            //modelBuilder.Configurations.Add(new AspNetRoleMap());
            //modelBuilder.Configurations.Add(new AspNetUserClaimMap());
            //modelBuilder.Configurations.Add(new AspNetUserLoginMap());
            //modelBuilder.Configurations.Add(new AspNetUserRoleMap());
            //modelBuilder.Configurations.Add(new AspNetUserMap());
        }

        public DbSet<AirbnbAccount> AirbnbAccounts { get; set; }

        public DbSet<CPL> CPLs { get; set; }

        public DbSet<InquiriesValidation> InquiriesValidations { get; set; }

        public DbSet<AuditLog> AuditLog { get; set; }

        public DbSet<DojoLog> DojoLogs { get; set; }

        public DbSet<NewFeature> NewFeatures { get; set; }

        public DbSet<InputError> InputErrors { get; set; }

        public DbSet<GrossEarning> GrossEarnings { get; set; }

        public DbSet<Expense> Expenses { get; set; }

        public DbSet<JobCost> JobCosts { get; set; }

        public DbSet<Lookup> Lookups { get; set; }

        public DbSet<OtherRevenue> OtherRevenues { get; set; }

        public DbSet<OwnerPayout> OwnerPayouts { get; set; }

        public DbSet<OwnerStatement> OwnerStatements { get; set; }

        public DbSet<PayoutMethod> PayoutMethods { get; set; }

        public DbSet<PayoutPayment> PayoutPayments { get; set; }

        public DbSet<PropertyPayoutMethod> PropertyPayoutMethods { get; set; }

        public DbSet<PropertyAccount> PropertyAccounts { get; set; }

        public DbSet<PropertyAccountPayoutMethod> PropertyAccountPayoutMethods { get; set; }

        public DbSet<PropertyEntity> PropertyEntities { get; set; }

        public DbSet<PropertyCodePropertyEntity> PropertyCodePropertyEntities { get; set; }

        public DbSet<PropertyFee> PropertyFees { get; set; }

        public DbSet<PropertyBalance> PropertyBalances { get; set; }

        public DbSet<PropertyTitleHistory> PropertyTitleHistories { get; set; }

        public DbSet<Reservation> Reservations { get; set; }

        public DbSet<Resolution> Resolutions { get; set; }

        public DbSet<FutureReservation> FutureReservations { get; set; }

        public DbSet<FutureResolution> FutureResolutions { get; set; }

        public DbSet<ReportMapping> ReportMappings { get; set; }

        public DbSet<StatementCompletion> StatementCompletions { get; set; }
    }
}
