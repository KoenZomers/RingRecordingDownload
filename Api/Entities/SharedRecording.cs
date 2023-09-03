using System.Text.Json.Serialization;

namespace KoenZomers.Ring.Api.Entities
{
    /// <summary>
    /// Message providing an URL where to download a shared recording from
    /// </summary>
    public class SharedRecording
    {
        /// <summary>
        /// The URL where to download a shared recording from
        /// </summary>
        [JsonPropertyName("wrapper_url")]
        public string WrapperUrl { get; set; }
    }
}