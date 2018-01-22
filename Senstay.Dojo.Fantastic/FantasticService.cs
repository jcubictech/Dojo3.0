using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Senstay.Dojo.Fantastic.Models;

namespace Senstay.Dojo.Fantastic
{
    public class FantasticService
    {
        public const string API_LISTINGS_SERVICE = "/api/listings";
        public const string API_CALENDAR_SERVICE = "/api/calendar";
        public const string API_CUSTOMSTAY_SERVICE = "/api/custom_stay";
        public const string API_GET = "GET";
        public const string API_POST = "POST";

        public FantasticService()
        {
        }

        public ListingResult PropertyListing()
        {
            var endPoint = RestRequest.GetEndPoint(API_LISTINGS_SERVICE);
            var response = RestRequest.Get(endPoint);
            var listingJson = JsonConvert.DeserializeObject<ListingResult>(response);
            return listingJson;
        }

        public string CalendarListing(int listingId)
        {
            var parameters = new List<KeyValuePair<string, object>>() {
                                 new KeyValuePair<string, object>("listing_id", listingId.ToString())
                             };
            var endPoint = RestRequest.GetEndPoint(API_CALENDAR_SERVICE, parameters);
            return RestRequest.Get(endPoint);
        }

        public CalendarResult PriceListing(int listingId, DateTime startDate, DateTime endDate)
        {
            var parameters = new List<KeyValuePair<string, object>>() {
                                 new KeyValuePair<string, object>("listing_id", listingId.ToString()),
                                 new KeyValuePair<string, object>("start_date", startDate.ToString("yyyy-MM-dd")),
                                 new KeyValuePair<string, object>("end_date", endDate.ToString("yyyy-MM-dd")),
                             };
            var endPoint = RestRequest.GetEndPoint(API_CALENDAR_SERVICE, parameters);
            return RestRequest.GetPrices(endPoint);
        }

        public PostResponse PricePush(int listingId, DateTime startDate, DateTime endDate, bool isAvailable, double price, string note)
        {
            // Fantastic API takes request body as the same format as query parameters; not a serialized json object
            var requestContent = PricingParameters.Stringify(listingId, startDate, endDate, isAvailable, price, note);
            var endPoint = RestRequest.GetEndPoint(API_CALENDAR_SERVICE);
            return RestRequest.Post(endPoint, requestContent);
        }

        public PostResponse PricePush(FantasticPriceModel model)
        {
            return PricePush(
                        model.ListingId,
                        model.StartDate,
                        model.EndDate,
                        model.IsAvailable,
                        model.Price.Value,
                        model.Note
                   );
        }

        public PostResponse CustomStay(int listingId, DateTime startDate, DateTime endDate, int minStay)
        {
            // Fantastic API takes request body as the same format as query parameters; not a serialized json object
            var requestContent = CustomStayParameters.Stringify(listingId, startDate, endDate, minStay);
            var endPoint = RestRequest.GetEndPoint(API_CUSTOMSTAY_SERVICE);
            return RestRequest.Post(endPoint, requestContent);
        }

        public PostResponse CustomStay(FantasticCustomStayModel model)
        {
            return CustomStay(model.ListingId, model.StartDate, model.EndDate, model.MinStay);
        }
    }
}
