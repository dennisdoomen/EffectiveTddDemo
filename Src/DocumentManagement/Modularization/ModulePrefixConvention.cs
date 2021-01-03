using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace DocumentManagement.Modularization
{
    internal class ModulePrefixConvention : IApplicationModelConvention
    {
        private readonly IReadOnlyDictionary<Type, BaseRoute> controllerToModulePrefixMap;

        public ModulePrefixConvention(IReadOnlyDictionary<Type, BaseRoute> controllerToModulePrefixMap)
        {
            this.controllerToModulePrefixMap =
                controllerToModulePrefixMap ?? throw new ArgumentNullException(nameof(controllerToModulePrefixMap));
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                if (controllerToModulePrefixMap.TryGetValue(controller.ControllerType, out var controllerPrefix))
                {
                    var controllerSelectorsWithRoutes = controller.Selectors.Where(SelectorContainsRouteAttribute);

                    var onlyPrefixRoute = new AttributeRouteModel(new RouteAttribute(controllerPrefix.ToString()));

                    var prefixAdded = false;

                    foreach (var selectorModel in controllerSelectorsWithRoutes)
                    {
                        AddPrefixesToExistingRoutes(selectorModel, onlyPrefixRoute);
                        prefixAdded = true;
                    }

                    if (!prefixAdded)
                    {
                        throw new InvalidOperationException(
                            $"{controller.ControllerType} must have an explicit routing provided by the RouteAttribute");
                    }
                }
            }
        }

        private static bool SelectorContainsRouteAttribute(SelectorModel selectorModel)
        {
            return selectorModel.AttributeRouteModel != null;
        }

        private static void AddPrefixesToExistingRoutes(SelectorModel selectorModel, AttributeRouteModel prefixRoute)
        {
            var originalAttributeRoute = selectorModel.AttributeRouteModel;

            selectorModel.AttributeRouteModel =
                AttributeRouteModel.CombineAttributeRouteModel(prefixRoute, originalAttributeRoute);
        }
    }
}
