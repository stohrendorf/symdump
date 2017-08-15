using Newtonsoft.Json;

namespace frontend.Controllers
{
    public class LineInfo
    {
        [JsonProperty("address")] public uint Address { get; set; }
        [JsonProperty("text")] public string Text { get; set; }
        [JsonProperty("jumpTarget")] public uint? JumpTarget { get; set; }
    }
}
