using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth.Interfaces
{
    public interface IAuthUserRepository
    {
        Task<bool> CreateUser(IAuthUser user);
        Task<bool> SetUserEmailVerified(string username, string email);
        Task<bool> SetUserPassword(string username, string password);
        Task<IAuthUser> GetUserByName(string name);
        Task<IAuthUser> GetUserById(long id);
        Task<IEnumerable<IAuthUser>> GetUsersByEmail(string email);
        Task SetRefreshTokenForUser(string name, string refreshToken, DateTime? modifiedRefreshTokenTime);
        Task<List<string>> GetUserRoles(string username);
        Task<TokenModel> AuthorizeWithPasswordGetToken(IAuthUser foundUser, List<string> userRoles, string inputPassword);
        Task RevokeRefreshTokenForUser(string name);
    }
}
