using System;

namespace DocumentManagement.Common
{
    internal static class TypeExtensions
    {
        public static bool Implements<TInterface>(this Type type)
        {
            return Implements(type, typeof(TInterface));
        }

        private static bool Implements(this Type type, Type expectedBaseType)
        {
            // This method is explicitly not cached using Memoize or any other technique. Doing so slows it down.
            return expectedBaseType.IsAssignableFrom(type) && (type != expectedBaseType);
        }
    }
}