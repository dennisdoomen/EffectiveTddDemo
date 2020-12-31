using System;

namespace DocumentManagement.Modularization
{
    internal static class NamingConventions
    {
        private const string ModuleTypeNameSuffix = "Module";

        public static string GetModuleName(Type moduleType)
        {
            if (moduleType == null)
            {
                throw new ArgumentNullException(nameof(moduleType));
            }

            var fullModuleName = moduleType.Name;

            return fullModuleName.EndsWith(ModuleTypeNameSuffix)
                ? fullModuleName.Substring(0, fullModuleName.Length - ModuleTypeNameSuffix.Length)
                : fullModuleName;
        }
    }
}
