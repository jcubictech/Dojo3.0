using System.ComponentModel.DataAnnotations.Schema;
using Senstay.Dojo.Data.Providers;

namespace Senstay.Dojo.Models.View
{
    [NotMapped]  // this will make subclass not to create [Discriminator] column in datebase
    public class InquiryViewModel : InquiriesValidation
    {
        public InquiryViewModel() : base()
        {
            InquiryId = 0;

            CustomActionBar = new ActionBarViewModel()
            {
                CustomFilter1 = new CustomFilterProvider().Markets(),
                CustomFilter2 = new CustomFilterProvider().Statuses(),
                CustomFilter3 = new CustomFilterProvider().Verticals()
            };
        }

        public string Market { get; set; }

        public string BeltDesignation { get; set; }

        public int InquiryId { get; set; }

        public string UserName { get; set; }

        public ActionBarViewModel CustomActionBar { get; set; }
    }
}