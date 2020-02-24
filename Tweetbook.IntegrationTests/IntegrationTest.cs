using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Tweetbook.Contracts.V1;
using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Data;

namespace Tweetbook.IntegrationTests
{
	public class IntegrationTest : IDisposable
	{
		private readonly IServiceProvider _serviceProvider;

		protected readonly HttpClient client;

		protected IntegrationTest()
		{
			var appFactory = new WebApplicationFactory<Startup>()

			   .WithWebHostBuilder(builder =>
			   {
				   builder.ConfigureServices(services =>
				   {
					   var descriptor = services.SingleOrDefault(
										d => d.ServiceType == typeof(DbContextOptions<DataContext>));
					   services.Remove(descriptor);

					   services.AddEntityFrameworkInMemoryDatabase();
					   services.AddDbContext<DataContext>(options =>
					   {
						   options.UseInMemoryDatabase("TestDb");
					   });

					   var sp = services.BuildServiceProvider();
					   using (var scope = sp.CreateScope())
					   {
						   var scopedServices = scope.ServiceProvider;
						   var db = scopedServices.GetRequiredService<DataContext>();

						   db.Database.EnsureDeleted();
						   db.Database.EnsureCreated();
					   }
				   });
			   });

			_serviceProvider = appFactory.Services;
			client = appFactory.CreateClient();
		}

		protected async Task AuthenticateAsync()
		{
			client.DefaultRequestHeaders.Authorization = 
				new AuthenticationHeaderValue("bearer", await GetJwtAsync());
		}

		protected async Task<PostResponse> CreatePostASync(string name)
		{
			var response = await client.PostAsJsonAsync(ApiRoutes.Posts.Create,
														new CreatePostRequest { Name = name });

			return await response.Content.ReadAsAsync<PostResponse>();
		}

		private async Task<string> GetJwtAsync()
		{
			var response = await client.PostAsJsonAsync(ApiRoutes.Identity.Register, new UserRegistrationRequest
			{
				Email = "test@integration.com",
				Password = "StrongPassword123!"
			});

			var registrationResponse = await response.Content.ReadAsAsync<AuthSuccessResponse>();
			return registrationResponse.Token;
		}

		public void Dispose()
		{
			using var serviceScope = _serviceProvider.CreateScope();
			var dbContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

			dbContext.Database.EnsureDeleted();
		}
	}
}
