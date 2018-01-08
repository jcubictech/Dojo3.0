using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("Lookup")]
    public class Lookup
    {
        [Key]
        public int Id { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }
    }
}