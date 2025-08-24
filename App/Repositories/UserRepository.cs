using Dapper;
using EasyAuth;
using EasyAuth.Utils.PasswordUtil;
using Microsoft.Extensions.Configuration;
using App.Data;
using App.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections;
using EasyAuth.Interfaces;

namespace App.Repositories
{
    public class UserRepository: UserRepositoryBase, IAuthUserRepository
    {
        private readonly DbConnectionContext _db;
        public UserRepository(DbConnectionContext db, IConfiguration configuration, AuthTokenizationService tokenizationService) 
            : base(configuration, tokenizationService)
        {
            _db = db;
        }

        public async Task<bool> CreateUser(IAuthUser user)
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

            using var connection = _db.CreateConnection();

            await connection.ExecuteAsync(insertNewUserSql,
                new
                {
                    user.Username,
                    user.Password,
                    user.Email,
                    IsVerified = false
                });

            return true;
        }

        public async Task<bool> SetUserEmailVerified(string username, string email)
        {
            string updateSql =
$@"UPDATE Users
SET IsVerified = 1
WHERE Email = @Email AND Username = @Username";
            
            using var connection = _db.CreateConnection();

            await connection.ExecuteAsync(updateSql,
                new 
                {  
                    Email = email,
                    Username = username
                }
            );

            return true;
        }

        public async Task<bool> SetUserPassword(string username, string password)
        {
            //It's a good practice to invalidate refresh token on password change
            string updateSql =
$@"UPDATE Users
SET 
    Password = @Password,
    RefreshToken = NULL,
    RefreshTokenExpirationUTC = NULL,
    SecurityStamp = @SecurityStamp
WHERE Username = @Username";

            using var connection = _db.CreateConnection();

            await connection.ExecuteAsync(updateSql,
                new
                {
                    Password = password,
                    Username = username,
                    SecurityStamp = DateTime.UtcNow //Super important for security reasons
                }
            );

            return true;
        }

        public async Task<IAuthUser> GetUserByName(string name)
        {

            var command = (
$@"
SELECT
*
FROM Users WHERE Username = @Username");

            using (var ctx = _db.CreateConnection())
            {
                var user = await ctx.QueryFirstOrDefaultAsync<User>(command, new {Username = name });
                return user;
            }
        }

        public async Task<IEnumerable<IAuthUser>> GetUsersByEmail(string email)
        {

            var command = (
$@"
SELECT
*
FROM Users WHERE Email = @Email");

            using (var ctx = _db.CreateConnection())
            {
                var user = await ctx.QueryAsync<User>(command, new { Email = email });
                return user;
            }
        }

        public async Task<IAuthUser> GetUserById(long id)
        {
            string findUserByNameSQL = $@"
SELECT
*
FROM Users WHERE Id = @Id";

            using (var connection = _db.CreateConnection())
            {
                var foundUser = await connection.QueryFirstOrDefaultAsync<User>(findUserByNameSQL, new { Id = id });
                return foundUser;
            }
        }

        public override async Task SetRefreshTokenForUser(string name, string refreshToken, DateTime? modifiedRefreshTokenTime)
        {
            string setRefreshTokenSql = $@"
UPDATE Users SET RefreshToken = @RefreshToken,
RefreshTokenExpirationUTC = @RefreshTokenExpirationUTC 
WHERE Username = @Username;";

            using var connection = _db.CreateConnection();
            await connection.ExecuteAsync(setRefreshTokenSql,
                new
                {
                    Username = name,
                    RefreshToken = refreshToken,
                    RefreshTokenExpirationUTC = modifiedRefreshTokenTime
                });
        }

        public async Task<List<string>> GetUserRoles(string username)
        {
            string sql = $@"
SELECT r.RoleName FROM Users u
INNER JOIN UsersAndRoles ur ON u.Username = @Username AND u.Id = ur.UserId
INNER JOIN Roles r ON ur.RoleName = r.RoleName
";
            using var connection = _db.CreateConnection();
            var roles = await connection.QueryAsync<string>(sql,
                new
                {
                    Username = username
                });
            
            return roles.ToList();
        }
    }
}
