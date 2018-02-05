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

        [MaxLength(50)]
        public string Type { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }
    }
}