using System.Collections.Generic;

namespace Senstay.Dojo.Models.View
{
    // support only 2 level deep menu system
    public class MenuViewModel
    {
        public MenuViewModel()
        {
            Id = string.Empty;
            Name = string.Empty;
            LinkUrl = string.Empty;
            SubMenus = new List<SubmenuViewModel>();
        }


        public string Id { get; set; }
        public string Name { get; set; }
        public string LinkUrl { get; set; }
        public List<SubmenuViewModel> SubMenus { get; set; }
    }

    public class SubmenuViewModel
    {
        public SubmenuViewModel()
        {
            Id = string.Empty;
            Name = string.Empty;
            LinkUrl = string.Empty;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string LinkUrl { get; set; }
    }
}