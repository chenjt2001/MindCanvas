using Microsoft.Services.Store.Engagement;

namespace MindCanvas
{
    static class LogHelper
    {
        private static readonly StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();

        public static void Debug(object message)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(message);
#endif
        }

        public static void Info(string message)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(message);
#endif
#if !DEBUG
            string eventName = string.Format("[INFO]{0}", message);
            logger.Log(eventName);
#endif
        }

        public static void Error(string message)
        {
            try
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(message);
#endif
                Settings.LastError = message;
#if !DEBUG
                string eventName = string.Format("[ERROR]{0}", message);
                logger.Log(eventName);
#endif
            }
            catch { }
        }
    }
}
