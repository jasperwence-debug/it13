using Microsoft.Extensions.Configuration;

namespace CRM.winforms
{
    public static class AppConfig
    {
        public static IConfigurationRoot Configuration { get; }

        static AppConfig()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string ApiBaseUrl =>
            Configuration["Api:BaseUrl"] ?? "https://localhost:7017";
    }
}