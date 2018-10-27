// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreation.Core.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Threading.Tasks;

namespace ProposalCreation.Core.Extensions
{
	public static class AzureAdAuthenticationBuilderExtensions
	{
		public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder)
		   => builder.AddAzureAdBearer(_ => { });

		public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder, Action<AzureAdConfiguration> configureOptions)
		{
			builder.Services.Configure(configureOptions);
			builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureAzureAdBearerOptions>();
			builder.AddJwtBearer();
			return builder;
		}

		private class ConfigureAzureAdBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
		{
			private readonly AzureAdConfiguration _azureOptions;

			public ConfigureAzureAdBearerOptions(IOptions<AzureAdConfiguration> azureOptions) => _azureOptions = azureOptions.Value;

			public void Configure(string name, JwtBearerOptions options)
			{
				options.Audience = _azureOptions.ClientId;
				options.Authority = $"{_azureOptions.Instance}{_azureOptions.TenantId}";

				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = false,
					SaveSigninToken = true
				};

				options.Events = new JwtBearerEvents
				{
					OnTokenValidated = TokenValidatedAsync,
					OnAuthenticationFailed = AuthenticationFailedAsync
				};

				options.Validate();
			}

			public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);

			// TokenValidated event
			private Task TokenValidatedAsync(TokenValidatedContext context) =>
				// Store the token in the token cache
				Task.FromResult(0);

			// Handle sign-in errors differently than generic errors.
			private Task AuthenticationFailedAsync(AuthenticationFailedContext context) => Task.FromResult(0);
		}
	}
}