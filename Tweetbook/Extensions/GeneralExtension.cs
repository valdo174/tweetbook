using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

			return context.User.Claims.SingleOrDefault(x => x.Type == "Id")?.Value;
		}
	}
}
