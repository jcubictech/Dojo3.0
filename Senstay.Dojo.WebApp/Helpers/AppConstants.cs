namespace Senstay.Dojo.Helpers
{
    public static class AppConstants
    {
        public const string APP_ASSEMBLY_PREFIX = "Senstay";
        public const string SUPER_ADMIN_ROLE = "SenstayAdmin";
        public const string ADMIN_ROLE = "Admin";
        public const string VIEWER_ROLE = "Viewer";
        public const string APPROVER_ROLE = "Approver";
        public const string EDITOR_ROLE = "AllEditor";
        public const string PROPERTY_EDITOR_ROLE = "PropertyEditor";
        public const string ACCOUNT_EDITOR_ROLE = "AccountEditor";
        public const string INQUIRY_EDITOR_ROLE = "InquiryEditor";
        public const string REVENUE_CREATOR_ROLE = "RevenueCreator";
        public const string REVENUE_VIEWER_ROLE = "RevenueViewer";
        public const string REVENUE_REVIEWER_ROLE = "RevenueReviewer";
        public const string REVENUE_APPROVER_ROLE = "RevenueApprover";
        public const string REVENUE_FINALIZER_ROLE = "RevenueFinalizer";
        public const string REVENUE_OWNER_ROLE = "RevenueOwner";
        public const string REVENUE_ADMIN_ROLE = "RevenueAdmin";
        public const string STATEMENT_VIEWER_ROLE = "StatementViewer";
        public const string STATEMENT_OWNER_ROLE = "StatementOwner";
        public const string STATEMENT_ADMIN_ROLE = "StatementAdmin";
        public const string STATEMENT_EDITOR_ROLE = "StatementEditor";
        public const string DATA_IMPORTOR_ROLE = "DataImportor";

        public const string TRANSACTION_KEY = "_Transaction";
        public const string TRANSACTION_ERROR_KEY = "_TransactionError";
        public const string ERROR_KEY = "_Error";
        public const string ALERT_KEY = "_Alerts";
        public const string PARTIAL_ALERT_KEY = "_PartialAlerts";
        public const string JSON_ALERT_KEY = "_JsonAlerts";
        public const string CONTAINER_KEY = "_Container";

        public const string MAINTENANCE_ACTION_NAME = "Maintenance";
        public const string ONLINE_ESTIMATE_TIME = "EstimatedOnlineTime";
        public const string SEED_DATA_ALWAYS = "SeedDataAlways";
        public const string AUDIT_TABLES = "AuditTables";
        public const string HTTPS_ONLY = "HttpsOnly";
        public const string LOG_LEVEL = "LogLevel";
        public const string DOJO_CONNECTION_NAME = "DojoDbConnection";
        public const string GOOGLE_EMAIL_DOMAIN = "google.com";

        public const string DOWNLOAD_COOKIE_NAME = "DojoDownload";
        public const string COOKIE_DONE = "done";
        public const string COOKIE_ERROR = "error";

        public const string EMAIL_SUPPORT_KEY = "AwsSupportEmail";
        public const string EMAIL_DEVELOPER_KEY = "AwsDeveloperEmail";
        public const string EMAIL_SENDER_KEY = "AwsEmailSender";
        public const string EMAIL_HOST_KEY = "AwsEmailHost";
        public const string EMAIL_PORT_KEY = "AwsEmailPort";
        public const string SMTP_USER_KEY = "AwsSmtpUser";
        public const string SMTP_USER_CODE_KEY = "AwsSmtpUserCode";
        public const string EMAIL_HOST_DEFAULT = "email-smtp.us-west-2.amazonaws.com";
        public const int EMAIL_PORT_DEFAULT = 587; // SSL-enabled (port can be 25, 465 or 587)

        public const string INVITATION_CLAIM = "Invitation";
        public const string INVITATION_CODE_CLAIM = "InvitationCode";
        public const string EXPIRATION_DATE_CLAIM = "ExpirationDate";
        public const string ROLES_CLAIM = "Roles";

        public const string AIRBNB_URL_TEMPLATE = "https://www.airbnb.com/rooms/{0}";
        public const string DOJO_DEV_LOCAL = "localhost";
        public const string DOJO_DEV_AWS_ELASTIC_IP = "DevElasticIP";

        public const string FAVORITE_PAGE = "FavoritePage";

        public const string DEFAULT_PROPERTY_CODE = "PropertyPlaceholder";
    }
}