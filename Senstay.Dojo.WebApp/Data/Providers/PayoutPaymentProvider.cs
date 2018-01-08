using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Data.Providers
{
    public class PayoutPaymentProvider : CrudProviderBase<PayoutPayment>
    {
        private readonly DojoDbContext _context;

        public PayoutPaymentProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public bool Exist(PayoutPayment payment)
        {
            return _context.PayoutPayments.FirstOrDefault(p => p.PayoutMethodId == payment.PayoutPaymentId) != null;
        }

        public bool Exist(int paymentId)
        {
            return _context.PayoutPayments.FirstOrDefault(p => p.PayoutMethodId == paymentId) != null;
        }

        public List<PayoutPayment> GetPaymentsForPayoutMethod(int payoutMethodId)
        {
            try
            {
                return _context.PayoutPayments.Where(p => p.PayoutMethodId == payoutMethodId).OrderBy(x => x.PaymentDate).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public void SavePayoutMethodPayments(List<PayoutPayment> payments)
        {
            try
            {
                foreach (PayoutPayment payment in payments)
                {
                    if (payment.PayoutMethodId > 0)
                    {
                        if (payment.PayoutPaymentId == 0) // create payment
                        {
                            payment.PaymentDate = ConversionHelper.EnsureUtcDate(DateTime.Now);
                            Create(payment);
                        }
                        else // update payment
                        {
                            var entity = Retrieve(payment.PayoutPaymentId);
                            if (entity.PaymentAmount != payment.PaymentAmount)
                            {
                                entity.PaymentAmount = payment.PaymentAmount;
                                entity.PaymentDate = ConversionHelper.EnsureUtcDate(DateTime.Now);
                                Update(payment.PayoutPaymentId, entity);
                            }
                        }
                    }
                }
                Commit();
            }
            catch
            {
                throw;
            }
        }
    }
}
