using Microsoft.Extensions.Configuration;
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
    }
}
