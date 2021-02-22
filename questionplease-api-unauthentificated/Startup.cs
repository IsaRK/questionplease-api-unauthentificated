using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using questionplease_api_unauthentificated;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace questionplease_api_unauthentificated
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddFilter(level => true);
            });

            builder.Services.AddSingleton((s) =>
            {
                CosmosClientBuilder cosmosClientBuilder =
                new CosmosClientBuilder(System.Environment.GetEnvironmentVariable(Constants.CONNECTION_STRING, EnvironmentVariableTarget.Process));

                return cosmosClientBuilder.WithConnectionModeDirect()
                    .WithApplicationRegion("North Europe")
                    .WithBulkExecution(true)
                    .Build();
            });
        }
    }
}
