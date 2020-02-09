using System;
using System.Collections.Generic;
using System.Linq;
using Tweetbook.Domain;

namespace Tweetbook.Services
{
	public class PostService : IPostService
	{
		private List<Post> _posts;

		public PostService()
		{
			_posts = new List<Post>();

			for (var i = 0; i < 5; i++)
			{
				_posts.Add(new Post
				{
					Id = Guid.NewGuid(),
					Name = $"Post Name {i}"
				});
			}
		}

		public Post GetPostById(Guid Id)
		{
			return _posts.SingleOrDefault(x => x.Id == Id);
		}

		public List<Post> GetAllPosts()
		{
			return _posts;
		}

		public bool UpdatePost(Post postToUpdate)
		{
			var exists = GetPostById(postToUpdate.Id) != null;

			if (!exists)
				return false;

			var index = _posts.FindIndex(x => x.Id == postToUpdate.Id);
			_posts[index] = postToUpdate;

			return true;
		}

		public bool DeletePost(Guid postId)
		{
			var post = GetPostById(postId);

			if (post == null)
				return false;

			_posts.Remove(post);

			return true;
		}
	}
}
