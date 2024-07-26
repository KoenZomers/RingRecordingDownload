using System.Text.Json.Serialization;
using System;

namespace KoenZomers.Ring.Api.Entities
{
    /// <summary>
    /// Represents an OAuth Token received from the Ring API that can be used to communicate with the Ring Services
    /// </summary>
    public class OAutToken
    {
        private int _expiresInSeconds;

        /// <summary>
        /// The OAuth access token that can be used as a Bearer token to communicate with the Ring API
        /// </summary>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets the amount of seconds after creation of this OAuth token after which it expires
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresInSeconds
        {
            get { return _expiresInSeconds; }
            set { _expiresInSeconds = value; ExpiresAt = DateTime.Now.AddSeconds(value); }
        }

        /// <summary>
        /// Gets a DateTime with when this token expires
        /// </summary>
        public DateTime ExpiresAt { get; private set; }

        /// <summary>
        /// The OAuth Refresh Token that can be used to get a new OAuth Access Token after it expires
        /// </summary>
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }
}