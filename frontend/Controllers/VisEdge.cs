using Newtonsoft.Json;

namespace frontend.Controllers
{
    public class VisEdge
    {
        [JsonIgnore] public VisNode From;

        [JsonIgnore] public VisNode To;

        [JsonProperty("from")] public string FromId => From.Id;

        [JsonProperty("to")] public string ToId => To.Id;

        [JsonProperty("arrows")] public string Arrows { get; set; } = "to";

        [JsonProperty("physics")] public bool Physics { get; set; }

        [JsonProperty("smooth")] public VisSmooth Smooth { get; set; } = new VisSmooth();

        [JsonProperty("color")] public string Color { get; set; } = "#0000ff";

        [JsonProperty("dashes")] public bool Dashes { get; set; }

        public class VisSmooth
        {
            [JsonProperty("type")] public string Type { get; set; } = "cubicBezier";

            [JsonProperty("enabled")] public bool Enabled { get; set; } = true;
        }
    }
}