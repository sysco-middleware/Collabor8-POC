using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NOA.Common.DI.Backend;
using Xunit.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting;

namespace AuthenticateService_IntegrationTests
{
    public class Startup 
    {
        internal static DoStartup StartupContainer;

        public void ConfigureServices(IServiceCollection services)
        {
            StartupContainer = new DoStartup(services);
        }
    }
}
