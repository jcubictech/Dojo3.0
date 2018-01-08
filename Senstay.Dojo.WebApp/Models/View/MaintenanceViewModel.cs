using System;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Models.View
{
    public class MaintenanceViewModel
    {
        public MaintenanceViewModel() : base()
        {
            string onlineTime = SettingsHelper.GetSafeSetting(AppConstants.ONLINE_ESTIMATE_TIME);
            DateTime estimatedOnLineTime = DateTime.Now;
            bool ok = DateTime.TryParse(onlineTime, out estimatedOnLineTime);
            if (!ok) estimatedOnLineTime = DateTime.Now.AddHours(1.0); // default to be an hour from now
            EstimatedOnLineTime = estimatedOnLineTime;
        }

        public DateTime EstimatedOnLineTime { get; set; }
    }
}