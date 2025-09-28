using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Models
{
    class LoginRequest
    {
        private string? username;
        private string? password;
        private int? expiresInMins;

        public string? Username { get => username; set => username = value; }
        public string? Password { get => password; set => password = value; }
        public int? ExpiresInMins { get => expiresInMins; set => expiresInMins = value; }
    }
}
