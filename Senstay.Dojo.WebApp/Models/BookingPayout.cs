using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("BookingPayout")]
    public class BookingPayout
    {
        [Key]
        public int ReservationId { get; set; }

        public float PayoutAmount { get; set; }

        public DateTime? PayoutDate { get; set; }
    }
}
