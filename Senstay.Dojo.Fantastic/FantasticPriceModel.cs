using System;

namespace Senstay.Dojo.Fantastic.Models
{
    public class FantasticPriceModel
    {
        // default constructor
        public FantasticPriceModel() { }

        // copy constructor
        public FantasticPriceModel(FantasticPriceModel model)
        {
            ListingId = model.ListingId;
            StartDate = model.StartDate;
            EndDate = model.EndDate;
            IsAvailable = model.IsAvailable;
            Price = model.Price;
            Note = model.Note;
        }

        public int ListingId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsAvailable { get; set; }

        public double? Price { get; set; } = null;

        public string Note { get; set; } = null;
    }

    internal class PricingParameters
    {
        public static string Stringify(int listingId)
        {
            return "listing_id=" + listingId.ToString();
        }

        public static string Stringify(int listingId, DateTime startDate, DateTime endDate, bool isAvailable)
        {
            return Stringify(listingId, startDate, endDate, isAvailable, null, null);
        }

        public static string Stringify(int listingId, DateTime startDate, DateTime endDate, bool isAvailable, double price)
        {
            return Stringify(listingId, startDate, endDate, isAvailable, price, null);
        }

        public static string Stringify(int listingId, DateTime startDate, DateTime endDate, bool isAvailable, double? price, string note)
        {
            return "listing_id=" + listingId.ToString() +
                   "&start_date=" + startDate.ToString("yyyy-MM-dd") +
                   "&end_date=" + endDate.ToString("yyyy-MM-dd") +
                   "&is_available=" + (isAvailable == true ? "1" : "0") +
                   (price == null ? string.Empty : "&price=" + price.Value.ToString("#.##")) +
                   (note == null ? string.Empty : "&note=" + note);
        }

        public static string Stringify(FantasticPriceModel model)
        {
            return Stringify(model.ListingId, model.StartDate, model.EndDate, model.IsAvailable, model.Price, model.Note);
        }
    }
}