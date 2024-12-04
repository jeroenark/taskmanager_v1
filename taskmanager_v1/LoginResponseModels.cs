using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace taskmanager_v1
{
    public class LoginResponse
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string[] Message { get; set; }

        [JsonProperty("data")]
        public LoginData Data { get; set; }
    }

    public class LoginData
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("access_token_expiry")]
        public int AccessTokenExpiry { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }
    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string[] Message { get; set; }
    }
}
