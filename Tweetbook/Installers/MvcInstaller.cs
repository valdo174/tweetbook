using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using System.Collections.Generic;
using System.Text;
using Tweetbook.Options;
using Tweetbook.Services;

namespace Tweetbook.Installers
{
	public class MvcInstaller : IInstaller
	{
		public void InstallServices(IServiceCollection services, IConfiguration configuration)
		{
			var jwtSetttings = new JwtSettings();
			configuration.GetSection("JwtSettings").Bind(jwtSetttings);
			services.AddSingleton(jwtSetttings);

			services.AddAuthentication(x =>
			{
				x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
				x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(x =>
			{
				x.SaveToken = true;
				x.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSetttings.Secret)),
					ValidateIssuer = false,
					ValidateAudience = false,
					RequireExpirationTime = false,
					ValidateLifetime = true
				};
			});

			services.AddScoped<IIdentityService, IdentityService>();

			services.AddSwaggerGen(x =>
			{
				x.SwaggerDoc("v1", new OpenApiInfo { Title = "Tweetbook API", Version = "v1" });

				x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Description = "JWT Authorization header using the bearer scheme",
					Name = "Authorixation",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.ApiKey
				});

				x.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{ new OpenApiSecurityScheme { Name = "Bearer" }, new string[0] }
				});
			});
		}
	}
}
