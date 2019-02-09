// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectSmartLink.Web.Extensions
{
	public static class AzureAdAuthenticationBuilderExtensions
	{
		public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder)
		   => builder.AddAzureAdBearer(_ => { });

		public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder, Action<AzureAdOptions> configureOptions)
		{
			builder.Services.Configure(configureOptions);
			builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureAzureAdBearerOptions>();
			builder.AddJwtBearer();
			return builder;
		}

		private class ConfigureAzureAdBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
		{
			private readonly AzureAdOptions _azureOptions;

			public ConfigureAzureAdBearerOptions(IOptions<AzureAdOptions> azureOptions)
			{
				_azureOptions = azureOptions.Value;
			}

			public void Configure(string name, JwtBearerOptions options)
			{
				options.Audience = _azureOptions.ClientId;
				options.Authority = $"{_azureOptions.Instance}{_azureOptions.TenantId}";
				
				options.TokenValidationParameters = new TokenValidationParameters
				{
                    ValidateAudience = false,
					ValidateIssuer = false,
					SaveSigninToken = true
				};

				options.Events = new JwtBearerEvents
				{
					OnTokenValidated = TokenValidated,
					OnAuthenticationFailed = AuthenticationFailed
				};

				options.Validate();
			}

			public void Configure(JwtBearerOptions options)
			{
				Configure(Options.DefaultName, options);
			}

			// TokenValidated event
			private Task TokenValidated(Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext context)
			{
                // Check if tenant is allowed
                string tenantID = context.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

                if(!_azureOptions.TenantId.Equals(tenantID, StringComparison.InvariantCultureIgnoreCase) && !_azureOptions.AllowedTenants.Any(x => x.Equals(tenantID, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return Task.FromException(new SecurityTokenException("The tenant is not allowed."));
                }

                return Task.FromResult(0);
			}

			// Handle sign-in errors differently than generic errors.
			private Task AuthenticationFailed(Microsoft.AspNetCore.Authentication.JwtBearer.AuthenticationFailedContext context)
			{
                context.Fail(context.Exception);
				return Task.FromResult(0);
			}
		}
	}
}