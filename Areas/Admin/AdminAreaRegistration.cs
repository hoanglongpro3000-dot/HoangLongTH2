using System.Web.Mvc;
using System.Web.Routing;

namespace HoangLongTH.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Admin";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                name: "Admin_default",
                url: "Admin/{controller}/{action}/{id}",
                defaults: new { controller = "AdminHome", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "HoangLongTH.Areas.Admin.Controllers" }
            );
        }
    }
}
