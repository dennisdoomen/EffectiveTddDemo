using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Raven.Client.Documents.Session;

namespace DocumentManagement
{
    public static class MiddleWareExtensions
    {
        public static IApplicationBuilder UseStatistics(this IApplicationBuilder appBuilder, Func<IAsyncDocumentSession> sessionFactory)
        {
            appBuilder.IsolatedMap(
                "/statistics", 
                app => app.UseMvc(), 
                services =>
            {
                services
                    .AddMvc()
                    .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());;
                
                services.AddSingleton(sessionFactory);
            });
            
            return appBuilder;
        }
    }
}