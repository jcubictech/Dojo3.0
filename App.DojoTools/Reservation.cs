using System;

namespace DojoTools
{
    public class Reservation
    {
        public int ReservationId { get; set; }

        public string PropertyCode { get; set; }

        public DateTime? ReservsationDate { get; set; }

        public string ConfirmationCode { get; set; }

        public DateTime? StartDate { get; set; }

        public int Nights { get; set; }

        public string GuestName { get; set; }

        public float PayoutAmount { get; set; }

        public float HostFee { get; set; }

        public float CleanFee { get; set; }
    }
}
