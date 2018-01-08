using System.Collections.Generic;
using System.Security.Claims;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public static class AuthorizationProvider
    {
        public static bool IsAuthenticated()
        {
            return ClaimsPrincipal.Current.Identity.IsAuthenticated;
        }

        public static bool HasRole()
        {
            return ClaimsPrincipal.Current.Identity.IsAuthenticated && IsViewer();
        }

        public static bool NoRoleAssignment()
        {
            return ClaimsPrincipal.Current.Identity.IsAuthenticated &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.VIEWER_ROLE) &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.EDITOR_ROLE) &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.ACCOUNT_EDITOR_ROLE) &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.PROPERTY_EDITOR_ROLE) &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.INQUIRY_EDITOR_ROLE) &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.APPROVER_ROLE) &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.ADMIN_ROLE) &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.SUPER_ADMIN_ROLE);

        }

        #region App roles
        public static bool IsSuperAdmin()
        {
            return ClaimsPrincipal.Current.IsInRole(AppConstants.SUPER_ADMIN_ROLE);
        }

        public static bool IsAdmin()
        {
            return IsSuperAdmin() || ClaimsPrincipal.Current.IsInRole(AppConstants.ADMIN_ROLE);
        }

        public static bool IsAppover()
        {
            return IsAdmin() || ClaimsPrincipal.Current.IsInRole(AppConstants.APPROVER_ROLE);
        }

        public static bool IsEditor()
        {
            return IsAdmin() || IsAppover() || ClaimsPrincipal.Current.IsInRole(AppConstants.EDITOR_ROLE);
        }

        public static bool IsPropertyEditor()
        {
            return IsAdmin() || IsEditor() || ClaimsPrincipal.Current.IsInRole(AppConstants.PROPERTY_EDITOR_ROLE);
        }

        public static bool IsAccountEditor()
        {
            return IsAdmin() || IsEditor() || ClaimsPrincipal.Current.IsInRole(AppConstants.ACCOUNT_EDITOR_ROLE);
        }

        public static bool IsInquiryEditor()
        {
            return IsAdmin() || IsEditor() || ClaimsPrincipal.Current.IsInRole(AppConstants.INQUIRY_EDITOR_ROLE);
        }

        public static bool IsViewer()
        {
            return IsEditor() 
                    || ClaimsPrincipal.Current.IsInRole(AppConstants.PROPERTY_EDITOR_ROLE)
                    || ClaimsPrincipal.Current.IsInRole(AppConstants.ACCOUNT_EDITOR_ROLE)
                    || ClaimsPrincipal.Current.IsInRole(AppConstants.INQUIRY_EDITOR_ROLE)
                    || ClaimsPrincipal.Current.IsInRole(AppConstants.APPROVER_ROLE)
                    || ClaimsPrincipal.Current.IsInRole(AppConstants.VIEWER_ROLE);
        }

        public static bool IsDataImporter()
        {
            return ClaimsPrincipal.Current.IsInRole(AppConstants.DATA_IMPORTER_ROLE);
        }

        #endregion

        #region Revenue roles

        public static bool IsRevenueAdmin()
        {
            return ClaimsPrincipal.Current.IsInRole(AppConstants.REVENUE_ADMIN_ROLE);
        }

        public static bool IsRevenueOwner()
        {
            return ClaimsPrincipal.Current.IsInRole(AppConstants.REVENUE_OWNER_ROLE);
        }

        public static bool IsRevenueViewer()
        {
            return ClaimsPrincipal.Current.IsInRole(AppConstants.REVENUE_VIEWER_ROLE);
        }

        public static bool CanReviewRevenue()
        {
            return IsRevenueAdmin() || ClaimsPrincipal.Current.IsInRole(AppConstants.REVENUE_REVIEWER_ROLE);
        }

        public static bool CanEditRevenue()
        {
            return CanReviewRevenue() || CanApproveRevenue() || CanFinalizeRevenue();
        }

        public static bool CanApproveRevenue()
        {
            return IsRevenueAdmin() ||  ClaimsPrincipal.Current.IsInRole(AppConstants.REVENUE_APPROVER_ROLE);
        }

        public static bool CanFinalizeRevenue()
        {
            return IsRevenueAdmin() || ClaimsPrincipal.Current.IsInRole(AppConstants.REVENUE_FINALIZER_ROLE);
        }

        public static bool CanViewRevenue()
        {
            return CanReviewRevenue() || CanApproveRevenue() || CanFinalizeRevenue() ||
                   IsRevenueOwner() || IsRevenueViewer();
        }

        public static bool HasSpecialRevenueView()
        {
            return CanReviewRevenue() || CanApproveRevenue() || CanFinalizeRevenue() || IsRevenueOwner();
        }

        public static bool IsStatementViewer()
        {
            return ClaimsPrincipal.Current.IsInRole(AppConstants.STATEMENT_VIEWER_ROLE);
        }

        public static bool IsStatementOwner()
        {
            return ClaimsPrincipal.Current.IsInRole(AppConstants.STATEMENT_OWNER_ROLE);
        }

        public static bool IsStatementAdmin()
        {
            return ClaimsPrincipal.Current.IsInRole(AppConstants.STATEMENT_ADMIN_ROLE);
        }

        public static bool CanViewStatement()
        {
            return IsStatementAdmin() || IsStatementOwner() || IsStatementViewer();
        }

        public static bool CanEditStatement()
        {
            return IsStatementAdmin() || ClaimsPrincipal.Current.IsInRole(AppConstants.STATEMENT_EDITOR_ROLE);
        }

        public static bool CanExportStatement()
        {
            return IsStatementAdmin() || IsStatementOwner();
        }

        public static bool CanCreateStatement()
        {
            return IsStatementAdmin();
        }

        public static bool CanFreezeEditing()
        {
            return ClaimsPrincipal.Current.IsInRole(AppConstants.FINANCIAL_ADMIN_ROLE);
        }

        #endregion
    }
}
