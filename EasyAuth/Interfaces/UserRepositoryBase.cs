using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using EasyAuth.Utils.PasswordUtil;
using System;

namespace EasyAuth.Interfaces
{
    public abstract class UserRepositoryBase
    {

        private readonly IConfiguration _configuration;
        private readonly AuthTokenizationService _tokenizationService;
        public UserRepositoryBase(IConfiguration configuration, AuthTokenizationService tokenizationService)
        {
            _configuration = configuration;
            _tokenizationService = tokenizationService;
        }
        public abstract Task SetRefreshTokenForUser(string name, string refreshToken, DateTime? modifiedRefreshTokenTime);
        public async Task RevokeRefreshTokenForUser(string name)
        {
            await SetRefreshTokenForUser(name, null, null);
        }
        //AuthRefreshTokenExpirationMinutes
        public async Task<TokenModel> AuthorizeWithPasswordGetToken(IAuthUser foundUser, List<string> userRoles, string inputPassword)
        {

            var identity = AuthTokenizationService.GetIdentity(foundUser.Username, foundUser.Email, foundUser.IsVerified, userRoles);

            if (!PasswordHasher.PasswordsMatch(foundUser.Password, foundUser.Username, inputPassword))
            {
                return null;
            }

            var accessToken = _tokenizationService.GenerateAccessToken(identity);

            //_configuration["AuthRefreshTokenExpirationMinutes"]
            var refreshToken = _tokenizationService.GenerateRefreshToken();

            var modifiedTime = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["AuthSettings:AuthRefreshTokenExpirationMinutes"]));
            await SetRefreshTokenForUser(
                            name: foundUser.Username,
                            refreshToken: refreshToken,
                            modifiedTime);

            return new TokenModel()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
    }
}
