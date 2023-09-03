using System.Text.Json.Serialization;

namespace KoenZomers.Ring.Api.Entities
{
    public class Doorbot
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
