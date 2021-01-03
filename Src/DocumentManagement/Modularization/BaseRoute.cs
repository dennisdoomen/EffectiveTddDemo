using System;

namespace DocumentManagement.Modularization
{
    /// <summary>
    /// Represents a route prefix
    /// <para>Can be both a simple route 'moduleOne'
    /// and a composite template route 'moduleOne/region/{regionCode}'</para>
    /// </summary>
    public sealed class BaseRoute
    {
        private readonly string routePrefix;

        /// <summary>
        /// Creates an instance of the RoutePrefix.
        /// <para>Route must not start with '/', must not end with '/', must not contain white spaces,
        /// must not contain '\', must not contain '//'</para>
        /// </summary>
        /// <param name="routePrefix">A route prefix</param>
        /// <exception cref="System.ArgumentNullException">Thrown when route prefix is null</exception>
        /// <exception cref="System.ArgumentException">Thrown when route prefix contains illegal characters</exception>
        public BaseRoute(string routePrefix)
        {
            this.routePrefix = routePrefix ?? throw new ArgumentNullException(nameof(routePrefix));

            AssertEmptyPrefix();
            AssertPrefixEndings(@"/");
            AssertIllegalSymbolInThePrefix(@"//");
            AssertIllegalSymbolInThePrefix(@" ");
            AssertIllegalSymbolInThePrefix(@"\");
        }

        private void AssertEmptyPrefix()
        {
            if (routePrefix == string.Empty)
            {
                throw new ArgumentException("Route prefix must not be empty");
            }
        }

        private void AssertPrefixEndings(string illegalSymbol)
        {
            if (routePrefix.StartsWith(illegalSymbol))
            {
                throw new ArgumentException($"Route prefix: '{routePrefix}' must not start with: '{illegalSymbol}'");
            }

            if (routePrefix.EndsWith(illegalSymbol))
            {
                throw new ArgumentException($"Route prefix: '{routePrefix}' must not end with: '{illegalSymbol}'");
            }
        }

        private void AssertIllegalSymbolInThePrefix(string illegalSymbol)
        {
            if (routePrefix.Contains(illegalSymbol))
            {
                throw new ArgumentException($"Route prefix: '{routePrefix}' must not contain: '{illegalSymbol}'");
            }
        }

        public override string ToString()
        {
            return routePrefix;
        }
    }
}