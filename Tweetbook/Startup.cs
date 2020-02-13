using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tweetbook.Options;
using Tweetbook.Installers;

namespace Tweetbook
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.InstallServicesInAssembly(Configuration);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseHsts();
			}
			app.UseHttpsRedirection();
			app.UseStaticFiles();

			var swaggerOptions = new SwaggerOptions();
			Configuration.GetSection(nameof(SwaggerOptions)).Bind(swaggerOptions);

			app.UseRouting();

			app.UseAuthorization();
			app.UseAuthentication();

			app.UseSwagger(option =>
			{
				option.RouteTemplate = swaggerOptions.JsonRoute;
			});

			app.UseSwaggerUI(option =>
			{
				option.SwaggerEndpoint(swaggerOptions.UiEndpoint, swaggerOptions.Description);
			});

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");
				endpoints.MapRazorPages();
			});
		}
	}
}
