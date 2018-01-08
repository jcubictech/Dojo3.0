using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;

namespace Senstay.Dojo.Models
{
    [Table("NewFeature")]
    public class NewFeature
    {
        [Key]
        public int NewFeatureId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public DateTime DeployDate { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public DateTime ExpiredDate { get; set; }

        [AllowHtml]
        public string Description { get; set; }
    }
}
