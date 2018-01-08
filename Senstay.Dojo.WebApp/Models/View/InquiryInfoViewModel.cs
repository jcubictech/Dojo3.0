using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models.View
{
    [NotMapped]  // this will make subclass not to create [Discriminator] column in datebase
    public class InquiryInfoViewModel : InquiriesValidation
    {
        public InquiryInfoViewModel() : base()
        {
            Property = new CPL();
        }

        public CPL Property { get; set; }
    }
}