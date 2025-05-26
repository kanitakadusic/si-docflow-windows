using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace docflow
{
    public static class AppConfig
    {
        private static IConfigurationRoot config;

        static AppConfig()
        {
            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
        }

        public static string Get(string key) => config[key];

        public static string docflow_api => Get("docflow_api");
        public static string machine_id => Get("machine_id");
        public static string admin_api => Get("admin_api");
        public static string lang => Get("lang");
        public static string engine => Get("engine");
        public static int defaultPort => int.Parse(Get("defaultPort"));
        public static string portCheckingSite => Get("portCheckingSite");
        public static string AppTitle => Get("AppTitle");
        public static string defaultLocation => Get("defaultLocation");
        public static string DefaultWorkingMode => Get("DefaultWorkingMode");
        public static int defaultPollingFrequency => int.Parse(Get("defaultPollingFrequency"));
        public static List<string> defaultExtensions =>
           config.GetSection("defaultExtensions").Get<List<string>>();

    }
}
