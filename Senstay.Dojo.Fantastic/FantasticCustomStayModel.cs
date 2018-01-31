using System;

namespace Senstay.Dojo.Fantastic.Models
{
    public class FantasticCustomStayModel
    {
        // default constructor
        public FantasticCustomStayModel() { }

        // copy constructor
        public FantasticCustomStayModel(FantasticCustomStayModel model)
        {
            ListingId = model.ListingId;
            StartDate = model.StartDate;
            EndDate = model.EndDate;
            MinStay = model.MinStay;
        }

        public int ListingId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int MinStay { get; set; }
    }

    internal class CustomStayParameters
    {
        public static string Stringify(int listingId)
        {
            return "listing_id=" + listingId.ToString();
        }

        public static string Stringify(FantasticCustomStayModel model)
        {
            return Stringify(model.ListingId, model.StartDate, model.EndDate, model.MinStay);
        }

        public static string Stringify(int listingId, DateTime startDate, DateTime endDate, int minStay)
        {
            return "listing_id=" + listingId.ToString() +
                   "&start_date=" + startDate.ToString("yyyy-MM-dd") +
                   "&end_date=" + endDate.ToString("yyyy-MM-dd") +
                   "&min_stay=" + minStay.ToString();
        }
    }
}