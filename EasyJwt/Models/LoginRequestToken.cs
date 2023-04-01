using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EasyJwt.Models
{
    public class LoginRequestToken
    {
        [Required]
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}
