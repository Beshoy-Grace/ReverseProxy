using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReverseProxyAPI.Settings
{
    public class TokenSetting
    {
        public string BaseUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string x_api_key_GetWay { get; set; }
        public string x_api_key_Token { get; set; }
        public string TokenUrl { get; set; }
    }
}
