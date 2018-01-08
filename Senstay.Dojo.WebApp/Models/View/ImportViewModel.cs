using System;
using System.ComponentModel.DataAnnotations;

namespace Senstay.Dojo.Models.View
{
    public class ImportViewModel
    {
        public ImportViewModel()
        {
        }

        [Required(ErrorMessage = "{0} is required.")]
        public DateTime ImportDate { get; set; }

        public ImportFileType FileType { get; set; }

        public string ImportFile { get; set; }
    }
}