using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public class Eauth_token
    {
        public string Username { get; set; } = null;
        public string AccessToken { get; set; } = null;

        public string RefreshToken { get; set; } = null;
        public DateTime? RefreshTokenExpiration { get; set; } = null;
    }
}
