using System;

namespace Senstay.Dojo.Models.View
{
    public class AppLogViewModel
    {
        public DateTime EventDateTime { get; set; }

        public string EventLevel { get; set; }

        public string UserName { get; set; }

        public string MachineName { get; set; }

        public string EventMessage { get; set; }
    }
}