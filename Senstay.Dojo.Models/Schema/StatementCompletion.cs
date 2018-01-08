using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("StatementCompletion")]
    public class StatementCompletion
    {
        [Key]
        public int StatementCompletionId { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        public bool Completed { get; set; } = false;
    }
}

