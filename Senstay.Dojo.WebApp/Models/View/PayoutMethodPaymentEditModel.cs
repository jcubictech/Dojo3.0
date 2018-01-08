using System;
using System.Collections.Generic;

namespace Senstay.Dojo.Models.View
{
    public class PayoutMethodPaymentEditModel
    {
        public PayoutMethodPaymentEditModel()
        {
            Payments = new List<PayoutPaymentItem>();
        }

        public List<PayoutPaymentItem> Payments { get; set; }
    }

    public class PayoutPaymentItem
    {
        public int PayoutMethodId { get; set; }

        public int? PayoutPaymentId { get; set; }

        public string PayoutMethodName { get; set; }

        public PayoutMethodType PayoutMethodType { get; set; }

        public string PayoutAccount { get; set; }

        public int PaymentMonth { get; set; }

        public int PaymentYear { get; set; }

        public double? BeginBalance { get; set; }

        public double? TotalBalance { get; set; }

        public double? CarryOver { get; set; }

        public double? PaymentAmount { get; set; }

        public DateTime? PaymentDate { get; set; }
    }
}