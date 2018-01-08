using System.ComponentModel.DataAnnotations;

namespace Senstay.Dojo.Models.HelperClass
{
    public partial class Approver
    {
        [Key]
        public string Name { get; set; }
        public string UserId { get; set; }
    }
}
