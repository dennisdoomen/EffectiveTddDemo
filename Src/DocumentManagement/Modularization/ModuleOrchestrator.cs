using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DocumentManagement.Modularization
{
    public static class WebHostBuilderExtensions
    {
        public static IServiceCollection AddServicesFrom(this IServiceCollection services, ModuleRegistry moduleRegistry)
        {
            foreach (var module in moduleRegistry.Modules)
            {
                foreach (var serviceDescriptor in module.Dependencies)
                {
                    services.TryAdd(serviceDescriptor);
                }

                services.Add(module.HostedServices);
            }

            return services;
        }

        public static IMvcCoreBuilder ConfigureMvcUsing(this IMvcCoreBuilder mvcCoreBuilder, ModuleRegistry moduleRegistry)
        {
            var controllerTypes = new List<Type>();

            var controllerToModulePrefixMap = new Dictionary<Type, BaseRoute>();
            foreach (var module in moduleRegistry.Modules)
            {
                var moduleControllerTypes = module.ControllerTypes;
                foreach (var moduleControllerType in moduleControllerTypes)
                {
                    controllerToModulePrefixMap.Add(moduleControllerType, module.BaseRoute);
                    controllerTypes.Add(moduleControllerType);
                }
            }

            return mvcCoreBuilder
                .AddMvcOptions(o => o.Conventions.Insert(0, new ModulePrefixConvention(controllerToModulePrefixMap)))
                .AddSpecificControllers(controllerTypes);
        }
    }
}
