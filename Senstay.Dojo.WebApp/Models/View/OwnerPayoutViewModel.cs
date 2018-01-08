using System;
using System.ComponentModel.DataAnnotations;

namespace Senstay.Dojo.Models.View
{
    public class OwnerPayoutViewModel
    {
        public OwnerPayoutViewModel()
        {
        }

        [Required(ErrorMessage = "{0} is required.")]
        public DateTime ReportDate { get; set; }

        public string CompletedTransactionFiles { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public string FutureTransactionFiles { get; set; }

        public string StreamlineFile { get; set; }

        public string ExpenseFile { get; set; }
    }
}