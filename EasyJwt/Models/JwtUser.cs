using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EasyJwt.Models
{
    public class JwtUser
    {
        public string Name { get; set; }

        public string Password { get; set; }

        public List<Claim> Claims { get; set; } = new List<Claim>();

        public List<string> Roles { get; set; }
    }
}
