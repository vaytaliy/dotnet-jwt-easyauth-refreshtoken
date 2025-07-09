using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth.Interfaces
{
    public interface IAuthUser
    {
        int Id { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        bool IsVerified { get; set; }
        string RefreshToken { get; set; }
        DateTime RefreshTokenExpirationUTC { get; set; }
    }
}
