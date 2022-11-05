using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public class JwtLibOptions
    {
        public string Authority { get; set; }
        public string Audience { get; set; }
        public string AuthKey { get; set; }
        public int AuthTokenExpirationMinutes { get; set; }
        public string Salt { get; set; }
        public string EmailVerificationKey { get; set; }
        public int EmailTokenExpirationMinutes { get; set; }
        public bool SendCookies { get; set; } = false;
        public bool SendTokensInResponseHeader { get; set; } = false;

    }
}
