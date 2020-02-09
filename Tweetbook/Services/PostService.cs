using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
	}
}
