using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Graph.Models.ExternalConnectors;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using NOA.Common.Service.Model;
using NOA.Common.Constants;
using NOA.Common.Service;

namespace NOA.Common.DI.Backend
{
    public class DoStartup
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly IConfiguration _configuration;

        public DoStartup(IConfiguration configuration, IServiceCollection serviceCollection)
        {
            this._serviceCollection = serviceCollection;
            this._configuration = configuration;
            ConfigureServiceCollection();
        }

        public DoStartup(IServiceCollection serviceCollection)
        {
            this._serviceCollection = serviceCollection;
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            ConfigureServiceCollection();
        }


        public void ConfigureServiceCollection()
        {
            var downstreamApi = _configuration.GetSection("DownstreamApi");

            _serviceCollection
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(_configuration)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddMicrosoftGraph(downstreamApi)
                .AddInMemoryTokenCaches();

            var allowedTenants = _configuration.GetSection("AzureAd:AllowedTenants").Get<string[]>();

            _serviceCollection.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var existingOnTokenValidatedHandler = options.Events.OnTokenValidated;

                    options.Events.OnTokenValidated = async context =>
                    {
                        await existingOnTokenValidatedHandler(context);
                        string tenantId = context?.Principal?.GetTenantId() ?? String.Empty;
                        if (!allowedTenants?.Contains(tenantId) ?? true)
                        {
                            throw new UnauthorizedAccessException($"Application from tenant '{tenantId}' are not authorized to call this Web API");
                        }
                    };
                });


            _serviceCollection
                .AddAuthorization();

            _serviceCollection
                .AddControllers();

            _serviceCollection
                .AddHttpContextAccessor();

            _serviceCollection
                .AddRazorPages();

            _serviceCollection
                .AddServerSideBlazor()
                .AddMicrosoftIdentityConsentHandler();

            var adOptionsSection = _configuration.GetSection(ConfigConstants.AzureAdOptions);
            var usersConnectionModelSection = _configuration.GetSection(ConfigConstants.UsersConnectionModel);
            var aiOptions = new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions();

            _serviceCollection
                .Configure<UsersConnectionModel>(opt => usersConnectionModelSection.Bind(opt))
                .Configure<AzureAdOptions>(opt => adOptionsSection.Bind(opt))                            
                .AddApplicationInsightsTelemetry(aiOptions)
                .AddLogging(builder =>
                {
                    // Only Application Insights is registered as a logger provider
                    builder.AddApplicationInsights(
                        configureTelemetryConfiguration: (config) => config.ConnectionString = (_configuration["ApplicationInsights:ConnectionString"] as string),
                        configureApplicationInsightsLoggerOptions: (options) => { }
                    );
                })
                .AddTransient<IGraphServiceProxy, GraphServiceProxy>()
                .AddTransient<Service.IAuthenticationService, Service.AuthenticationService>();
        }

        public ServiceProvider GetServiceProvider()
        {
            return _serviceCollection.BuildServiceProvider();
        }
    }
}