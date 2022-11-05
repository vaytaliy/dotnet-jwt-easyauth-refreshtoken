using Dapper;
using EasyAuth;
using EasyAuth.Utils.PasswordUtil;
using Microsoft.Extensions.Configuration;
using Recipes.Data;
using Recipes.Models;
using Recipes.SQL;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Recipes.Repositories
{
    public class UserRepository
    {
        private readonly RecipesContext _db;
        private readonly IConfiguration _configuration;
        private readonly AuthTokenizationService _tokenizationService;
        public UserRepository(RecipesContext db, IConfiguration configuration, AuthTokenizationService tokenizationService)
        {
            _db = db;
            _configuration = configuration;
            _tokenizationService = tokenizationService;
        }

        //public ClaimsIdentity GetIdentity(User user) //used for jwt gen
        //{

        //    var claims = new List<Claim>
        //        {
        //            new Claim(ClaimTypes.Name, user.Username),
        //            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        //        };

        //    ClaimsIdentity claimsIdentity = new(
        //        claims: claims,
        //        authenticationType: "Login",
        //        nameType: ClaimTypes.Name,
        //        null
        //    );
        //    return claimsIdentity;
        //}

        public async Task<bool> CreateUser(User user)
        {
            var existingUser = await GetUserByName(user.Username);
            if (existingUser != null)
            {
                return false;
            }

            string insertNewUserSql = 
$@"INSERT INTO Users
(Username, Password, Email, IsVerified)
VALUES (@Username, @Password, @Email, @IsVerified)";

            using (var connection = _db.CreateConnection())
            {

                await connection.ExecuteAsync(insertNewUserSql,
                    new
                    {
                        Username = user.Username,
                        Password = user.Password,
                        Email = user.Email,
                        IsVerified = false
                    });

                return true;
            }
        }

        public async Task<User> GetUserByName(string name)
        {

            var command = (
$@"
SELECT
*
FROM {Tables.GetTableName(Tables.TableNames.Users)} WHERE Username = @Username");

            using (var ctx = _db.CreateConnection())
            {
                var user = await ctx.QueryFirstOrDefaultAsync<User>(command, new {Username = name });
                return user;
            }
        }

        public async Task<User> GetUserById(long id)
        {
            string findUserByNameSQL = $@"
SELECT
Id,
Username,
Password,
Email,
IsVerified,
RefreshToken,
RefreshTokenExpiration
FROM Users WHERE Id = @Id";

            using (var connection = _db.CreateConnection())
            {
                var foundUser = await connection.QueryFirstOrDefaultAsync<User>(findUserByNameSQL, new { Id = id });
                return foundUser;
            }
        }

        public async Task SetRefreshTokenForUser(string name, string refreshToken)
        {
            string setRefreshTokenSql = $@"
UPDATE Users SET RefreshToken = @RefreshToken,
RefreshTokenExpiration = @RefreshTokenExpiration 
WHERE Username = @Username;";
            var timeNow = DateTime.UtcNow;
            var modifiedTime = timeNow.AddMinutes(int.Parse(_configuration["AuthSettings:AuthRefreshTokenExpirationMinutes"]));

            using (var connection = _db.CreateConnection())
            {
                await connection.ExecuteAsync(setRefreshTokenSql,
                    new
                    {
                        Username = name,
                        RefreshToken = refreshToken,
                        RefreshTokenExpiration = modifiedTime
                    });
            }
        }

        public async Task RevokeRefreshTokenForUser(string name)
        {
            await SetRefreshTokenForUser(name, null);
        }

        public async Task<TokenModel> AuthorizeWithPasswordGetToken(User foundUser, string inputPassword)
        {
            var identity = AuthTokenizationService.GetIdentity(foundUser.Username);

            if (!PasswordHasher.PasswordsMatch(foundUser.Password, foundUser.Username, inputPassword))
            {
                return null;
            }

            var accessToken = _tokenizationService.GenerateAccessToken(identity);
            var refreshToken = _tokenizationService.GenerateRefreshToken();

            await SetRefreshTokenForUser(
                name: foundUser.Username,
                refreshToken: refreshToken);

            return new TokenModel() { 
                 AccessToken = accessToken,
                 RefreshToken = refreshToken
            };
        }
    }
}
