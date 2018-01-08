using System.Web.Optimization;

namespace Senstay.Dojo
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
            "~/Scripts/jquery-ui-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate.min.js",
                        "~/Scripts/jquery.validate.unobtrusive.min.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                        "~/Scripts/bootstrap.min.js",
                        "~/Scripts/underscore.min.js",
                        "~/Scripts/respond.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/app").Include(
                        "~/Scripts/jquery.blockUI.js",
                        "~/Scripts/jquery.cookie-1.4.1.min.js",
                        "~/Scripts/datejs.js",
                        "~/Scripts/app/kendo.all.js",
                        "~/Scripts/app/services/bootstrapAlerts.js",
                        "~/Scripts/bootstrap-multiselect.js",
                        "~/Scripts/app/dojoCommon.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                        "~/Content/font-awesome.min.css",
                        "~/Content/kendo.common.min.css",
                        "~/Content/bootstrap.css",
                        "~/Content/kendo.bootstrap.min.css",
                        "~/Content/bootstrap-multiselect.css"));

            bundles.Add(new StyleBundle("~/Content/themes/base/css").Include(
                          "~/Content/themes/base/core.css",
                          "~/Content/themes/base/resizable.css",
                          "~/Content/themes/base/selectable.css",
                          "~/Content/themes/base/accordion.css",
                          "~/Content/themes/base/autocomplete.css",
                          "~/Content/themes/base/button.css",
                          "~/Content/themes/base/dialog.css",
                          "~/Content/themes/base/slider.css",
                          "~/Content/themes/base/tabs.css",
                          "~/Content/themes/base/datepicker.css",
                          "~/Content/themes/base/progressbar.css",
                          "~/Content/themes/base/theme.css"));

            bundles.Add(new ScriptBundle("~/bundles/inputmask").Include(
                        "~/Scripts/Inputmask/inputmask.js",
                        "~/Scripts/Inputmask/jquery.inputmask.js",
                        "~/Scripts/Inputmask/inputmask.extensions.js",
                        "~/Scripts/Inputmask/inputmask.date.extensions.js",
                        //and other extensions you want to include
                        "~/Scripts/Inputmask/inputmask.numeric.extensions.js"));
        }
    }
}
