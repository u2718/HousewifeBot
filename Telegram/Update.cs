using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
