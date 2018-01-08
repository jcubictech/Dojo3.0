using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Senstay.Dojo.Data.Providers;

namespace Senstay.Dojo.Models.View
{
    [NotMapped]  // this will make subclass not to create [Discriminator] column in datebase
    public class AirbnbAccountViewModel : AirbnbAccount
    {
        public AirbnbAccountViewModel() : base()
        {
            CustomActionBar = new ActionBarViewModel()
            {
                CustomFilter1 = new CustomFilterProvider().Markets(),
            };
        }

        public ActionBarViewModel CustomActionBar { get; set; }

    }
}