using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Tweetbook.Extensions
{
	public static class GeneralExtension
	{
		public static string GetUserId(this HttpContext context)
		{
			if (context.User == null)
			{
				return string.Empty;
			}

			return context.User.Claims.Single(
				x => x.Type.Equals("id", System.StringComparison.InvariantCultureIgnoreCase)).Value;
		}
	}
}
