using Microsoft.Extensions.DependencyInjection;
using Weikio.ApiFramework.Abstractions.DependencyInjection;
using Weikio.ApiFramework.Plugins.Odbc.Configuration;
using Weikio.ApiFramework.SDK;

namespace Weikio.ApiFramework.Plugins.Odbc
{
    public static class ServiceExtensions
    {
        public static IApiFrameworkBuilder AddOdbc(this IApiFrameworkBuilder builder)
        {
            var assembly = typeof(OdbcOptions).Assembly;
            var apiPlugin = new ApiPlugin { Assembly = assembly };

            builder.Services.AddSingleton(typeof(ApiPlugin), apiPlugin);

            builder.Services.Configure<ApiPluginOptions>(options =>
            {
                if (options.ApiPluginAssemblies.Contains(assembly))
                {
                    return;
                }

                options.ApiPluginAssemblies.Add(assembly);
            });

            return builder;
        }

        public static IApiFrameworkBuilder AddOdbc(this IApiFrameworkBuilder builder, string endpoint, OdbcOptions configuration)
        {
            builder.AddOdbc();

            builder.Services.RegisterEndpoint(endpoint, "Weikio.ApiFramework.Plugins.Odbc", configuration);

            return builder;
        }
    }
}
