using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagement.Modularization
{
    public interface IModule
    {
        /// <summary>
        /// The base route of the module where all the endpoints of the module will be hosted
        /// </summary>
        BaseRoute BaseRoute { get; }

        /// <summary>
        /// The dependencies this module exposes or needs itself in its hosted services and controllers 
        /// </summary>
        IEnumerable<ServiceDescriptor> Dependencies { get; }

        /// <summary>An enumerable of types representing controllers owned and exposed by module</summary>
        /// <remarks>Default spa controller should not be part of controller types returned</remarks>
        IEnumerable<Type> ControllerTypes { get; }

        /// <summary>
        /// An enumerable of service descriptors representing hosted services (jobs) defined by module
        /// </summary>
        IEnumerable<ServiceDescriptor> HostedServices { get; }
    }
}