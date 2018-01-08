using System.ComponentModel.DataAnnotations.Schema;
using Senstay.Dojo.Data.Providers;

namespace Senstay.Dojo.Models.View
{
    [NotMapped]  // this will make subclass not to create [Discriminator] column in datebase
    public class PropertyViewModel : CPL
    {
        public PropertyViewModel() : base()
        {
            CustomActionBar = new ActionBarViewModel() {
                CustomFilter1 = new CustomFilterProvider().Markets(),
                CustomFilter2 = new CustomFilterProvider().Statuses(),
                CustomFilter3 = new CustomFilterProvider().Verticals()
            };
        }

        public ActionBarViewModel CustomActionBar { get; set; }
    }
}