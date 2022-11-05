using System.Text.Json.Serialization;

namespace EasyAuth
{
    public class TokenModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
