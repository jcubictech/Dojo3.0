using System.Collections.Generic;

namespace Senstay.Dojo.Fantastic
{

    /// <summary>
    /// Fantastic API POST request response Json string
    /// </summary>
    public class PostResponse
    {
        public bool success { get; set; }
        public string error { get; set; } = string.Empty;
    }

    public class ListingResult
    {
        public bool success { get; set; }
        public List<PropertyMap> listings { get; set; }
        public int total { get; set; }
    }

    public class CalendarResult
    {
        public bool success { get; set; }
        public List<CalendarMap> calendar { get; set; }
    }

    public class PropertyMap
    {
        public string id { get; set; }
        public string nickname { get; set; }
    }

    public class CalendarMap
    {
        public string id { get; set; }  // airbnb listing id
        public string fs_listing_id { get; set; }  // fantastic listing id
        public double price { get; set; }
        public string note { get; set; }
        public string date { get; set; } // yyyy-MM-dd format
    }

}