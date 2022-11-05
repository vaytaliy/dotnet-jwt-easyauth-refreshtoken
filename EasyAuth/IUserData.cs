using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyAuth
{
    public interface IUserData
    {
        public string Jwta_token { get; set; }
    }
}
