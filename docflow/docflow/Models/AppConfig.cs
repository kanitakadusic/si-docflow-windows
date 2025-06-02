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

        public static string PROCESSING_SERVER_BASE_URL => Get("PROCESSING_SERVER_BASE_URL");
        public static string MACHINE_ID => Get("MACHINE_ID");
        public static string ADMIN_SERVER_BASE_URL => Get("ADMIN_SERVER_BASE_URL");
        public static string lang => Get("lang");
        public static string engine => Get("engine");
        public static int PORT => int.Parse(Get("PORT"));
        public static string OPERATIONAL_MODE => Get("OPERATIONAL_MODE");
        public static int POLLING_FREQUENCY => int.Parse(Get("POLLING_FREQUENCY"));
        public static List<string> defaultExtensions =>
           config.GetSection("defaultExtensions").Get<List<string>>();

    }
}
