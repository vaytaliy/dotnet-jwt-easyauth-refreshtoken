using EasyAuth;
using Microsoft.Extensions.Configuration;
using Recipes.Models;
using Recipes.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Recipes.Data
{
    public class UserData : IUserData
    {
        public string Jwta_token { get; set; }
    }
    public class MyJWTDataAccess : ISimpleAuthDataAccess
    {
        private readonly UserRepository _userRepository;

        public MyJWTDataAccess(UserRepository userRepository) //specify from where youll get this data or implement directly here
        {
            _userRepository = userRepository;
        }

        public async Task<Eauth_token> GetUser(string username)
        {
            //query data
            var user = await _userRepository.GetUserByName(username);
            if (user != null)
            {
                return new Eauth_token()
                {
                    Username = user.Username,
                    RefreshToken = user.RefreshToken,
                    RefreshTokenExpiration = user.RefreshTokenExpiration
                };
            }
            return default;
            //etc
        }

        public async Task PersistNewRefreshToken(string username, string refreshToken)
        {
            await _userRepository.SetRefreshTokenForUser(username, refreshToken);
        }
        //todo:
        //return expiration time also
    }
}
