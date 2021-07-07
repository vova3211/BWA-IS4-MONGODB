using IdentityServer4.Validation;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using WebApplication1.Server.IdentityServer.Microsoft;

namespace WebApplication1.Server.IdentityServer
{
    public static class IdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddOwnApiAuthorization<TUser>(
            this IIdentityServerBuilder builder) where TUser : class
        {
            builder.AddOwnApiAuthorization<TUser>(o => { });
            return builder;
        }

        public static IIdentityServerBuilder AddOwnApiAuthorization<TUser>(
            this IIdentityServerBuilder builder,
            Action<ApiAuthorizationOptions> configure)
                where TUser : class
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddAspNetIdentity<TUser>()
                .ConfigureReplacedServices()
                .AddIdentityResources()
                .AddApiResources()
                .AddClients()
                .AddSigningCredentials();

            builder.Services.Configure(configure);

            return builder;
        }

        public static IIdentityServerBuilder ConfigureReplacedServices(this IIdentityServerBuilder builder)
        {
            builder.Services.TryAddSingleton<IAbsoluteUrlFactory, AbsoluteUrlFactory>();
            builder.Services.AddSingleton<IRedirectUriValidator, RelativeRedirectUriValidator>();
            builder.Services.AddSingleton<IClientRequestParametersProvider, CustomClientRequestParametersProvider>();
            ReplaceEndSessionEndpoint(builder);

            return builder;
        }

        private static void ReplaceEndSessionEndpoint(IIdentityServerBuilder builder)
        {
            // We don't have a better way to replace the end session endpoint as far as we know other than looking the descriptor up
            // on the container and replacing the instance. This is due to the fact that we chain on AddIdentityServer which configures the
            // list of endpoints by default.
            var endSessionEndpointDescriptor = builder.Services
                            .Single(s => s.ImplementationInstance is IdentityServer4.Hosting.Endpoint e &&
                                    string.Equals(e.Name, "Endsession", StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals("/connect/endsession", e.Path, StringComparison.OrdinalIgnoreCase));

            builder.Services.Remove(endSessionEndpointDescriptor);
            builder.AddEndpoint<AutoRedirectEndSessionEndpoint>("EndSession", "/connect/endsession");
        }
    }
}
