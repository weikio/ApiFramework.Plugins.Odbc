using Microsoft.Extensions.DependencyInjection;
using Weikio.ApiFramework.Abstractions.DependencyInjection;
using Weikio.ApiFramework.Plugins.Odbc.Configuration;
using Weikio.ApiFramework.SDK;

namespace Weikio.ApiFramework.Plugins.Odbc
{
    public static class ServiceExtensions
    {
        public static IApiFrameworkBuilder AddOdbc(this IApiFrameworkBuilder builder, string endpoint = null, OdbcOptions configuration = null)
        {
            builder.Services.AddOdbc(endpoint, configuration);

            return builder;
        }

        public static IServiceCollection AddOdbc(this IServiceCollection services, string endpoint = null, OdbcOptions configuration = null)
        {
            services.RegisterPlugin(endpoint, configuration);

            return services;
        }
    }
}
