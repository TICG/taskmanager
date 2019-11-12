using System;
using System.Net.Http;
using Common.Helpers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;


namespace Portal
{
    public class Program
    {
        private static GraphServiceClient _graphServiceClient;
        private static HttpClient _httpClient;

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                //.UseUrls("https://localhost:44363")
                .UseStartup<Startup>()
                .ConfigureAppConfiguration(builder =>
                {
                    var azureAppConfConnectionString = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING");

                    var (keyVaultAddress, keyVaultClient) = SecretsHelpers.SetUpKeyVaultClient();

                    builder.AddAzureAppConfiguration(new AzureAppConfigurationOptions()
                    {
                        ConnectionString = azureAppConfConnectionString
                    }).AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager()).Build();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("logging:portal"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddAzureWebAppDiagnostics();
                });
    }
}
