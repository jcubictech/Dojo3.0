namespace Senstay.Dojo.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Senstay.Dojo.Models.DojoDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            MigrationsDirectory = @"Migrations";
        }

        protected override void Seed(Senstay.Dojo.Models.DojoDbContext context)
        {
            //  This method will be called after migrating to the latest version.
            //Senstay.Dojo.Data.SeedData seed = new Data.SeedData(context);
            //seed.Execute();
        }
    }
}
