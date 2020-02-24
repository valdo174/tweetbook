using FluentAssertions;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Tweetbook.Contracts.V1;
using Tweetbook.Domain;
using Xunit;

namespace Tweetbook.IntegrationTests
{
	public class PostsIntegrationTest : IntegrationTest
	{
		public PostsIntegrationTest() : base()
		{

		}

		[Fact]
		public async Task GetAll_WithoutAnyPosts_ReturnEmptyResponse()
		{
			// Arrange
			await AuthenticateAsync();

			// Act
			var response = await client.GetAsync(ApiRoutes.Posts.GetAll);

			// Assert
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			(await response.Content.ReadAsAsync<List<Post>>()).Should().BeEmpty();
		}

		[Fact]
		public async Task Get_ReturnsPost_WhenPostExistsInTheDataBase()
		{
			// Arrange
			var postName = "Test Post";

			await AuthenticateAsync();
			var createdPost = await CreatePostASync(postName);

			// Act
			var response = await client.GetAsync(ApiRoutes.Posts.Get.Replace("{postId}",
												createdPost.Id.ToString()));

			// Assert
			response.StatusCode.Should().Be(HttpStatusCode.OK);

			var returnedPost = await response.Content.ReadAsAsync<Post>();
			returnedPost.Id.Should().Be(createdPost.Id);
			returnedPost.Name.Should().Be(postName);
		}
	}
}
