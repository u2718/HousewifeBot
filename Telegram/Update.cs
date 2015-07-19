using Newtonsoft.Json;

namespace Telegram
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Update
    {
        [JsonProperty (PropertyName = "update_id")]
        public int UpdateId { get; set; }

        [JsonProperty (PropertyName = "message")]
        public Message Message { get; set; }
    }
}
