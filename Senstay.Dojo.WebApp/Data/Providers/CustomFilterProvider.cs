using System;
using System.Collections.Generic;
using System.Linq;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Data.Providers
{
    public class CustomFilterProvider
    {
        public CustomFilterProvider()
        {
        }

        public List<CustomFilterType> Markets()
        {
            List<CustomFilterType> filters = new List<CustomFilterType>()
            {
                new CustomFilterType { DisplayName = "New York", ID = "Market-NewYork", Field = "Market", Type = FilterType.Button },
                new CustomFilterType { DisplayName = "Boston", ID = "Market-Boston", Field = "Market", Type = FilterType.Button },
                new CustomFilterType { DisplayName = "Phoenix", ID = "Market-Phoenix", Field = "Market", Type = FilterType.Button },
                new CustomFilterType { DisplayName = "Denver", ID = "Market-Denver", Field = "Market", Type = FilterType.Button },
                new CustomFilterType { DisplayName = "Los Angeles", ID = "Market-LA", Field = "Market", Type = FilterType.Button },
                //new CustomFilterType { DisplayName = "San Diego", ID = "MarketSanDiego", Field = "Market", Type = FilterType.Button },
                new CustomFilterType { DisplayName = "Cabo San Lucas", ID = "MarketCabo", Field = "Market", Type = FilterType.Button },
            };
            return filters;
        }

        public List<CustomFilterType> Statuses()
        {
            List<CustomFilterType> filters = new List<CustomFilterType>()
            {
                new CustomFilterType { DisplayName = "Active", ID = "PropertyStatus-Active", Field = "PropertyStatus", Type = FilterType.Checkbox },
                new CustomFilterType { DisplayName = "Inactive", ID = "PropertyStatus-Inactive", Field = "PropertyStatus", Type = FilterType.Checkbox },
                new CustomFilterType { DisplayName = "Pending", ID = "PropertyStatus-Pending", Field = "PropertyStatus", Type = FilterType.Checkbox },
            };
            return filters;
        }

        public List<CustomFilterType> Verticals()
        {
            List < CustomFilterType> filters = new List<CustomFilterType>()
            {
                new CustomFilterType { DisplayName = "FS", ID = "Vertical-FS", Field = "Vertical", Type = FilterType.Checkbox },
                new CustomFilterType { DisplayName = "RS", ID = "Channel-RS", Field = "Vertical", Type = FilterType.Checkbox },
                new CustomFilterType { DisplayName = "CHESS", ID = "Channel-CHESS", Field = "Vertical", Type = FilterType.Checkbox },
                new CustomFilterType { DisplayName = "Payne", ID = "Channel-Payne", Field = "Vertical", Type = FilterType.Checkbox },
            };
            return filters;
        }

        public List<CustomFilterType> Channels()
        {
            List<CustomFilterType> filters = new List<CustomFilterType>()
            {
                new CustomFilterType { DisplayName = "Airbnb", ID = "Channel-AirBnB", Field = "Channel", Type = FilterType.Checkbox },
                new CustomFilterType { DisplayName = "Home Away", ID = "Channel-HomeAway", Field = "Channel", Type = FilterType.Checkbox },
            };
            return filters;
        }

        public List<CustomFilterType> InquiryApprovals()
        {
            List<CustomFilterType> filters = new List<CustomFilterType>()
            {
                new CustomFilterType { DisplayName = "Approved", ID = "PricingDecision1-YES", Field = "PricingDecision1", Type = FilterType.Checkbox },
                new CustomFilterType { DisplayName = "Disapproved", ID = "PricingDecision1-NO", Field = "PricingDecision1", Type = FilterType.Checkbox },
                new CustomFilterType { DisplayName = "Hold", ID = "PricingDecision1-HOLD", Field = "PricingDecision1", Type = FilterType.Checkbox },
                new CustomFilterType { DisplayName = "Counter Offer", ID = "PricingDecision1-COUNTEROFFER", Field = "PricingDecision1", Type = FilterType.Checkbox },
            };
            return filters;
        }
    }
}
