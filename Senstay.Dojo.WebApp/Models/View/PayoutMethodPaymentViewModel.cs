using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Senstay.Dojo.Models.View
{
    public class PayoutMethodPaymentViewModel
    {
        public PayoutMethodPaymentViewModel()
        {
        }

        public int PayoutMethodId { get; set; }

        public int? PayoutPaymentId { get; set; }

        public string PayoutMethodName { get; set; }

        public DateTime EffectiveDate { get; set; }

        public string PayoutAccount { get; set; }

        public string PayoutMethodType { get; set; }

        public double? BeginBalance { get; set; }

        public double? PayoutTotal { get; set; }

        public double? TotalBalance { get; set; }

        public string SelectedProperties { get; set; } // comma delimited property codes

        public DateTime? PaymentDate { get; set; }

        public double? PaymentAmount { get; set; }

        public int PaymentMonth { get; set; }

        public int PaymentYear { get; set; }
    }

    public class OwnerPayoutModel
    {
        public OwnerPayoutModel(string properties = null)
        {
            LinkedPropertyCodes = new List<SelectListItem>();
            if (properties != null)
            {
                string[] tokens = properties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(string token in tokens)
                {
                    LinkedPropertyCodes.Add(new SelectListItem { Text = token.Trim(), Value = token.Trim() });
                }
            }
        }

        public int PayoutMethodId { get; set; }

        public string PayoutMethodName { get; set; }

        public DateTime EffectiveDate { get; set; }

        public string PayoutAccount { get; set; }

        public string PayoutMethodType { get; set; }

        public double? BeginBalance { get; set; }

        public double? PayoutTotal { get; set; }

        public double? TotalBalance { get; set; }

        public string SelectedProperties { get; set; } // comma delimited property codes

        public List<SelectListItem> LinkedPropertyCodes { get; set; }

        public List<OwnerPaymentModel> Children { get; set; }
    }

    public class OwnerPaymentModel
    {
        public OwnerPaymentModel()
        {
        }

        public int PayoutMethodId { get; set; }

        public int? PayoutPaymentId { get; set; }

        public DateTime? PaymentDate { get; set; }

        public double? PaymentAmount { get; set; }

        public int PaymentMonth { get; set; }

        public int PaymentYear { get; set; }
    }
}