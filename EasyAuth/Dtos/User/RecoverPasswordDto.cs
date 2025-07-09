using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EasyAuth.Dtos.User
{
    public class RecoverPasswordDto
    {
        [JsonPropertyName("recoveryPassword")]
        [JsonRequired]
        public string RecoveryPassword { get; set; }
    }
}
