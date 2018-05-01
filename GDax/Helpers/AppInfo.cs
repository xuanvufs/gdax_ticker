using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GDax.Helpers
{
    public static class AppInfo
    {
        private static readonly Assembly _entryAssembly = Assembly.GetEntryAssembly();
        private static readonly string _bastPath = AppDomain.CurrentDomain.BaseDirectory;

        public static string GetFullPath(string relativePath)
        {
            return Path.Combine(_bastPath, relativePath);
        }

        public static FileVersionInfo GetVersionInfo()
        {
            return FileVersionInfo.GetVersionInfo(_entryAssembly.Location);
        }

        public static string GetEntryAssemblyName()
        {
            return _entryAssembly?.GetName()?.Name;
        }

        public static Stream GetResourceStream(string resource)
        {
            return _entryAssembly.GetManifestResourceStream($"{_entryAssembly.GetName().Name}.{resource}");
        }

        public static string GetCompanyName()
        {
            var attribute = _entryAssembly.GetCustomAttributes<AssemblyCompanyAttribute>().FirstOrDefault();
            var company = attribute?.Company;

            if (string.IsNullOrWhiteSpace(company))
                company = GetVersionInfo().CompanyName;

            if (string.IsNullOrWhiteSpace(company))
            {
                var ns = _entryAssembly.EntryPoint?.ReflectedType?.Namespace ?? string.Empty;
                var idx = ns.IndexOf(".", StringComparison.OrdinalIgnoreCase);
                company = idx != -1 ? ns.Substring(0, idx) : ns;
            }

            return company;
        }
    }
}