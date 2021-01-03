using DocumentManagement.Modularization;
using DocumentManagement.Statistics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DocumentManagement
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var modules = new ModuleRegistry(new StatisticsModule());

            services
                .AddServicesFrom(modules)
                .AddMvcCore()
                .ConfigureMvcUsing(modules);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(o => o.MapControllers());
        }
    }
}