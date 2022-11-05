using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public interface ITokenizationService
    {
        string GenerateAccessToken(ClaimsIdentity identity);
        ClaimsPrincipal GetPrincipalCheckAccessTokenNoLifetime(string token);
        string GenerateRefreshToken();

    }
}
