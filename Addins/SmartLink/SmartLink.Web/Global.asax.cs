using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using System.Reflection;
using System.Configuration;
using SmartLink.Service;
using Microsoft.Azure;
using System.Data.Entity;
using System.Globalization;
using System.Threading;

namespace SmartLink.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            //Configure ApplicationInsights instrumentation key
            Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.InstrumentationKey = CloudConfigurationManager.GetSetting("InstrumentationKey");

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            RegisterIoC();

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<SmartlinkDbContext, SmartLink.Service.Migrations.Configuration>());
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            var userLanguages = Request.UserLanguages;
            var browserLanguage = userLanguages.FirstOrDefault();

            if (browserLanguage != null)
            {
                CultureInfo browserCulture = CultureInfo.DefaultThreadCurrentCulture;
                try
                {
                    browserCulture = new CultureInfo(browserLanguage);
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = browserCulture;
                    Thread.CurrentThread.CurrentUICulture = browserCulture;
                }
            }
        }

        private void RegisterIoC()
        {
            var builder = new ContainerBuilder();

            builder.RegisterApiControllers(typeof(MvcApplication).Assembly);
            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            AutofacBootstrap.Init(builder);

            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}
