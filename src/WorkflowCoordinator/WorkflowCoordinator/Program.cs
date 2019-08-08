﻿using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using WorkflowCoordinator.Config;

namespace WorkflowCoordinator
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
            .UseEnvironment(Environment.GetEnvironmentVariable("ENVIRONMENT"))
            .ConfigureWebJobs(b =>
            {
                b.AddAzureStorageCoreServices();
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var keyVaultAddress = Environment.GetEnvironmentVariable("KEY_VAULT_ADDRESS");

                var tokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager()).Build();
            })
            .ConfigureLogging((hostingContext, b) =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureServices((hostingContext, services) =>
            {
                services.AddOptions<ExampleConfig>()
                    .Bind(hostingContext.Configuration.GetSection("ExampleConfig"));
                //.PostConfigure(o =>
                //{
                //    var errors = string.Join(",", o.ValidateDataAnnotations().Concat(o.Validate()));
                //    if (errors.Any())
                //    {
                //        var message = $"Found configuration error(s) in {o.GetType().Name}: {errors}";
                //        _logger.LogError(message);
                //        throw new ApplicationException(message);
                //    }
                //});

                services.AddOptions<SecretsConfig>()
                    .Bind(hostingContext.Configuration.GetSection("NsbDbSection"));

                    // if local
                    services.AddOptions<UrlsConfig>()
                        .Configure(o => o.BaseUrl = "http://localhost:27720/");
                // else
                //services.AddOptions<UrlsConfig>()
                //    .Configure(o => o.BaseUrl = "http://localhost:27720/");

                
                services.AddHttpClient<IDataServiceApiClient, DataServiceApiClient>()
                    .SetHandlerLifetime(TimeSpan.FromMinutes(5)); 

                services.AddScoped<IJobHost, NServiceBusJobHost>();
            })
            .UseConsoleLifetime();

            var host = builder.Build();

            using (host)
            {
                await host.RunAsync();
            }
        }
    }

    public class UrlsConfig
    {
        public string  BaseUrl { get; set; }
    }
}