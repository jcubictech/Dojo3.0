using System.Collections.Generic;
using NLog;
using NLog.Targets;
using NLog.Config;
using System;
using Senstay.Dojo.App.Helpers;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo
{
    public class LogConfig
    {
        public static void SyncWithAppConfig(string logLevelName)
        {
            ConfigurationItemFactory.Default.LayoutRenderers
                .RegisterDefinition("app-user", typeof(AppUserLayoutRenderer));

            // if NLog auto-load is not auto-loading its configuration file, we load it manually.
            if (LogManager.Configuration.AllTargets.Count == 0)
            {
                string logPath = AppDomain.CurrentDomain.BaseDirectory + "/NLog.config";
                LogManager.Configuration = new XmlLoggingConfiguration(logPath);
            }

            var databaseTarget = LogManager.Configuration.FindTargetByName<DatabaseTarget>("db");
            string connectionString = SettingsHelper.GetSafeConnectionString(AppConstants.DOJO_CONNECTION_NAME);
            if (databaseTarget != null && !string.IsNullOrEmpty(connectionString))
            {
                databaseTarget.ConnectionString = connectionString;
            }

            if (logLevelName != null)
            {
                bool done = false;
                IList<LoggingRule> loggingRules = LogManager.Configuration.LoggingRules;
                foreach (LoggingRule rule in loggingRules)
                {
                    foreach (Target target in rule.Targets)
                    {
                        if (!done && target.Name == "db") // we only interested in db logging configuration
                        {
                            LogLevel logLevel = LogLevel.Error;
                            // move loglevel one level down and disable it and all levels below it
                            switch (logLevelName.ToLower())
                            {
                                case "fatal": logLevel = LogLevel.Error; break;
                                case "error": logLevel = LogLevel.Warn;  break;
                                case "warn" : logLevel = LogLevel.Info;  break;
                                case "info" : logLevel = LogLevel.Debug; break;
                                case "debug": logLevel = LogLevel.Trace; break;
                                case "trace": logLevel = null; break;
                                default: logLevel = LogLevel.Error; break;
                            }

                            rule.EnableLoggingForLevel(LogLevel.Trace);
                            rule.EnableLoggingForLevel(LogLevel.Debug);
                            rule.EnableLoggingForLevel(LogLevel.Info);
                            rule.EnableLoggingForLevel(LogLevel.Warn);
                            rule.EnableLoggingForLevel(LogLevel.Error);
                            rule.EnableLoggingForLevel(LogLevel.Fatal);

                            if (logLevel != null)
                            {
                                if (logLevel.Ordinal >= LogLevel.Trace.Ordinal) rule.DisableLoggingForLevel(LogLevel.Trace);
                                if (logLevel.Ordinal >= LogLevel.Debug.Ordinal) rule.DisableLoggingForLevel(LogLevel.Debug);
                                if (logLevel.Ordinal >= LogLevel.Info.Ordinal) rule.DisableLoggingForLevel(LogLevel.Info);
                                if (logLevel.Ordinal >= LogLevel.Warn.Ordinal) rule.DisableLoggingForLevel(LogLevel.Warn);
                                if (logLevel.Ordinal >= LogLevel.Error.Ordinal) rule.DisableLoggingForLevel(LogLevel.Error);
                                if (logLevel.Ordinal >= LogLevel.Fatal.Ordinal) rule.DisableLoggingForLevel(LogLevel.Fatal);
                            }

                            done = true;
                        }
                    }

                    if (done) break;
                }
            }

            LogManager.ReconfigExistingLoggers();
        }
    }
}