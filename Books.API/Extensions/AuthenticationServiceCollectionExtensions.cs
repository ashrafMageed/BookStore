using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication.Extensions
{
    public static class AuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddWebApplicationAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(o => 
            {
                o.ClientId = "c2e9a664-3bd1-41d4-9e20-bdedd30a7966";
                o.CallbackPath = "/signin-oidc";
                o.Authority = $"https://login.microsoftonline.com/tfp/BookStoreAD.onmicrosoft.com/B2C_1_DefaultSignInUpPolicy/v2.0";
                o.UseTokenLifetime = true;

            });
            
            return services;
        }
    }
}
