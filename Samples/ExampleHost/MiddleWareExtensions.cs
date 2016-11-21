using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Owin;
using Raven.Client;
using TinyIoC;

namespace LiquidProjections.ExampleHost
{
    public static class MiddleWareExtensions
    {
        public static IAppBuilder UseStatistics(this IAppBuilder app, Func<IAsyncDocumentSession> sessionFactory)
        {
            var container = new TinyIoCContainer();
            container.Register(sessionFactory);

            HttpConfiguration configuration = BuildHttpConfiguration(container);
            app.Map("/api", a => a.UseWebApi(configuration));

            return app;
        }

        private static HttpConfiguration BuildHttpConfiguration(TinyIoCContainer container)
        {
            var configuration = new HttpConfiguration
            {
                DependencyResolver = new TinyIocWebApiDependencyResolver(container),
                IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always
            };

            configuration.Services.Replace(typeof(IHttpControllerTypeResolver), new MiddleWareExtensions.ControllerTypeResolver());
            configuration.MapHttpAttributeRoutes();

            return configuration;
        }

        internal class ControllerTypeResolver : IHttpControllerTypeResolver
        {
            public ICollection<Type> GetControllerTypes(IAssembliesResolver assembliesResolver)
            {
                return new List<Type>
                {
                    typeof(StatisticsController)
                };
            }
        }
    }
}