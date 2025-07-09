using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EasyAuth.Dtos.User
{
    public class ChangePasswordDto
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("oldPassword")]
        public required string OldPassword { get; set; }
        [JsonPropertyName("newPassword")]
        public required string NewPassword { get; set; }
    }
}
