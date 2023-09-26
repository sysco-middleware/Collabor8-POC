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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.UI;


namespace NOA.Common.DI.DIFrontend
{
    public class DoFrontendStartup
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly IConfiguration _configuration;

        public DoFrontendStartup(IConfiguration configuration, IServiceCollection serviceCollection)
        {
            this._serviceCollection = serviceCollection;
            this._configuration = configuration;
            ConfigureServiceCollection();
        }

        public DoFrontendStartup(IServiceCollection serviceCollection)
        {
            this._serviceCollection = serviceCollection;
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            ConfigureServiceCollection();
        }


        public void ConfigureServiceCollection()
        {
            _serviceCollection.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                // Handling SameSite cookie according to https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1
                options.HandleSameSiteCookieCompatibility();
            });

            //Add authentication with the Microsoft identity platform.
            _serviceCollection.AddMicrosoftIdentityWebAppAuthentication(_configuration)
                    .EnableTokenAcquisitionToCallDownstreamApi(new string[] { _configuration["Users:NorskOffshoreAuthenticateServiceScope"] })
                    .AddInMemoryTokenCaches();

            //Enables to add client service to use the HttpClient by dependency injection.
            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            _serviceCollection.AddHttpClient<IUserService, UserService>();

            _serviceCollection.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddMicrosoftIdentityUI();

            _serviceCollection.AddRazorPages();


            var customLogOptions = _configuration.GetSection(ConfigConstants.CustomLogOptions);
            var aiOptions = new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions();
            _serviceCollection
                .AddApplicationInsightsTelemetry(aiOptions)
                .AddLogging(builder =>
                {
                    // Only Application Insights is registered as a logger provider
                    builder.AddApplicationInsights(
                        configureTelemetryConfiguration: (config) => config.ConnectionString = (_configuration["ApplicationInsights:ConnectionString"] as string),
                        configureApplicationInsightsLoggerOptions: (options) => { }
                    );
                })
                .Configure<CustomLogOptions>(opt => customLogOptions.Bind(opt));

        }

        public ServiceProvider GetServiceProvider()
        {
            return _serviceCollection.BuildServiceProvider();
        }
    }
}