using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("PropertyChangeHistory")]
    public class PropertyChangeHistory
    {
        [Key]
        public int PropertyChangeHistoryId { get; set; }

        [MaxLength(50)]
        public string PropertyCode { get; set; }

        public DateTime? ChangeDate { get; set; }

        [MaxLength(50)]
        public string FieldName { get; set; }

        [MaxLength(50)]
        public string OriginalValue { get; set; }

        [MaxLength(50)]
        public string NewValue { get; set; }

        public virtual CPL Proeprties { get; set; } // foreign key
    }
}
