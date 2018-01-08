using System;
using System.Diagnostics;
using System.Reflection;
using NLog;

namespace Senstay.Dojo.Helpers
{
    public static class EventLogger
    {
        public static void Error(Logger logger, Exception ex, string message, Type t = null)
        {
            LogEventInfo ev = EventLogger.SetDbErrorInfo(ex, message, t);
            logger.Error(ev);
        }

        public static LogEventInfo SetDbErrorInfo(Exception ex, string message, Type t = null)
        {
            string className = null;
            if (t != null) className = t.Name;
            return SetDbErrorInfo(ex, message, className);
        }

        public static LogEventInfo SetDbErrorInfo(Exception ex, string message, string className)
        {
            LogEventInfo ev = new LogEventInfo(LogLevel.Error, "DojoLogger", message);

            if (ex != null)
            {
                StackTrace trace = new StackTrace(ex);

                if (trace.FrameCount > 0)
                {
                    StackFrame frame = trace.GetFrame(0);
                    MethodBase method = frame.GetMethod();

                    // these NLog event properties are configured in NLog.config
                    ev.Properties["error-source"] = ex.Source;
                    ev.Properties["error-class"] = (className != null ? className : method.DeclaringType.FullName);
                    ev.Properties["error-method"] = string.Concat(method.DeclaringType.FullName, ".", method.Name);
                    ev.Properties["error-message"] = ex.Message;
                    ev.Properties["inner-error-message"] = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                }

                ev.Exception = ex;
            }

            return ev;
        }
    }
}