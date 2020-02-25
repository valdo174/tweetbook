﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Tweetbook.Data;
using Tweetbook.Domain;
using Tweetbook.Options;

namespace Tweetbook.Services
{
	public class IdentityService : IIdentityService
	{
		private readonly UserManager<IdentityUser> _userManager;

		private readonly JwtSettings _jwtSettings;

		private readonly TokenValidationParameters _tokenValidationParameters;

		private readonly DataContext _dataContext;

		public IdentityService(UserManager<IdentityUser> userManager, JwtSettings jwtSettings,
			TokenValidationParameters tokenValidationParameters, 
			DataContext dataContext)
		{
			_userManager = userManager;
			_jwtSettings = jwtSettings;
			_tokenValidationParameters = tokenValidationParameters;
			_dataContext = dataContext;
		}

		public async Task<AuthenticationResult> LoginAsync(string email, string password)
		{
			var user = await _userManager.FindByEmailAsync(email);

			if (user == null)
			{
				return new AuthenticationResult
				{
					Errors = new[] { "User does not exist." },
				};
			}

			var userHasValidPassword = await _userManager.CheckPasswordAsync(user, password);

			if (!userHasValidPassword)
			{
				return new AuthenticationResult
				{
					Errors = new[] { "User/password combination is wrong." },
				};
			}

			return await GenerateAuthenticationResultForUserAsync(user);
		}

		public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken)
		{
			var validatedToken = GerPrincipalFromToken(token);

			if (validatedToken == null)
			{
				return new AuthenticationResult { Errors = new[] { "Invalid token" } };
			}

			var expiryDateUnix = 
				long.Parse(validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

			var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
				.AddSeconds(expiryDateUnix);

			if (expiryDateTimeUtc > DateTime.UtcNow)
			{
				return new AuthenticationResult { Errors = new[] { "This token hasn't expired yet" } };
			}

			var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

			var storedRefreshToken = await _dataContext.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshToken);

			if (storedRefreshToken == null)
			{
				return new AuthenticationResult { Errors = new[] { "Refresh token doesn't exists" } };
			}

			if (DateTime.UtcNow > storedRefreshToken.ExpiredDate)
			{
				return new AuthenticationResult { Errors = new[] { "Refresh token has expired" } };
			}

			if (storedRefreshToken.Invalidated)
			{
				return new AuthenticationResult { Errors = new[] { "Refresh token has been invalidated" } };
			}

			if (storedRefreshToken.Used)
			{
				return new AuthenticationResult { Errors = new[] { "Refresh token has been used" } };
			}

			if (storedRefreshToken.JwtId != jti)
			{
				return new AuthenticationResult { Errors = new[] { "Refresh token doesn't match this JWT" } } ;
			}

			storedRefreshToken.Used = true;
			_dataContext.RefreshTokens.Update(storedRefreshToken);

			await _dataContext.SaveChangesAsync();

			var user = await _userManager.FindByIdAsync(validatedToken.Claims.Single(x => x.Type.Equals("id", StringComparison.InvariantCultureIgnoreCase)).Value);
			return await GenerateAuthenticationResultForUserAsync(user);
		}

		private ClaimsPrincipal GerPrincipalFromToken(string token)
		{
			var tokenHandler = new JwtSecurityTokenHandler();

			try
			{
				var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
				if(!IsJwtWithValidSecurityAlgorithm(validatedToken))
				{
					return null;
				}

				return principal;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
		{
			return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
				jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, 
													StringComparison.InvariantCultureIgnoreCase);
		}
			
		public async Task<AuthenticationResult> RegisterAsync(string email, string password)
		{
			var existingUser = await _userManager.FindByEmailAsync(email);

			if (existingUser != null)
			{
				return new AuthenticationResult
				{
					Errors = new[] { "User with email address already exists." },
				};
			}

			var newUser = new IdentityUser
			{
				Email = email,
				UserName = email
			};

			var createdUser = await _userManager.CreateAsync(newUser, password);

			if (!createdUser.Succeeded)
			{
				return new AuthenticationResult
				{
					Errors = createdUser.Errors.Select(x => x.Description),
				};
			}

			return await GenerateAuthenticationResultForUserAsync(newUser);
		}

		private async Task<AuthenticationResult> GenerateAuthenticationResultForUserAsync(IdentityUser user)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
				{
					new Claim(JwtRegisteredClaimNames.Sub, user.Email),
					new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
					new Claim(JwtRegisteredClaimNames.Email, user.Email),
					new Claim("id", user.Id)
				}),
				Expires = DateTime.UtcNow.Add(_jwtSettings.TokenLifetime),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
															SecurityAlgorithms.HmacSha256Signature)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);

			var refreshToken = new RefreshToken
			{
				JwtId = token.Id,
				UserId = user.Id,
				CreationDate = DateTime.UtcNow,
				ExpiredDate = DateTime.UtcNow.AddMonths(6),
			};

			await _dataContext.RefreshTokens.AddAsync(refreshToken);
			await _dataContext.SaveChangesAsync();

			return new AuthenticationResult
			{
				Success = true,
				Token = tokenHandler.WriteToken(token),
				RefreshToken = refreshToken.Token
			};
		}
	}
}
