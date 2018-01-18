using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Senstay.Dojo.Models.View
{
    public class PropertyAccountViewModel
    {
        public PropertyAccountViewModel()
        {
            SelectedPayoutMethods = new List<SelectListItem>();
        }

        public int PropertyAccountId { get; set; }

        public string LoginAccount { get; set; }

        public string OwnerName { get; set; }

        public string OwnerEmail { get; set; }

        public string CurrentPayoutMethodIds { get; set; }

        public ICollection<SelectListItem> SelectedPayoutMethods { get; set; }
    }

    public class PayoutMethodViewModel
    {
        public PayoutMethodViewModel()
        {
            SelectedPropertyCodes = new List<SelectListItem>();
        }

        public int PayoutMethodId { get; set; }

        [MaxLength(100), Required]
        public string PayoutMethodName { get; set; }

        [MaxLength(50)]
        public string PayoutAccount { get; set; }

        public string PayoutMethodType { get; set; }

        public DateTime EffectiveDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public string CurrentPropertyCodes { get; set; }

        public ICollection<SelectListItem> SelectedPropertyCodes { get; set; }
    }

    public class PropertyEntityViewModel
    {
        public PropertyEntityViewModel()
        {
            SelectedPropertyCodes = new List<SelectListItem>();
        }

        public int PropertyEntityId { get; set; }

        [MaxLength(50), Required]
        public string EntityName { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        public string CurrentPropertyCodes { get; set; }

        public ICollection<SelectListItem> SelectedPropertyCodes { get; set; }
    }

    public class PropertyFeeViewModel
    {
        public PropertyFeeViewModel()
        {
        }

        public int PropertyFeeId { get; set; }

        [MaxLength(50), Required]
        public string PropertyCode { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public double CityTax { get; set; }

        [Required]
        public double ManagementFee { get; set; }

        [Required]
        public double DamageWaiver { get; set; }

        [Required]
        public double Cleanings { get; set; }

        [Required]
        public double Laundry { get; set; }

        [Required]
        public double Consumables { get; set; }

        [Required]
        public double PoolService { get; set; }

        [Required]
        public double Landscaping { get; set; }

        [Required]
        public double TrashService { get; set; }

        [Required]
        public double PestService { get; set; }
    }

    public class PropertyCodeViewModel
    {
        public PropertyCodeViewModel()
        {
        }

        public string PropertyCode { get; set; }

        public double CityTax { get; set; }

        public double DamageWaiver { get; set; }

        public double ManagementFee { get; set; }

        public string PropertyOwner { get; set; }

        public string PayoutMethod { get; set; }

        public string PayoutEntity { get; set; }
    }
}