using System;
using System.Collections.Generic;
using DocumentManagement.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DocumentManagement.Modularization
{
    public sealed class ModuleRegistry
    {
        private readonly List<IModule> modules = new List<IModule>();

        public ModuleRegistry(params IModule[] modules)
        {
            Add(modules);
        }

        public IModule[] Modules => modules.ToArray();

        public void Add(params IModule[] modules)
        {
            this.modules.AddRange(modules);

            ValidateDependencies();
            ValidateHostedServices();
            ValidateControllers();
        }

        private void ValidateDependencies()
        {
            var registeredServices = new HashSet<Type>();
            foreach (var module in modules)
            {
                foreach (ServiceDescriptor dependency in module.Dependencies)
                {
                    Type dependencyType = dependency.ServiceType;

                    var moduleName = module.GetType().Name;
                    if (typeof(IHostedService).IsAssignableFrom(dependency.ImplementationType))
                    {
                        throw new InvalidOperationException(
                            $"Don't register a hosted service {dependencyType} as a dependency of module {moduleName}");
                    }

                    if (!registeredServices.Add(dependencyType))
                    {
                        throw new InvalidOperationException(
                            $"Dependency {dependencyType} of {moduleName} was already registered by another module");
                    }
                }
            }
        }

        private void ValidateHostedServices()
        {
            var registeredHostedServices = new HashSet<Type>();
            foreach (IModule module in modules)
            {
                foreach (ServiceDescriptor serviceDescriptor in module.HostedServices)
                {
                    var moduleName = module.GetType().Name;

                    if (serviceDescriptor.ServiceType != typeof(IHostedService))
                    {
                        throw new InvalidOperationException(
                            $"{serviceDescriptor.ServiceType} exposed by module {moduleName} is not a {nameof(IHostedService)}");
                    }

                    if (serviceDescriptor.ImplementationType == null)
                    {
                        throw new InvalidOperationException(
                            $"Module {moduleName} must provide an implementation for each hosted service it exposes");
                    }

                    if (!serviceDescriptor.ImplementationType.Implements<IHostedService>())
                    {
                        throw new InvalidOperationException(
                            $"Module {moduleName} exposes a hosted service with implementation " +
                            $"{serviceDescriptor.ImplementationType} which is not a {nameof(IHostedService)}");
                    }

                    if (!registeredHostedServices.Add(serviceDescriptor.ImplementationType))
                    {
                        throw new InvalidOperationException(
                            $"{moduleName} exposes a duplicate implementation of {serviceDescriptor.ImplementationType} as a hosted service");
                    }
                }
            }
        }

        private void ValidateControllers()
        {
            var registeredControllers = new HashSet<Type>();

            foreach (IModule module in modules)
            {
                var moduleName = module.GetType().Name;

                foreach (Type controllerType in module.ControllerTypes)
                {
                    if (module.BaseRoute == null)
                    {
                        throw new InvalidOperationException(
                            $"Module {moduleName} cannot register controllers without specifying the base route");
                    }

                    if (!controllerType.IsSubclassOf(typeof(ControllerBase)))
                    {
                        throw new InvalidOperationException(
                            $"{controllerType} exposed by {moduleName} is not a subclass of {nameof(ControllerBase)}");
                    }

                    if (!registeredControllers.Add(controllerType))
                    {
                        throw new InvalidOperationException(
                            $"Controller {controllerType} exposed by module {moduleName} was already exposed by another module");
                    }
                }
            }
        }
    }
}