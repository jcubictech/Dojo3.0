using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models.View
{
    public class RebalanceEditModel
    {
        public RebalanceEditModel()
        {
            Balances = new List<PropertyBalanceEditModel>();
        }

        public List<PropertyBalanceEditModel> Balances { get; set; }
    }

    [NotMapped]
    public class PropertyBalanceEditModel : PropertyBalance
    {
        public bool IsSummary { get; set; }
    }
}