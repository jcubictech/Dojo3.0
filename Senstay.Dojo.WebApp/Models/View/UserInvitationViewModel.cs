using System;
using System.Collections.Generic;

namespace Senstay.Dojo.Models.View
{
    public class UserInvitationViewModel
    {
        public string UserId { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public string InvitationCode { get; set; }

        public DateTime ExpirationDate { get; set; }

        public string MobilePhone { get; set; }

        public string Provider { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }

        public ICollection<CustomTuple> UserRoles { get; set; }
    }
}