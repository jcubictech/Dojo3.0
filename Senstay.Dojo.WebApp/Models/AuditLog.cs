using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    public class AuditLog
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }
        public DateTime EventDate { get; set; }
        [StringLength(80)]
        public string EventType { get; set; }
        [StringLength(80)]
        public string TableName { get; set; }
        [StringLength(80)]
        public string RecordId { get; set; }
        [StringLength(80)]
        public string ColumnName { get; set; }
        public string OriginalValue { get; set; }
        public string NewValue { get; set; }
        public string AuditMessage { get; set; }
        public string ModifiedBy { get; set; }
    }
}