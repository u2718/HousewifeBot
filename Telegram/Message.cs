using Newtonsoft.Json;

namespace Telegram
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Message
    {
        [JsonProperty (PropertyName = "message_id")]
        public int MessageId { get; set; }

        [JsonProperty(PropertyName = "from")]
        public User From { get; set; }

        [JsonProperty(PropertyName = "date")]
        public int Date { get; set; }

        [JsonProperty(PropertyName = "chat")]
        public User Chat { get; set; }
        
        [JsonProperty (PropertyName = "text")]
        public string Text { get; set; }

    }
}
