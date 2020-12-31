using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagement.Modularization
{
    internal static class MvcCoreExtensions
    {
        public static IMvcCoreBuilder IgnoreAccessModifiersOfControllers(this IMvcCoreBuilder mvcCoreBuilder)
        {
            return mvcCoreBuilder.ConfigureApplicationPartManager(
                partManager => partManager.FeatureProviders.Add(new CustomControllerFeatureProvider()));
        }

        public static IMvcCoreBuilder AddSpecificControllers(this IMvcCoreBuilder mvcCoreBuilder, IEnumerable<Type> controllerTypes)
        {
            return mvcCoreBuilder.ConfigureApplicationPartManager(
                partManager => partManager.AddSpecificControllers(controllerTypes));
        }

        private static void AddSpecificControllers(this ApplicationPartManager partManager, IEnumerable<Type> controllerTypes)
        {
            var existingSelectedControllersApplicationParts =
                partManager.ApplicationParts.OfType<SelectedControllersApplicationParts>().SingleOrDefault();

            var existingControllerTypes =
                existingSelectedControllersApplicationParts?.Types ?? Enumerable.Empty<Type>(); // Capture

            partManager.ApplicationParts.Clear();

            partManager.ApplicationParts.Add(
                new SelectedControllersApplicationParts(existingControllerTypes.Concat(controllerTypes)));
        }

        private class SelectedControllersApplicationParts : ApplicationPart, IApplicationPartTypeProvider
        {
            public SelectedControllersApplicationParts(IEnumerable<Type> types)
            {
                Types = types.Select(x => x.GetTypeInfo()).ToArray();
            }

            public override string Name { get; } = "Only allow selected controllers";

            public IEnumerable<TypeInfo> Types { get; }
        }

        private class CustomControllerFeatureProvider : ControllerFeatureProvider
        {
            protected override bool IsController(TypeInfo typeInfo)
            {
                // this is exact copy from the ControllerFeatureProvider apart from public class limitation
                return typeInfo.IsClass && !typeInfo.IsAbstract && !typeInfo.ContainsGenericParameters &&
                       !typeInfo.IsDefined(typeof(NonControllerAttribute)) &&
                       (typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ||
                        typeInfo.IsDefined(typeof(ControllerAttribute)));
            }
        }
    }
}
