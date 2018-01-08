using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Senstay.Dojo.Models.HelperClass;

namespace Senstay.Dojo.Models.View
{
    public class ResolutionViewModel
    {
        public ResolutionViewModel()
        {
        }

        [Required(ErrorMessage = "{0} is required.")]
        public DateTime ReportDate { get; set; }

        public string CompletedGoogleFiles { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public string FutureGoogleFiles { get; set; }

        public string StreamlineFile { get; set; }

        public string ExpenseFile { get; set; }
    }
}