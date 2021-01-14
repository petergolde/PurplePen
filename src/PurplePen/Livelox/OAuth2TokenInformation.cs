using System;

namespace PurplePen.Livelox
{
    class OAuth2TokenInformation
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}