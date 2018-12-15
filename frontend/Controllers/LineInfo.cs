using Newtonsoft.Json;

namespace frontend.Controllers
{
    public class LineInfo
    {
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("text")] public string Text { get; set; }
    }
}