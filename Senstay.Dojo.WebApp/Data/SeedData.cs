using System;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Linq;
using Senstay.Dojo.Infrastructure.Tasks;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;
using System.Collections.Generic;
using System.Data.Entity.Migrations;

namespace Senstay.Dojo.Data
{
    public class SeedData : IRunAtStartup
    {
        private readonly DojoDbContext _dbContext;

        public SeedData(DojoDbContext context)
        {
            _dbContext = context;
        }

        public void Execute()
        {
            // seed data during dojo 2.0 initial rollout
            //SeedSuperAccount();
            //SeedAppRoles();

            //SeedVerticals();
            //SeedCities();
            //SeedMarkets();
            //SeedChannels();
            //SeedAreas();
            //SeedNeighborhoods();
            //SeedBelts();
            //SeedCurrenies();
            //SeedAbbreviatedStates();
            //SeedYesNos();
            //SeedYesNoNas();
            //SeedApprovals();
            //SeedDecisions();
            //AddFeature_20170502();
            //SeedRevenueRoles();
            //SeedExpenseCategories();
            //SeedImpacts();
            //SeedChannels();
            //SeedSenstayPayoutAccounts();
            //SeedCauses();
            //SeedPropertyStatuses();
            //SeedFinancialRoles();
            //SeedStatementCompletions();
            //SeedPricingRoles();
        }

        public void SeedPricingRoles()
        {
            var roleManager = new RoleManager<ApplicationRole>(new RoleStore<ApplicationRole>(_dbContext));

            if (!roleManager.RoleExists(AppConstants.PRICING_ADMIN_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.PRICING_ADMIN_ROLE });
            if (!roleManager.RoleExists(AppConstants.PRICING_EDITOR_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.PRICING_EDITOR_ROLE });
        }

        public void SeedStatementCompletions()
        {
            var list = new List<StatementCompletion>()
            {
                new Models.StatementCompletion() { Month = 7, Year = 2017, Completed = true },
                new Models.StatementCompletion() { Month = 8, Year = 2017, Completed = true },
                new Models.StatementCompletion() { Month = 9, Year = 2017, Completed = true },
                new Models.StatementCompletion() { Month = 10, Year = 2017, Completed = true },
                new Models.StatementCompletion() { Month = 11, Year = 2017, Completed = true },
                new Models.StatementCompletion() { Month = 12, Year = 2017, Completed = false },
            };

            foreach(StatementCompletion  model in list)
            {
                if (_dbContext.StatementCompletions.Where(x => x.Month == model.Month && x.Year == model.Year).FirstOrDefault() == null)
                {
                    _dbContext.StatementCompletions.Add(model);
                }
            }
            _dbContext.SaveChanges();
        }

        public void SeedFinancialRoles()
        {
            var roleManager = new RoleManager<ApplicationRole>(new RoleStore<ApplicationRole>(_dbContext));

            if (!roleManager.RoleExists(AppConstants.FINANCIAL_ADMIN_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.FINANCIAL_ADMIN_ROLE });
        }

        public void SeedCauses()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.Cause.ToString(), Name = "Cleanliness" },
                new Models.Lookup() { Type = LookupType.Cause.ToString(), Name = "Maintenance Issue" },
                new Models.Lookup() { Type = LookupType.Cause.ToString(), Name = "Check-in Issue" },
                new Models.Lookup() { Type = LookupType.Cause.ToString(), Name = "Inaccuracy in Listing" },
                new Models.Lookup() { Type = LookupType.Cause.ToString(), Name = "City Management" },
                new Models.Lookup() { Type = LookupType.Cause.ToString(), Name = "Other" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedSenstayPayoutAccounts()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.SenStayAccount.ToString(), Name = "00250069146" },
                new Models.Lookup() { Type = LookupType.SenStayAccount.ToString(), Name = "00250074271" },
                new Models.Lookup() { Type = LookupType.SenStayAccount.ToString(), Name = "00250074301" },
                new Models.Lookup() { Type = LookupType.SenStayAccount.ToString(), Name = "00250074336" },
                new Models.Lookup() { Type = LookupType.SenStayAccount.ToString(), Name = "00250074344" },
                new Models.Lookup() { Type = LookupType.SenStayAccount.ToString(), Name = "00250074352" },
                new Models.Lookup() { Type = LookupType.SenStayAccount.ToString(), Name = "00250074360" },
                new Models.Lookup() { Type = LookupType.SenStayAccount.ToString(), Name = "00250074379" },
                new Models.Lookup() { Type = LookupType.SenStayAccount.ToString(), Name = "00250074387" },
                new Models.Lookup() { Type = LookupType.SenStayAccount.ToString(), Name = "00250069421" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedImpacts()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.Impact.ToString(), Name = "Adjustment" },
                new Models.Lookup() { Type = LookupType.Impact.ToString(), Name = "Advance Payout" },
                new Models.Lookup() { Type = LookupType.Impact.ToString(), Name = "Client Expense" },
                new Models.Lookup() { Type = LookupType.Impact.ToString(), Name = "SenStay Expense" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedExpenseCategories()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Cleaning" },
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Consumables" },
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Furnishings" },
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Laundry" },
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Maintenance" },
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Move Out Costs" },
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Postage" },
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Rent" },
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Utilities" },
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Debit To Property" },
                new Models.Lookup() { Type = LookupType.ExpenseCategory.ToString(), Name = "Credit From Property" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedRevenueRoles()
        {
            var roleManager = new RoleManager<ApplicationRole>(new RoleStore<ApplicationRole>(_dbContext));

            if (!roleManager.RoleExists(AppConstants.REVENUE_VIEWER_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.REVENUE_VIEWER_ROLE });
            if (!roleManager.RoleExists(AppConstants.REVENUE_ADMIN_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.REVENUE_ADMIN_ROLE });
            if (!roleManager.RoleExists(AppConstants.REVENUE_APPROVER_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.REVENUE_APPROVER_ROLE });
            if (!roleManager.RoleExists(AppConstants.REVENUE_CREATOR_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.REVENUE_CREATOR_ROLE });
            if (!roleManager.RoleExists(AppConstants.REVENUE_FINALIZER_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.REVENUE_FINALIZER_ROLE });
            if (!roleManager.RoleExists(AppConstants.REVENUE_OWNER_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.REVENUE_OWNER_ROLE });
            if (!roleManager.RoleExists(AppConstants.REVENUE_REVIEWER_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.REVENUE_REVIEWER_ROLE });
            if (!roleManager.RoleExists(AppConstants.DATA_IMPORTER_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.DATA_IMPORTER_ROLE });
            if (!roleManager.RoleExists(AppConstants.STATEMENT_ADMIN_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.STATEMENT_ADMIN_ROLE });
            if (!roleManager.RoleExists(AppConstants.STATEMENT_OWNER_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.STATEMENT_OWNER_ROLE });
            if (!roleManager.RoleExists(AppConstants.STATEMENT_VIEWER_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.STATEMENT_VIEWER_ROLE });
            if (!roleManager.RoleExists(AppConstants.STATEMENT_EDITOR_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.STATEMENT_EDITOR_ROLE });
        }

        private void AddFeature_20170502()
        {
            try
            {
                string newFesatureTemplate = "<div><b>New Feature Announcement – {0}</b></div>" +
                                             "<div>We have made the following improvements to the Dojo Web Application:</div>{1}" +
                                             "<div>We hope you enjoy these new features. Let us know if you have any suggestion so we can continue to improve our app and processes.</div>" +
                                             "<div><a href=\"mailto:%20jason.jou@senstay.com\" target=\"_blank\">Dojo support team</a></div>";

                string newFeatureContent = "<ul>" +
                                           "<li><b>Improvements</b>:<ul>" +
                                                "<li><b style=\"color:red\">New <i class=\"fa fa-flash red\"></i>: </b>You can now save your favorite page as the start page when you start out a Dojo day. Just click the <i class=\"fa fa-heart-o\"></i> icon when it appears on the menu.</li>" +
                                                "<li><b style=\"color:red\">New <i class=\"fa fa-flash red\"></i>: </b>Allow outstanding balance in property input form to take negative value.</li>" +
                                                "<li>To improve response time, property retrieval will initially contain only active and pending ones. The inactive and dead ones can then be retrieved on-demand. Market fitlers are removed to give screen space for this new feature.</li>" +
                                                "<li>There will be a message displayed at the bottom right of the browser window as a result of a creation, update, or delete operation. The message will remove itself after 8 seconds.</li>" +
                                                "<li>Rearrange inquiry grid columns to favor frequently viewed columns.</li>" +
                                                "<li>Data entry for check-in and check-out date in inquiry form can now guarantee that they will be properly ordered during data entry.</li>" +
                                                "<li>Date range in action bar can now guarantee that they will be properly ordered during data entry.</li>" +
                                                "<li>Dropdown menu is now activated by hovering over.</li>" +
                                                "</ul></li>" +
                                           "<li><b>Bug fixes</b>:<ul>" +
                                                "<li>Resovle inquiry grid column mis-alignment issue in Firefox browser.</li>" +
                                                "<li>Resolve issue that the dropdown menu becomes unresponsive after a popup window is closed.</li>" +
                                                "<li><b style=\"color:red\">New <i class=\"fa fa-flash red\"></i>: </b>Resolve issue that inquiry property popup window cannot be closed by clicking outside of it after inquiry was added or edited first.</li>" +
                                                "</ul></li>" +
                                           "</ul>";

                DateTime deployDate = new DateTime(2017, 5, 4);
                NewFeature newFeature = new NewFeature
                {
                    NewFeatureId = 2,
                    DeployDate = deployDate,
                    ExpiredDate = deployDate.AddDays(14),
                    Description = string.Format(newFesatureTemplate, deployDate.ToString("MM/dd"), newFeatureContent)
                };

                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    _dbContext.NewFeatures.AddOrUpdate(x => new { NewFeatureId = x.NewFeatureId }, newFeature);
                    //_dbContext.Database.ExecuteSqlCommand("SET IDENTITY_INSERT [dbo].[NewFeature] ON;");
                    _dbContext.SaveChanges();
                    //_dbContext.Database.ExecuteSqlCommand("SET IDENTITY_INSERT [dbo].[NewFeature] OFF;");
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                // TODO: log error
                Exception inner = ex.InnerException;
            }
        }

        private void AddFeature_20170415()
        {
            try
            {
                string newFesatureTemplate = "<div><b>New Feature Announcement – {0}</b></div>" +
                                             "<div><b>Great News!</b> We have added the following new features to the Dojo Web Application:</div>{1}" +
                                             "<div>We hope you enjoy these new features. Let us know if you have any suggestion so we can continue to improve our app and processes.</div>" +
                                             "<div><a href=\"mailto:%20jason.jou@senstay.com\" target=\"_blank\">Dojo support team</a></div>";

                string newFeatureContent = "<ul>" +
                                           "<li><b>Inquiry Deletion by Administrator</b>: Dojo administrator can now delete inquiry on behave of another user. This feature will come in handy if there are duplicate entries in inquiry database and the inquiry owner is not available.</li>" +
                                           "<li><b>Inquiry Duplication Detection</b>: Inquiry editor will receive a message if duplication is detected when saving an inquiry. A duplication is defined as when the property code, guest name, check-in date, and check-out date of an inquiry already exists in Dojo database.</li>" +
                                           "<li><b>Inquiry Property Code Link</b>: Inquiry editor can view property related information by clicking the property code from inquiry grid.</li>" +
                                           "<li><b>Close Inquiry View</b>: Inquiry editor can now close inquiry view and property view window by clicking anywhere outside of the popup window.</li>" +
                                           "<li><b>Auto-populate Inquiry Team and Approver Field</b>: Current user name is auto-populated to <i>Inquiry Team</i> and <i>Approver</i> field when creating new inquiry. These two fields are now read-only.</li>" +
                                           "<li><b>Improvements</b>:<ul>" +
                                                "<li>Re-arrange inquiry grid columns to favor frequently viewed columns.</li>" +
                                                "<li>Property address text formatting is improved for google map to show more correct geographical location.</li>" +
                                                "<li>Airbnb, HomeAway, and Streamline link in <i>Property</i> page can now be generated from their respective ID or code.</li>" +
                                                "</ul></li>" +
                                           "<li><b>Bug fixes</b>:<ul>" +
                                                "<li>Resovle missing data in <i>Pricing Reason</i> column in inquiry page.</li>" +
                                                "<li>Resolve broken Airbnb link after property form is submitted.</li>" +
                                                "</ul></li>" +
                                           "</ul>";

                DateTime deployDate = new DateTime(2017, 4, 24);
                NewFeature newFeature = new NewFeature
                {
                    NewFeatureId = 1,
                    DeployDate = deployDate,
                    ExpiredDate = deployDate.AddDays(14),
                    Description = string.Format(newFesatureTemplate, deployDate.ToString("MM/dd"), newFeatureContent)
                };

                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    _dbContext.NewFeatures.AddOrUpdate(x => new { NewFeatureId = x.NewFeatureId }, newFeature);
                    _dbContext.Database.ExecuteSqlCommand("SET IDENTITY_INSERT [dbo].[NewFeature] ON;");
                    _dbContext.SaveChanges();
                    _dbContext.Database.ExecuteSqlCommand("SET IDENTITY_INSERT [dbo].[NewFeature] OFF;");
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                // TODO: log error
                Exception inner = ex.InnerException;
            }
        }

        public void SeedInquiryTeams()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.InquiryTeam.ToString(), Name = "Genny Young" },
                new Models.Lookup() { Type = LookupType.InquiryTeam.ToString(), Name = "Steven Lee" },
                new Models.Lookup() { Type = LookupType.InquiryTeam.ToString(), Name = "Sierra Dehmler" },
                new Models.Lookup() { Type = LookupType.InquiryTeam.ToString(), Name = "Yann Thiollet" },
                new Models.Lookup() { Type = LookupType.InquiryTeam.ToString(), Name = "Sasha Rosewood" },
                new Models.Lookup() { Type = LookupType.InquiryTeam.ToString(), Name = "Rob Fan" },
                new Models.Lookup() { Type = LookupType.InquiryTeam.ToString(), Name = "Eric Zhang" },

                new Models.Lookup() { Type = LookupType.PriceApprover.ToString(), Name = "Genny Young" },
                new Models.Lookup() { Type = LookupType.PriceApprover.ToString(), Name = "Steven Lee" },
                new Models.Lookup() { Type = LookupType.PriceApprover.ToString(), Name = "Sierra Dehmler" },
                new Models.Lookup() { Type = LookupType.PriceApprover.ToString(), Name = "Yann Thiollet" },
                new Models.Lookup() { Type = LookupType.PriceApprover.ToString(), Name = "Sasha Rosewood" },
                new Models.Lookup() { Type = LookupType.PriceApprover.ToString(), Name = "Rob Fan" },
                new Models.Lookup() { Type = LookupType.PriceApprover.ToString(), Name = "Eric Zhang" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedPaymentMethods()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.PaymentMethod.ToString(), Name = "Visa" },
                new Models.Lookup() { Type = LookupType.PaymentMethod.ToString(), Name = "MasterCard" },
                new Models.Lookup() { Type = LookupType.PaymentMethod.ToString(), Name = "AE" },
                new Models.Lookup() { Type = LookupType.PaymentMethod.ToString(), Name = "Discovery" },
                new Models.Lookup() { Type = LookupType.PaymentMethod.ToString(), Name = "Cash" },
                new Models.Lookup() { Type = LookupType.PaymentMethod.ToString(), Name = "Wire" },
                new Models.Lookup() { Type = LookupType.PaymentMethod.ToString(), Name = "Paypal" },
                new Models.Lookup() { Type = LookupType.PaymentMethod.ToString(), Name = "Other" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedDecisions()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.PriceDecision.ToString(), Name = "Yes" },
                new Models.Lookup() { Type = LookupType.PriceDecision.ToString(), Name = "No" },
                new Models.Lookup() { Type = LookupType.PriceDecision.ToString(), Name = "Counter Offer" },
                new Models.Lookup() { Type = LookupType.PriceDecision.ToString(), Name = "Tetris" },
                new Models.Lookup() { Type = LookupType.PriceDecision.ToString(), Name = "Need More Info" },
                new Models.Lookup() { Type = LookupType.PriceDecision.ToString(), Name = "Hold" },
                new Models.Lookup() { Type = LookupType.PriceDecision.ToString(), Name = "Too Far Out" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedBelts()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.Belt.ToString(), Name = "Black Belt" },
                new Models.Lookup() { Type = LookupType.Belt.ToString(), Name = "White Belt" },
                new Models.Lookup() { Type = LookupType.Belt.ToString(), Name = "Yellow Belt" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedCurrenies()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.Currency.ToString(), Name = "USD ($)" },
                new Models.Lookup() { Type = LookupType.Currency.ToString(), Name = "Real (R$)" },
                new Models.Lookup() { Type = LookupType.Currency.ToString(), Name = "Euro (€)" },
                new Models.Lookup() { Type = LookupType.Currency.ToString(), Name = "Pound (£)" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedYesNos()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.YesNo.ToString(), Name = "Yes" },
                new Models.Lookup() { Type = LookupType.YesNo.ToString(), Name = "No" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedYesNoNas()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.YesNoNa.ToString(), Name = "Yes" },
                new Models.Lookup() { Type = LookupType.YesNoNa.ToString(), Name = "No" },
                new Models.Lookup() { Type = LookupType.YesNoNa.ToString(), Name = "N/A" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedPropertyStatuses()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.PropertyStatus.ToString(), Name = "Active" },
                new Models.Lookup() { Type = LookupType.PropertyStatus.ToString(), Name = "Active-Airbnb" },
                new Models.Lookup() { Type = LookupType.PropertyStatus.ToString(), Name = "Active-Full" },
                new Models.Lookup() { Type = LookupType.PropertyStatus.ToString(), Name = "Active-Shell" },
                new Models.Lookup() { Type = LookupType.PropertyStatus.ToString(), Name = "Inactive" },
                new Models.Lookup() { Type = LookupType.PropertyStatus.ToString(), Name = "Pending-Onboarding" },
                new Models.Lookup() { Type = LookupType.PropertyStatus.ToString(), Name = "Pending-Contract" },
                new Models.Lookup() { Type = LookupType.PropertyStatus.ToString(), Name = "Dead" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedApprovals()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.Approval.ToString(), Name = "Yes" },
                new Models.Lookup() { Type = LookupType.Approval.ToString(), Name = "No" },
                new Models.Lookup() { Type = LookupType.Approval.ToString(), Name = "Pending" },
                new Models.Lookup() { Type = LookupType.Approval.ToString(), Name = "N/A" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedVerticals()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.Vertical.ToString(), Name = "CO" },
                new Models.Lookup() { Type = LookupType.Vertical.ToString(), Name = "FS" },
                new Models.Lookup() { Type = LookupType.Vertical.ToString(), Name = "HO" },
                new Models.Lookup() { Type = LookupType.Vertical.ToString(), Name = "JV" },
                new Models.Lookup() { Type = LookupType.Vertical.ToString(), Name = "RS" },
            };

            // type + name must be unique
            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedCities()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Anaheim" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Beverly Hills" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Boston" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Cabo San Lucas" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Denver" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Florianópolis" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Goodyear" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Grand Rapids" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Hollywood" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Hollywood Hills" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Indio" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Jersey City" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Jurere Tradicional" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "La Quinta" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Laguna Beach" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Las Vegas" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Los Angeles" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Malibu" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Manhattan Beach" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Marina Del Rey" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Menezes" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Miami" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "New York" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Palm Springs" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Paris" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Phoenix" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Playa Del Carmen" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Rio De Janeiro" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "San Diego" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "San Francisco" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Santa Monica" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Sao Paulo" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Scottsdale" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Southampton" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Venice" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "West Hollywood" },
                new Models.Lookup() { Type = LookupType.City.ToString(), Name = "Westwood" },
            };

            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedAbbreviatedStates()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "AL" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "AK" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "AZ" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "AR" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "CA" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "CO" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "CT" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "DE" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "FL" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "GA" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "HI" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "ID" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "IL" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "IN" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "IA" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "KS" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "KY" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "LA" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "ME" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "MD" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "MA" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "MI" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "MN" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "MS" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "MO" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "MT" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "NE" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "NV" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "NH" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "NJ" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "NM" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "NY" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "NC" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "ND" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "OH" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "OK" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "OR" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "PA" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "RI" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "SC" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "SD" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "TN" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "TX" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "UT" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "VT" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "VA" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "WA" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "WV" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "WI" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "WY" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "GU" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "PR" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "VI" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "Baja California Sur" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "Colima" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "Quintana Roo" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "Rio De Janeiro" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "Santa Catarina" },
                new Models.Lookup() { Type = LookupType.AbbreviatedState.ToString(), Name = "Other (Non-US)" },
            };

            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedStates()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Alabama" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Alaska" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Arizona" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Arkansas" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "California" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Colorado" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Connecticut" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Delaware" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Florida" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Georgia" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Hawaii" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Idaho" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Illinois" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Indiana" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Iowa" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Kansas" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Kentucky" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Louisiana" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Maine" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Maryland" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Massachusetts" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Michigan" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Minnesota" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Mississippi" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Missouri" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Montana" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Nebraska" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Nevada" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "New Hampshire" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "New Jersey" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "New Mexico" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "New York" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "North Carolina" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "North Dakota" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Ohio" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Oklahoma" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Oregon" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Pennsylvania" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Rhode Island" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "South Carolina" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "South Dakota" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Tennessee" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Texas" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Utah" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Vermont" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Virginia" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Washington" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "West Virginia" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Wisconsin" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Wyoming" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Guam" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Puerto Rico" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Virgin Islands" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Colima" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Quintana Roo" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Rio De Janeiro" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Santa Catarina" },
                new Models.Lookup() { Type = LookupType.State.ToString(), Name = "Other (Non-US)" },
            };

            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();
        }

        public void SeedMarkets()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Boston" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Cabo San Lucas" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Denver" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Florianópolis" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Grand Rapids" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Las Vegas" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Los Angeles" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Miami" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "New York" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Paris" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Phoenix" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Playa Del Carmen" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Rio De Janeiro" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "San Diego" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "San Francisco" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Sao Paulo" },
                new Models.Lookup() { Type = LookupType.Market.ToString(), Name = "Southampton" },
            };

            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();

        }

        public void SeedChannels()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.Channel.ToString(), Name = "Airbnb" },
                new Models.Lookup() { Type = LookupType.Channel.ToString(), Name = "FlipKey" },
                new Models.Lookup() { Type = LookupType.Channel.ToString(), Name = "Direct" },
                new Models.Lookup() { Type = LookupType.Channel.ToString(), Name = "HomeAway" },
                new Models.Lookup() { Type = LookupType.Channel.ToString(), Name = "Booking.com" },
                new Models.Lookup() { Type = LookupType.Channel.ToString(), Name = "Owner" },
                new Models.Lookup() { Type = LookupType.Channel.ToString(), Name = "Maintenance" },
                new Models.Lookup() { Type = LookupType.Channel.ToString(), Name = "Privé" },
            };

            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();

        }

        public void SeedAreas()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Anaheim" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Boston" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Cabo San Lucas" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Denver" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "DTLA" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Florianópolis" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Grand Rapids" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Harlem" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "HollyHills" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "HollyWeho" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Jardins" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Jersey City" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "LA" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "LABeach" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Laguna Beach" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Las Vegas" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Lower Manhattan" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Malibu" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Miami" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Midtown East" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Midtown West" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Palm Springs" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Paris" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Phoenix" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Playa Del Carmen" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Rio De Janeiro" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "San Diego" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "San Francisco" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Scottsdale" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Southampton" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Upper East Side" },
                new Models.Lookup() { Type = LookupType.Area.ToString(), Name = "Upper West Side" },
            };

            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();

        }

        public void SeedNeighborhoods()
        {
            var list = new List<Lookup>()
            {
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Astoria" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Beverly Hills" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Boston" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Cabo San Lucas" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Canyon Trails West" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Chelsea" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Copacabana" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Creative District" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Denver" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "DTLA" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "East Harlem" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "East Village" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Financial District" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Flatiron" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Florianópolis" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Gavea" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Grand Rapids" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "HollyHills" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "HollyWeho" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Indio" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Ipanema" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Jardins" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Jefferson Park" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Jersey City" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "LA" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "La Quinta" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "LABeach" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Laguna Beach" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Las Vegas" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Leblon" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Leblon / Zona Sul" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Los Angeles" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Lower East Side" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Lower Manhattan" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Malibu" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Miami" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Mid-City" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Midtown East" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Midtown West" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Old Town" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Pacific Beach" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Palm Springs" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Paris" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Phoenix" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Playa Del Carmen" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Praia Mole" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Rio De Janeiro" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "San Francisco" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Scottsdale" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "SoHo" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Southampton" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "The Mission" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Tribeca" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Upper East Side" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "Upper West Side" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "West Hollywood" },
                new Models.Lookup() { Type = LookupType.Neighborhood.ToString(), Name = "West Village" },
            };

            _dbContext.Lookups.AddOrUpdate(x => new { Type = x.Type, Name = x.Name }, list.ToArray());
            _dbContext.SaveChanges();

        }

        public void SeedAppRoles()
        {
            var roleManager = new RoleManager<ApplicationRole>(new RoleStore<ApplicationRole>(_dbContext));

            if (!roleManager.RoleExists(AppConstants.EDITOR_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.EDITOR_ROLE });
            if (!roleManager.RoleExists(AppConstants.VIEWER_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.VIEWER_ROLE });
            if (!roleManager.RoleExists(AppConstants.APPROVER_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.APPROVER_ROLE });
            if (!roleManager.RoleExists(AppConstants.PROPERTY_EDITOR_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.PROPERTY_EDITOR_ROLE });
            if (!roleManager.RoleExists(AppConstants.ACCOUNT_EDITOR_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.ACCOUNT_EDITOR_ROLE });
            if (!roleManager.RoleExists(AppConstants.INQUIRY_EDITOR_ROLE)) roleManager.Create(new ApplicationRole { Name = AppConstants.INQUIRY_EDITOR_ROLE });
        }

        public void SeedSuperAccount()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(_dbContext));
            var roleManager = new RoleManager<ApplicationRole>(new RoleStore<ApplicationRole>(_dbContext));

            var superUser = userManager.FindByName(AppConstants.SUPER_ADMIN_ROLE);
            if (superUser == null)
            {
                var user = new ApplicationUser()
                {
                    UserName = AppConstants.SUPER_ADMIN_ROLE,
                    Email = SettingsHelper.GetSafeSetting("SuperUser"),
                    EmailConfirmed = true,
                };
                IdentityResult result = userManager.Create(user, "Senstay#1");
                if (result == IdentityResult.Success)
                {
                    superUser = userManager.FindByName(AppConstants.SUPER_ADMIN_ROLE);
                }
            }

            if (superUser != null && roleManager.Roles.Count() == 0)
            {
                roleManager.Create(new ApplicationRole { Name = AppConstants.SUPER_ADMIN_ROLE });
                roleManager.Create(new ApplicationRole { Name = AppConstants.ADMIN_ROLE });
            }

            if (superUser != null && roleManager.Roles.Count() > 0 && superUser.Roles.Count() == 0)
            {
                userManager.AddToRoles(superUser.Id, new string[] { AppConstants.SUPER_ADMIN_ROLE, AppConstants.ADMIN_ROLE });
            }
        }
    }
}