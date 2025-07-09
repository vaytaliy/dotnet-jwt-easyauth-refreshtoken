using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public class JwtLibOptions
    {
        public string SecretAuthKey { get; set; }
        public string Salt { get; set; }
        public string EmailSecretAuthKey { get; set; }
        public string EmailPassword { get; set; }
        public string EmailSecretRecoveryKey { get; set; }
        //public string ValidateIssuer { get; set; }
        //public string ValidateAudience { get; set; }
        //public string ValidateLifetime { get; set; }
        //public string ValidateIssuerSigningKey { get; set; }
    }
    //public static class JwtLibOptions
    //{
    //    public static string SecretAuthKey { get; set; }
    //    public static string Salt { get; set; }
    //    public static string EmailSecretAuthKey { get; set; }
    //    public static string EmailPassword { get; set; }
    //    public static string ValidateIssuer { get; set; }
    //    public static string ValidateAudience { get; set; }
    //    public static string ValidateLifetime { get; set; }
    //    public static string ValidateIssuerSigningKey { get; set; }
    //}
}
