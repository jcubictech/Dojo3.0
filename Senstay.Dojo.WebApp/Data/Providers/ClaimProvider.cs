using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Collections.Generic;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class ClaimProvider
    {
        //private readonly DojoDbContext _context;

        public static string GetFavoriteIcon(DojoDbContext _context, string url)
        {
            string favoriteIconClass = "fa-heart-o";
            try
            {
                var userName = ClaimsPrincipal.Current.Identity.Name;
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@UserName", SqlDbType.VarChar);
                sqlParams[0].Value = userName;

                string page = _context.Database.SqlQuery<string>("GetFavoritePage @UserName", sqlParams).FirstOrDefault();
                if (!string.IsNullOrEmpty(page) && url.ToLower().EndsWith(page.ToLower()))
                    favoriteIconClass = "fa-heart";
            }
            catch
            {
            }
            return favoriteIconClass;
        }

        public static string GetUserId(DojoDbContext _context)
        {
            string userId = "unknown";
            try
            {
                var userName = ClaimsPrincipal.Current.Identity.Name;
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@UserName", SqlDbType.VarChar);
                sqlParams[0].Value = userName;

                userId = _context.Database.SqlQuery<string>("GetUserId @UserName", sqlParams).FirstOrDefault();
                if (string.IsNullOrEmpty(userId)) userId = "unknown";
            }
            catch
            {
            }
            return userId;
        }

        public static string GetFriendlyName(DojoDbContext _context)
        {
            string userName = "unknown";
            try
            {
                userName = ClaimsPrincipal.Current.Identity.Name;
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@UserName", SqlDbType.VarChar);
                sqlParams[0].Value = userName;

                userName = _context.Database.SqlQuery<string>("GetUserName @UserName", sqlParams).FirstOrDefault();
                if (string.IsNullOrEmpty(userName)) userName = "unknown";
            }
            catch
            {
            }
            return userName;
        }
    }
}
