using System.Collections.Generic;
using System.Web.Mvc;

namespace Senstay.Dojo.Data.Providers
{
    public static class ListProvider
    {
        public static List<SelectListItem> Month()
        {
            return new List<SelectListItem>()
            {
                new SelectListItem() { Text = "January", Value = "1" },
                new SelectListItem() { Text = "Feburary", Value = "2" },
                new SelectListItem() { Text = "March", Value = "3" },
                new SelectListItem() { Text = "April", Value = "4" },
                new SelectListItem() { Text = "May", Value = "5" },
                new SelectListItem() { Text = "June", Value = "6" },
                new SelectListItem() { Text = "July", Value = "7" },
                new SelectListItem() { Text = "August", Value = "8" },
                new SelectListItem() { Text = "September", Value = "9" },
                new SelectListItem() { Text = "October", Value = "10" },
                new SelectListItem() { Text = "November", Value = "11" },
                new SelectListItem() { Text = "December", Value = "12" },
            };
        }

        private static List<SelectListItem> _currency = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "USD ($)", Value = "$" },
            new SelectListItem() { Text = "Real (R$)", Value = "R$" },
            new SelectListItem() { Text = "Euro (€)", Value = "€" },
            new SelectListItem() { Text = "Pounds (£)", Value = "£" },
        };

        public static List<SelectListItem> CurrencyList
        {
            get
            {
                return _currency;
            }
        }

        private static List<SelectListItem> _questionList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "YES", Value = "YES" },
            new SelectListItem() { Text = "NO", Value = "NO" },
            new SelectListItem() { Text = "COUNTEROFFER", Value = "COUNTEROFFER" },
            new SelectListItem() { Text = "TETRIS", Value = "TETRIS" },
            new SelectListItem() { Text = "NEED MORE INFO", Value = "NEED MORE INFO" },
            new SelectListItem() { Text = "HOLD", Value = "HOLD" },
            new SelectListItem() { Text = "TOO FAR OUT", Value = "TOO FAR OUT" },
        };

        public static List<SelectListItem> QuestionList
        {
            get
            {
                return _questionList;
            }
        }

        private static List<SelectListItem> _chanelList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "AirBnB", Value = "AirBnB" },
            new SelectListItem() { Text = "HomeAway", Value = "HomeAway" },
            new SelectListItem() { Text = "FlipKey", Value = "FlipKey" },
            new SelectListItem() { Text = "Direct", Value = "Direct" },
            new SelectListItem() { Text = "Booking.com", Value = "Booking.com" },
            new SelectListItem() { Text = "Expedia", Value = "Expedia" },
        };

        public static List<SelectListItem> ChanelList
        {
            get
            {
                return _chanelList;
            }
        }

        private static List<SelectListItem> _noolBoolList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Yes", Value = "True" },
            new SelectListItem() { Text = "No", Value = "False" },
        };

        public static List<SelectListItem> NoolBoolList
        {
            get
            {
                return _noolBoolList;
            }
        }

        private static List<SelectListItem> _yesNotList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Y", Value = "Y" },
            new SelectListItem() { Text = "N", Value = "N" },
        };

        public static List<SelectListItem> YesNotList
        {
            get
            {
                return _yesNotList;
            }
        }

        private static List<SelectListItem> _yesNoNotAvailableList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Yes", Value = "Yes" },
            new SelectListItem() { Text = "No", Value = "No" },
            new SelectListItem() { Text = "N/A", Value = "N/A" },
        };

        public static List<SelectListItem> YesNoNotAvailableList
        {
            get
            {
                return _yesNoNotAvailableList;
            }
        }


        private static List<SelectListItem> _statusList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Active", Value = "Active" },
            new SelectListItem() { Text = "Inactive", Value = "Inactive" },
            new SelectListItem() { Text = "Pending", Value = "Pending" },
        };

        public static List<SelectListItem> StatusList
        {
            get
            {
                return _statusList;
            }
        }

        private static List<SelectListItem> _propertyList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Active", Value = "Active" },
            new SelectListItem() { Text = "Active-Airbnb", Value = "Active-Airbnb" },
            new SelectListItem() { Text = "Active-Full", Value = "Active-Full" },
            new SelectListItem() { Text = "Active-Shell", Value = "Active-Shell" },
            new SelectListItem() { Text = "Inactive", Value = "Inactive" },
            new SelectListItem() { Text = "Pending-Contract", Value = "Pending-Contract" },
            new SelectListItem() { Text = "Pending-Onboarding", Value = "Pending-Onboarding" },
        };

        public static List<SelectListItem> PropertyList
        {
            get
            {
                return _propertyList;
            }
        }

        private static List<SelectListItem> _verticalList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "FS", Value = "FS" },
            new SelectListItem() { Text = "RS", Value = "RS" },
            new SelectListItem() { Text = "CHSS or Core", Value = "CHSS or Core" },
            new SelectListItem() { Text = "Core+", Value = "Core+" },
            new SelectListItem() { Text = "Elite", Value = "Elite" },
            new SelectListItem() { Text = "O/O+RN", Value = "O/O+RN" },
        };

        public static List<SelectListItem> VerticalList
        {
            get
            {
                return _verticalList;
            }
        }

        private static List<SelectListItem> _aproverList = new List<SelectListItem>()
        {
            //new SelectListItem() { Text = "", Value = null },
            new SelectListItem() { Text = "Amish", Value = "Amish" },
            new SelectListItem() { Text = "Andrew Liu", Value = "Andrew Liu" },
            new SelectListItem() { Text = "Caitlin", Value = "Caitlin" },
            new SelectListItem() { Text = "Christophe", Value = "Christophe" },
            new SelectListItem() { Text = "Eric", Value = "Eric" },
            new SelectListItem() { Text = "Genny Young", Value = "Genny Young" },
            new SelectListItem() { Text = "Kelly Cantrell", Value = "Kelly Cantrell" },
            new SelectListItem() { Text = "Mojdeh", Value = "Mojdeh" },
            new SelectListItem() { Text = "Rob", Value = "Rob" },
            new SelectListItem() { Text = "Steven Lee", Value = "Steven Lee" },
            new SelectListItem() { Text = "Temp Approver A", Value = "Temp Approver A" },
            new SelectListItem() { Text = "Temp Approver B", Value = "Temp Approver B" },
            new SelectListItem() { Text = "Tyler", Value = "Tyler" },
            new SelectListItem() { Text = "Yann Thiollet", Value = "Yann Thiollet" },
            new SelectListItem() { Text = "Pending Review", Value = "Pending Review" },
        };

        public static List<SelectListItem> AproverList
        {
            get
            {
                return _aproverList;
            }
        }

        private static List<SelectListItem> _approvedByOwnerList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Yes", Value = "YES" },
            new SelectListItem() { Text = "No", Value = "NO" },
            new SelectListItem() { Text = "Pending", Value = "PENDING" },
            new SelectListItem() { Text = "N/A", Value = "N/A" },
        };

        public static List<SelectListItem> ApprovedByOwnerList
        {
            get
            {
                return _approvedByOwnerList;
            }
        }

        private static List<SelectListItem> _pricingDesicionList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Yes", Value = "YES" },
            new SelectListItem() { Text = "No", Value = "NO" },
            new SelectListItem() { Text = "Pending", Value = "PENDING" },
            new SelectListItem() { Text = "N/A", Value = "N/A" },
            new SelectListItem() { Text = "COUNTEROFFER", Value = "COUNTEROFFER" },
            new SelectListItem() { Text = "HOLD", Value = "HOLD" },
            new SelectListItem() { Text = "TETRIS", Value = "TETRIS" },
            new SelectListItem() { Text = "TOO FAR OUT", Value = "TOO FAR OUT" },
            new SelectListItem() { Text = "NEED MORE INFO", Value = "NEED MORE INFO" },

        };

        public static List<SelectListItem> PricingDesicionList
        {
            get
            {
                return _pricingDesicionList;
            }
        }

        private static List<SelectListItem> _beltDesignationList = new List<SelectListItem>()
        {
            //new SelectListItem() { Text = "", Value = null },
            new SelectListItem() { Text = "Yellow Belt", Value = "Yellow belt" },
            new SelectListItem() { Text = "Black Belt", Value = "Black belt" },
            new SelectListItem() { Text = "White Belt", Value = "White belt" },
        };

        public static List<SelectListItem> BeltDesignationList
        {
            get
            {
                return _beltDesignationList;
            }
        }


        private static List<SelectListItem> _neighborhoodList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Astoria" },
            new SelectListItem() { Text = "Beverly Hills" },
            new SelectListItem() { Text = "Canyon Trails West" },
            new SelectListItem() { Text = "Chelsea" },
            new SelectListItem() { Text = "Copacabana" },
            new SelectListItem() { Text = "Creative District" },
            new SelectListItem() { Text = "Denver" },
            new SelectListItem() { Text = "DTLA" },
            new SelectListItem() { Text = "East Harlem" },
            new SelectListItem() { Text = "East Village" },
            new SelectListItem() { Text = "Financial District" },
            new SelectListItem() { Text = "Flatiron" },
            new SelectListItem() { Text = "Florianópolis" },
            new SelectListItem() { Text = "Gavea" },
            new SelectListItem() { Text = "Grand Rapids" },
            new SelectListItem() { Text = "HollyHills" },
            new SelectListItem() { Text = "HollyWeho" },
            new SelectListItem() { Text = "Indio" },
            new SelectListItem() { Text = "Ipanema" },
            new SelectListItem() { Text = "Jardins" },
            new SelectListItem() { Text = "Jefferson Park" },
            new SelectListItem() { Text = "LA" },
            new SelectListItem() { Text = "La Quinta" },
            new SelectListItem() { Text = "LABeach" },
            new SelectListItem() { Text = "Laguna Beach" },
            new SelectListItem() { Text = "Las Vegas" },
            new SelectListItem() { Text = "Leblon" },
            new SelectListItem() { Text = "Leblon / Zona Sul" },
            new SelectListItem() { Text = "Los Angeles" },
            new SelectListItem() { Text = "Lower East Side" },
            new SelectListItem() { Text = "Lower Manhattan" },
            new SelectListItem() { Text = "Malibu" },
            new SelectListItem() { Text = "Miami" },
            new SelectListItem() { Text = "Mid-City/" },
            new SelectListItem() { Text = "BevHills Adjacent" },
            new SelectListItem() { Text = "Midtown East" },
            new SelectListItem() { Text = "Midtown West" },
            new SelectListItem() { Text = "Old Town" },
            new SelectListItem() { Text = "Pacific Beach" },
            new SelectListItem() { Text = "Palm Springs" },
            new SelectListItem() { Text = "Paris" },
            new SelectListItem() { Text = "Phoenix" },
            new SelectListItem() { Text = "Praia Mole" },
            new SelectListItem() { Text = "Rio De Janeiro" },
            new SelectListItem() { Text = "San Francisco" },
            new SelectListItem() { Text = "Scottsdale" },
            new SelectListItem() { Text = "SoHo" },
            new SelectListItem() { Text = "Southampton" },
            new SelectListItem() { Text = "Tempe" },
            new SelectListItem() { Text = "The Mission" },
            new SelectListItem() { Text = "Tribeca" },
            new SelectListItem() { Text = "Upper East Side" },
            new SelectListItem() { Text = "Upper West Side" },
            new SelectListItem() { Text = "West Hollywood" },
            new SelectListItem() { Text = "West Village" },
        };

        public static List<SelectListItem> NeighborhoodList
        {
            get
            {
                return _neighborhoodList;
            }
        }

        private static List<SelectListItem> _areaList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Anaheim" },
            new SelectListItem() { Text = "Denver" },
            new SelectListItem() { Text = "DTLA" },
            new SelectListItem() { Text = "Florianópolis" },
            new SelectListItem() { Text = "Grand Rapids" },
            new SelectListItem() { Text = "Harlem" },
            new SelectListItem() { Text = "HollyHills" },
            new SelectListItem() { Text = "HollyWeho" },
            new SelectListItem() { Text = "Jardins" },
            new SelectListItem() { Text = "LA" },
            new SelectListItem() { Text = "LABeach" },
            new SelectListItem() { Text = "Laguna Beach" },
            new SelectListItem() { Text = "Las Vegas" },
            new SelectListItem() { Text = "Lower Manhattan" },
            new SelectListItem() { Text = "Malibu" },
            new SelectListItem() { Text = "Miami" },
            new SelectListItem() { Text = "Midtown East" },
            new SelectListItem() { Text = "Midtown West" },
            new SelectListItem() { Text = "Palm Springs" },
            new SelectListItem() { Text = "Paris" },
            new SelectListItem() { Text = "Phoenix" },
            new SelectListItem() { Text = "Rio De Janeiro" },
            new SelectListItem() { Text = "San Diego" },
            new SelectListItem() { Text = "San Francisco" },
            new SelectListItem() { Text = "Scottsdale" },
            new SelectListItem() { Text = "Southampton" },
            new SelectListItem() { Text = "Tempe" },
            new SelectListItem() { Text = "Upper East Side" },
            new SelectListItem() { Text = "Upper West Side" },
        };

        public static List<SelectListItem> AreaList
        {
            get
            {
                return _areaList;
            }
        }

        private static List<SelectListItem> _marketList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Denver" },
            new SelectListItem() { Text = "Florianópolis" },
            new SelectListItem() { Text = "Grand Rapids" },
            new SelectListItem() { Text = "Las Vegas" },
            new SelectListItem() { Text = "Los Angeles" },
            new SelectListItem() { Text = "Miami" },
            new SelectListItem() { Text = "New York" },
            new SelectListItem() { Text = "Paris" },
            new SelectListItem() { Text = "Phoenix" },
            new SelectListItem() { Text = "Rio De Janeiro" },
            new SelectListItem() { Text = "San Diego" },
            new SelectListItem() { Text = "San Francisco" },
            new SelectListItem() { Text = "Sao Paulo" },
            new SelectListItem() { Text = "Southampton" },
        };

        public static List<SelectListItem> MarketList
        {
            get
            {
                return _marketList;
            }
        }

        private static List<SelectListItem> _cityList = new List<SelectListItem>()
        {
            new SelectListItem() { Text = "Anaheim" },
            new SelectListItem() { Text = "Beverly Hills" },
            new SelectListItem() { Text = "Denver" },
            new SelectListItem() { Text = "Florianópolis" },
            new SelectListItem() { Text = "Goodyear" },
            new SelectListItem() { Text = "Grand Rapids" },
            new SelectListItem() { Text = "Hollywood" },
            new SelectListItem() { Text = "Hollywood Hills" },
            new SelectListItem() { Text = "Indio" },
            new SelectListItem() { Text = "Jurere Tradicional" },
            new SelectListItem() { Text = "La Quinta" },
            new SelectListItem() { Text = "Laguna Beach" },
            new SelectListItem() { Text = "Las Vegas" },
            new SelectListItem() { Text = "Los Angeles" },
            new SelectListItem() { Text = "Malibu" },
            new SelectListItem() { Text = "Manhattan Beach" },
            new SelectListItem() { Text = "Marina Del Rey" },
            new SelectListItem() { Text = "Menezes" },
            new SelectListItem() { Text = "Miami" },
            new SelectListItem() { Text = "New York" },
            new SelectListItem() { Text = "Palm Springs" },
            new SelectListItem() { Text = "Paris" },
            new SelectListItem() { Text = "Phoenix" },
            new SelectListItem() { Text = "Rio De Janeiro" },
            new SelectListItem() { Text = "San Diego" },
            new SelectListItem() { Text = "San Francisco" },
            new SelectListItem() { Text = "Santa Monica" },
            new SelectListItem() { Text = "Scottsdale" },
            new SelectListItem() { Text = "Venice" },
            new SelectListItem() { Text = "West Hollywood" },
            new SelectListItem() { Text = "Westwood" },
            new SelectListItem() { Text = "Southampton" },
            new SelectListItem() { Text = "Sao Paulo" },
        };

        public static List<SelectListItem> CityList
        {
            get
            {
                return _cityList;
            }
        }
    }
}
