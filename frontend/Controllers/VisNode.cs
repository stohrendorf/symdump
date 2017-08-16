using Newtonsoft.Json;

namespace frontend.Controllers
{
    public class VisNode
    {
        public class VisFont
        {
            [JsonProperty("face")]
            public string Face { get; set; } = "monospace";

            [JsonProperty("align")]
            public string Align { get; set; } = "left";
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; } = "#FFCFCF";

        [JsonProperty("shape")]
        public string Shape { get; set; } = "box";

        [JsonProperty("font")]
        public VisFont Font { get; set; } = new VisFont();

        [JsonProperty("size")]
        public int Size { get; set; } = 150; // TODO check if necessary
    }
}
