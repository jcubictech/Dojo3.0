using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("DojoLog")]
    public class DojoLog
    {
        [Key]
        public int Id { get; set; }

        public DateTime EventDateTime { get; set; }

        [StringLength(100)]
        public string EventLevel { get; set; }

        [StringLength(100)]
        public string UserName { get; set; }

        [StringLength(100)]
        public string MachineName { get; set; }

        public string EventMessage { get; set; }

        [StringLength(100)]
        public string ErrorSource { get; set; }

        [StringLength(500)]
        public string ErrorClass { get; set; }

        public string ErrorMethod { get; set; }

        public string ErrorMessage { get; set; }

        public string InnerErrorMessage { get; set; }
    }
}