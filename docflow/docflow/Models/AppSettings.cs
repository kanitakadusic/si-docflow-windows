using Microsoft.Extensions.Configuration;
using System.IO;

namespace docflow
{
    public static class AppSettings
    {
        private static IConfigurationRoot config;

        static AppSettings()
        {
            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
        }

        public static string Get(string key) => config[key];

        public static string ADMIN_SERVER_BASE_URL => Get("ADMIN_SERVER_BASE_URL");
        public static string PROCESSING_SERVER_BASE_URL => Get("PROCESSING_SERVER_BASE_URL");
        public static int PORT => int.Parse(Get("PORT"));
        public static string MACHINE_ID => Get("MACHINE_ID");
        public static string OPERATIONAL_MODE => Get("OPERATIONAL_MODE");
        public static int POLLING_FREQUENCY => int.Parse(Get("POLLING_FREQUENCY"));
        public static string OCR_LANGUAGE => Get("OCR_LANGUAGE");
        public static string OCR_ENGINE => Get("OCR_ENGINE");
    }
}
