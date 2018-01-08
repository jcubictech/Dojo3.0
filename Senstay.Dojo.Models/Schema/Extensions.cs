using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Models
{
    public partial class InquiriesValidation : IValidatableObject
    {
        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsUpload)
            {
                yield return ValidationResult.Success;
            }
            else
            {
                if(!TotalPayout.HasValue)
                {
                    yield return new ValidationResult("Total Payout is required", new[] { "TotalPayout" });
                }
                if (!Check_inDate.HasValue)
                {
                    yield return new ValidationResult("Check-in Date is required", new[] { "Check_inDate" });
                }
                if (!Check_outDate.HasValue)
                {
                    yield return new ValidationResult("Check-out Date is required", new[] { "Check_outDate" });
                }
                if (Check_inDate.HasValue && Check_outDate.HasValue && Check_inDate > Check_outDate)
                {
                    yield return new ValidationResult("Check-out Date " + Check_outDate + " must be later than Check-in Date " + Check_inDate, new[] { "Check_outDate" });
                }
            }
        }
    }

    public partial class InquiriesValidation 
    {
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsUpload { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string CplAirBnBHomeName { get { return CPL == null ? "" : CPL.AirBnBHomeName; } }


    }
}