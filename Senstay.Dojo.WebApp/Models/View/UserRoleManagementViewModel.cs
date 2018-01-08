using System.Collections.Generic;

namespace Senstay.Dojo.Models.View
{
    public class UserRoleManagementViewModel
    {
        public string UserId { get; set; }

        public string UserName { get; set; }

        public ICollection<CustomTuple> UserRoles { get; set; }
    }

    public class CustomTuple
    {
        public CustomTuple()
        {
            Id = string.Empty;
            Text = string.Empty;
        }

        public CustomTuple(string id = "", string text = "")
        {
            Id = id;
            Text = text;
        }

        public string Text { get; set; }
        public string Id { get; set; }
    }
}