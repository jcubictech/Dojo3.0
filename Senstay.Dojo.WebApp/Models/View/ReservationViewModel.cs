using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Senstay.Dojo.Models.HelperClass;

namespace Senstay.Dojo.Models.View
{
    public class ReservationViewModel
    {
        public ReservationViewModel()
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

    public class ResevationTetrisModel
    {
        public ResevationTetrisModel() { }

        public int ReservationId { get; set; }

        public string OldPropertyCode { get; set; }

        public string NewPropertyCode { get; set; }
    }

    public class ResevationSplitModel
    {
        public ResevationSplitModel()
        {
            TargetProperties = new List<string>();
        }

        public int ReservationId { get; set; }

        public string PropertyCode { get; set; }

        public string ConfirmationCode { get; set; }

        public double ReservationAmount { get; set; }

        public List<string> TargetProperties { get; set; }
    }
}