using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Security.Claims;
using System.Linq;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Models
{
    //===========================================================================================================
    // For AuditLog to work properly, the following needs to happen:
    // 1. Add [Key] attribute for the key field of the table
    // 2. Create timestamp fields: CreatedBy, CreatedDate, ModifiedBy, and ModifiedDate for primary tables.
    //    AuditLog will automate the setting of those fields.
    // 3. Set audit table 'AuditTables' configuration in web.config
    // 4. Include AuditLog table in schema
    //===========================================================================================================
    public class AuditLogging
    {
        public AuditLogging()
        {
        }

        /// <summary>
        /// Define scope need to trail, all Added/Deleted/Modified entities (not Unmodified or Detached)
        /// </summary>
        public static List<EntityState> changeStates = new List<EntityState>() { EntityState.Added, EntityState.Deleted, EntityState.Modified };
        
        /// <summary>
        ///  Define tables need to audit
        /// </summary>
        private static List<string> tablesTrail = SettingsHelper.GetSafeSetting(AppConstants.AUDIT_TABLES).Split('|').ToList();

        /// <summary>
        ///  Audit the entity framework changing. 
        /// </summary>
        /// <param name="changeTracker"></param>
        /// <param name="auditLog"></param>
        public static void WriteAuditLog(List<DbEntityEntry> entities, DbSet<AuditLog> auditLog)
        {
            try
            {
                foreach (var ent in entities)
                {
                    // For each changed record, get the audit record entries and add them
                    foreach (AuditLog x in GetAuditRecordsForChange(ent))
                    {
                        auditLog.Add(x);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        /// <summary>
        /// Audit the log directly.
        /// </summary>
        /// <param name="auditLog"></param>
        /// <param name="eventType"></param>
        /// <param name="tableName"></param>
        /// <param name="keyValue"></param>
        /// <param name="columnName"></param>
        /// <param name="origValue"></param>
        /// <param name="newValue"></param>
        /// <param name="auditMessage"></param>
        /// <param name="modifiedBy"></param>
        public static void WriteAuditLog(string eventType, string tableName, string keyValue, 
            string columnName, string origValue, string newValue, string auditMessage, string modifiedBy)
        {
            var db = new DojoDbContext();

            AuditLog log = new AuditLog()
            {
                EventDate = DateTime.UtcNow,
                EventType = eventType,
                TableName = tableName,
                RecordId = keyValue,
                ColumnName = columnName,
                OriginalValue = origValue,
                NewValue = newValue,
                AuditMessage = auditMessage,
                ModifiedBy = modifiedBy
            };

            db.AuditLog.Add(log);
            db.SaveChanges();
        }

        private static List<AuditLog> GetAuditRecordsForChange(DbEntityEntry dbEntry)
        {
            List<AuditLog> result = new List<AuditLog>();
            DateTime changeTime = DateTime.UtcNow;

            // Get the Table() attribute, if one exists
            TableAttribute tableAttr = dbEntry.Entity.GetType().GetCustomAttributes(typeof(TableAttribute), true).SingleOrDefault() as TableAttribute;

            // Get table name (if it has a Table attribute, use that, otherwise get the pluralized name)
            string tableName = tableAttr != null ? tableAttr.Name : dbEntry.Entity.GetType().Name;

            if (tablesTrail.Contains(tableName))
            {
                // Get primary key value (If you have more than one key column, this will need to be adjusted)
                var keyNames = dbEntry.Entity.GetType().GetProperties().Where(p => p.GetCustomAttributes(typeof(KeyAttribute), false).Count() > 0).ToList();
                string keyName = keyNames[0].Name;

                if (dbEntry.State == EntityState.Added || dbEntry.State == EntityState.Unchanged)
                {
                    // For Inserts, just add the whole record
                    // If the entity implements IDescribableEntity, use the description from Describe(), otherwise use ToString()
                    foreach (string propertyName in dbEntry.CurrentValues.PropertyNames)
                    {
                        result.Add(BuildLogModel(changeTime, dbEntry.State, tableName, keyName, propertyName, dbEntry));
                    }
                }
                else if (dbEntry.State == EntityState.Deleted)
                {
                    // Same with deletes, do the whole record, and use either the description from Describe() or ToString()
                    foreach (string propertyName in dbEntry.OriginalValues.PropertyNames)
                    {
                        result.Add(BuildLogModel(changeTime, dbEntry.State, tableName, keyName, propertyName, dbEntry));
                    }
                }
                else if (dbEntry.State == EntityState.Modified)
                {
                    foreach (string propertyName in dbEntry.OriginalValues.PropertyNames)
                    {
                        // For updates, we only want to capture the columns that actually changed
                        if (!object.Equals(dbEntry.OriginalValues.GetValue<object>(propertyName), dbEntry.CurrentValues.GetValue<object>(propertyName)))
                        {
                            result.Add(BuildLogModel(changeTime, dbEntry.State, tableName, keyName, propertyName, dbEntry));
                        }
                    }
                }
            }
            return result;
        }

        private static AuditLog BuildLogModel(DateTime changeTime, EntityState eventState, 
            string tableName, string keyName, string columnName, DbEntityEntry dbEntry)
        {

            var auditLog = new AuditLog();
            auditLog.EventDate = changeTime;
            auditLog.ColumnName = columnName;
            auditLog.TableName = tableName;
            auditLog.ModifiedBy = ClaimsPrincipal.Current.Identity.Name;
            switch (eventState)
            {
                case EntityState.Added:
                case EntityState.Unchanged:
                    auditLog.EventType = EntityState.Added.ToString();
                    auditLog.RecordId = dbEntry.CurrentValues.GetValue<object>(keyName).ToString();
                    auditLog.NewValue = dbEntry.CurrentValues.GetValue<object>(columnName) == null ? null : dbEntry.CurrentValues.GetValue<object>(columnName).ToString();
                    break;
                case EntityState.Deleted:
                    auditLog.EventType = EntityState.Deleted.ToString();
                    auditLog.RecordId = dbEntry.OriginalValues.GetValue<object>(keyName).ToString();
                    auditLog.OriginalValue = dbEntry.OriginalValues.GetValue<object>(columnName) == null ? null : dbEntry.OriginalValues.GetValue<object>(columnName).ToString();
                    break;
                case EntityState.Modified:
                    auditLog.EventType = EntityState.Modified.ToString();
                    auditLog.RecordId = dbEntry.OriginalValues.GetValue<object>(keyName).ToString();
                    auditLog.OriginalValue = dbEntry.OriginalValues.GetValue<object>(columnName) == null ? null : dbEntry.OriginalValues.GetValue<object>(columnName).ToString();
                    auditLog.NewValue = dbEntry.CurrentValues.GetValue<object>(columnName) == null ? null : dbEntry.CurrentValues.GetValue<object>(columnName).ToString();
                    break;
                default:
                    eventState.ToString();
                    break;
            }

            return auditLog;
        }
    }
}