using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public interface ISimpleAuthDataAccess
    {
        public Task<Eauth_token> GetUser(string username);

        public Task PersistNewRefreshToken(string username, string refreshToken);
        //IUserdata has username, refreshtoken, refreshtoken expiry
    }
}
