using System;
using System.Collections.Generic;
using Tweetbook.Domain;

namespace Tweetbook.Services
{
	public interface IPostService
	{
		List<Post> GetAllPosts();

		Post GetPostById(Guid Id);

		bool UpdatePost(Post postToUpdate);
	}
}
