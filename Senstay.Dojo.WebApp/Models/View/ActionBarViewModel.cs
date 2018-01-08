using System.Collections.Generic;

namespace Senstay.Dojo.Models.View
{
    public class ActionBarViewModel
    {
        public ActionBarViewModel()
        {
            CustomFilter1 = new List<CustomFilterType>();
            CustomFilter2 = new List<CustomFilterType>();
            CustomFilter3 = new List<CustomFilterType>();
        }

        public List<CustomFilterType> CustomFilter1 { get; set; }
        public List<CustomFilterType> CustomFilter2 { get; set; }
        public List<CustomFilterType> CustomFilter3 { get; set; }
    }

    public class CustomFilterType
    {
        public string DisplayName { get; set; }
        public string ID { get; set; }
        public string Field { get; set; }
        public FilterType Type { get; set; }
    }

    public enum FilterType
    {
        Button = 0,
        Checkbox = 1,
        Radio = 2
    }
}