using System.Collections.Generic;
using Newtonsoft.Json;

namespace frontend.Controllers
{
    public class VisGraph
    {
        [JsonProperty("nodes")] public IList<VisNode> Nodes { get; set; } = new List<VisNode>();

        [JsonProperty("edges")] public IList<VisEdge> Edges { get; set; } = new List<VisEdge>();
    }
}